using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace UnityNativeHull
{
    internal class HullFactory
    {
        public struct DetailedFaceDefine
        {
            public Vector3 Center;
            public Vector3 Normal;
            public List<float3> Verts; // 顶点列表
            public List<int> Indices; // 顶点索引列表
        }

        public unsafe struct NativeFaceDefine
        {
            public int VertexCount;// 顶点数量
            public int* Vertices;// 顶点数组指针 
            public int HighestIndex;// 最高的顶点索引，用于优化
        }

        public unsafe struct NativeHullDefine
        {
            public int FaceCount;// 面数量
            public int VertexCount;// 顶点数量
            public NativeArray<float3> VerticesNative;// 顶点原生数组
            public NativeArray<NativeFaceDefine> FacesNative;// 面原生数组
        }

        // 多边形周长计算结构
        public struct PolygonPerimeter
        {
            public struct Edge
            {
                public int StartIndex;
                public int EndIndex;
            }

            private static readonly List<Edge> _outsideEdges = new List<Edge>();
            // 计算多边形的边界周长（即外部边缘的有序列表）
            // 参数：
            //   indices：由三角面组成的索引数组（每 3 个为一组三角形）
            // 返回：
            //   外部边缘（即构成多边形轮廓的边）列表，已按顺序排列形成闭环
            public static List<Edge> CalculatePerimeter(int[] indices)
            { // 清空全局临时边列表 OutsideEdges（存储的是最终的“边界边”，不是三角形内部边）
                _outsideEdges.Clear();

                // 遍历所有三角形（每 3 个索引构成一个三角形）
                for (int i = 0; i < indices.Length - 1; i += 3)
                {
                    int v1 = indices[i];
                    int v2 = indices[i + 1];
                    int v3 = indices[i + 2];

                    // 将三角形的三条边尝试加入外部边集合
                    AddOutsideEdge(v1, v2);  // 边 v1->v2
                    AddOutsideEdge(v2, v3);  // 边 v2->v3
                    AddOutsideEdge(v3, v1);  // 边 v3->v1
                }

                for (int i = 0; i < _outsideEdges.Count; i++)
                {
                    var edge = _outsideEdges[i];
                    int nextIndex = (i + 1) % _outsideEdges.Count; // 下一个边的索引，使用模运算实现循环
                    var nextEdge = _outsideEdges[nextIndex];
                     
                    if (edge.EndIndex != nextEdge.StartIndex)
                    {// 如果当前边的终点不是下一个边的起点，说明边界不连续，需重构

                        return Rebuild();
                    }
                }
                return _outsideEdges;
            }

            private static List<Edge> Rebuild()
            {
                List<Edge> edges = new();
                // 构建一个从起点索引到终点索引的映射关系，方便后续重建边界路径，因为每次查找都是根据Start找到对应的End
                var map = _outsideEdges.ToDictionary(edge => edge.StartIndex, edge => edge.EndIndex);
                int curr = _outsideEdges.First().StartIndex;
                for (int i = 0; i < _outsideEdges.Count; i++)
                {
                    var edge = new Edge() { StartIndex = curr, EndIndex = map[curr] };
                    edges.Add(edge);
                    curr = edge.EndIndex;
                }
                return edges;
            }

            private static void AddOutsideEdge(int v1, int v2)
            {
                foreach (var edge in _outsideEdges)
                {
                    if ((edge.StartIndex == v1 && edge.EndIndex == v2) || (edge.StartIndex == v2 && edge.EndIndex == v1))
                    {
                        // 已经存在这条边或其反向边，说明它是两个三角形共享的“内部边”
                        // 将它从外部边集合中移除（最终我们只保留非共享的“轮廓边”）
                        _outsideEdges.Remove(edge);
                        return; // 退出，已经处理完这条边了
                    }
                }
                _outsideEdges.Add(new Edge { StartIndex = v1, EndIndex = v2 }); // 这条边还没有被处理过，加入外部边集合

            }
        }
        /// <summary>
        /// 从网格数据构建一个凸包
        /// </summary>
        /// <remarks>
        ///从 Mesh 顶点和三角形索引数据，计算每个三角形的法线和中心。
        ///去除重复顶点，合并具有相同法线且共享顶点的三角形，形成大面。
        ///计算每个合并面边界的顶点序列，标记“孤立顶点”并将其剔除。
        ///构造最终的顶点和面数据，传入底层方法生成 NativeHull 凸包数据结构
        /// </remarks>
        /// <param name="mesh">网格</param>
        /// <returns>凸包</returns>
        public static unsafe NativeHull CreateFromMesh(Mesh mesh)
        {
            #region 预处理顶点数据
            var faces = new List<DetailedFaceDefine>();
            var verts = mesh.vertices.Select(RoundVertex).ToList();// 对顶点位置进行四舍五入，减少浮点误差导致的重复顶点问题
            var uniqueVerts = verts.Distinct().ToList();// 去除重复顶点
            #endregion

            #region 收集三角形面信息
            var indicies = mesh.triangles;// 三角形索引数z
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {// 遍历三角形集合收集面数据，每三个索引构成一个三角形

                int idx0 = indicies[i];
                int idx1 = indicies[i + 1];
                int idx2 = indicies[i + 2];

                var v0 = verts[idx0];
                var v1 = verts[idx1];
                var v2 = verts[idx2];

                var normal = math.normalize(math.cross(v2 - v1, v0 - v1));
                var roundedNormal = RoundVertex(normal);// 对顶点位置进行四舍五入，减少浮点误差导致的重复顶点问题

                faces.Add(new DetailedFaceDefine
                {
                    Center = ((v0 + v1 + v2) / 3), // 面中心点（3顶点坐标均值）
                    Normal = roundedNormal, // 舍入后的法线
                    Verts = new List<float3> { v0, v1, v2 }, // 三角形顶点
                    Indices = new List<int>
                    {
                        uniqueVerts.IndexOf(v0), // 顶点在唯一顶点列表中的索引
                        uniqueVerts.IndexOf(v1),
                        uniqueVerts.IndexOf(v2)
                    }
                });
            }
            #endregion

            #region 合并面（按法线方向 & 共享顶点）          
            // 先根据法线分组，再根据共享顶点分组，合并具有相同法线和共享顶点的所有面
            List<List<DetailedFaceDefine>> mergedFaces = GroupBySharedVertex(GroupByNormal(faces));
            #endregion

            // 创建一个存储最终合并面定义的列表
            var faceDefs = new List<NativeFaceDefine>();
            // 用来记录“孤立”顶点的索引，这些顶点没有被任何边界连接
            var orphanIndices = new HashSet<int>();
            foreach (List<DetailedFaceDefine> faceDefines in mergedFaces)
            {
                #region 计算边界轮廓 & 识别孤立顶点
                // 收集该组所有面中所有顶点的索引，SelectMany 会将所有 face.Indices 扁平化成一个单一的集合（flat list），而不是一个集合的集合
                int[] indicesFromMergedFaces = faceDefines.SelectMany(faceDefines => faceDefines.Indices).ToArray();
                // 计算这些顶点形成的多边形的边界轮廓（边界顶点序列）
                List<PolygonPerimeter.Edge> border = PolygonPerimeter.CalculatePerimeter(indicesFromMergedFaces);
                // 获取边界顶点的索引列表，提取EndIndex组成新数组
                int[] borderIndices = border.Select(edge => edge.EndIndex).ToArray();
                foreach (int index in indicesFromMergedFaces.Except(borderIndices))
                {
                    orphanIndices.Add(index);
                }
                #endregion

                #region 构建面定义结构（NativeFaceDef）
                // 使用栈内存分配存储边界顶点索引的数组（为了性能）,不然在托管堆上
                var v = stackalloc int[borderIndices.Length];
                int max = 0;

                // 遍历边界顶点索引，记录最大索引值（后续删除孤立顶点时需要）
                for (int i = 0; i < borderIndices.Length; i++)
                {
                    if (borderIndices[i] > max)
                    {
                        max = borderIndices[i];
                    }
                    v[i] = borderIndices[i];
                }

                faceDefs.Add(new NativeFaceDefine
                {
                    VertexCount = borderIndices.Length,
                    Vertices = v,
                    HighestIndex = max
                });
                #endregion
            }

            #region 删除孤立顶点 & 修正索引
            // 处理孤立顶点：从唯一顶点列表中删除这些顶点,把孤立顶点索引从大到小排序，逐个处理,倒着删除索引不会乱掉
            // 删除不再被使用的孤立顶点，并在所有相关面中修正因删除而发生偏移的顶点索引，确保一致，OrderByDescending从大到小排列
            foreach (int orphanIdx in orphanIndices.OrderByDescending(i => i))
            {
                // 删除孤立顶点
                uniqueVerts.RemoveAt(orphanIdx);

                // 修正面中顶点索引，只修正那些 使用了索引大于或等于 orphanIdx 的面，因为小于它的索引不受影响，
                foreach (NativeFaceDefine face in faceDefs.Where(f => f.HighestIndex >= orphanIdx))
                {
                    for (int i = 0; i < face.VertexCount; i++)
                    {
                        int idxFaceVertex = face.Vertices[i];
                        if (idxFaceVertex >= orphanIdx)
                        {
                            // 顶点索引减1，保持索引正确
                            face.Vertices[i] = --idxFaceVertex;
                        }
                    }
                }
            }
            #endregion

            #region 构造 NativeHull
            var res = new NativeHull();

            // 使用临时原生数组（NativeArray）存放面和顶点数据，方便后续调用本地方法构建凸包
            // Allocator.Temp就是告诉Unity，我要分配一段临时使用的原生内存，在这一帧内使用完就释放
            using var faceNativeArray = new NativeArray<NativeFaceDefine>(faceDefs.ToArray(), Allocator.Temp);
            using var vertNativeArray = new NativeArray<float3>(uniqueVerts.ToArray(), Allocator.Temp);
            NativeHullDefine hullDefine = new NativeHullDefine
            {
                FaceCount = faceDefs.Count,
                VertexCount = uniqueVerts.Count,
                VerticesNative = vertNativeArray,
                FacesNative = faceNativeArray
            };

            SetFromFaces(ref res, hullDefine);
            #endregion

            res.IsCreated = true;
            return res;
        }

        /// <summary>
        /// 从面定义设置凸包（NativeHull）结构体
        /// </summary>
        /// <param name="result"></param>
        /// <param name="hullDefine"></param>
        private unsafe static void SetFromFaces(ref NativeHull nativeHull, NativeHullDefine hullDefine)
        {
            #region 基础验证与数据准备
            // 断言：面数和顶点数必须大于0，确保数据合法
            Debug.Assert(hullDefine.FaceCount > 0, "输入网格面数错误");
            Debug.Assert(hullDefine.VertexCount > 0, "输入网格顶点数错误");

            // 设置顶点数量
            nativeHull.VertexCount = hullDefine.VertexCount;

            // 将原生顶点数组转换为普通托管数组
            float3[] arr = hullDefine.VerticesNative.ToArray();

            // 将顶点数据复制到 Persistent 分配的 NativeArray 中（长期保留）
            nativeHull.VerticesNative = new Common.NativeArrayNoLeakDetection<float3>(arr, Allocator.Persistent);
            nativeHull.Vertices = (float3*)nativeHull.VerticesNative.GetUnsafePtr();

            nativeHull.FaceCount = hullDefine.FaceCount;
            // 创建面数组，存储所有 NativeFace 结构
            nativeHull.FacesNative = new Common.NativeArrayNoLeakDetection<NativeFace>(nativeHull.FaceCount, Allocator.Persistent);
            nativeHull.Faces = (NativeFace*)nativeHull.FacesNative.GetUnsafePtr();

            for (int i = 0; i < hullDefine.FaceCount; i++)
            {
                NativeFace* f = nativeHull.Faces + i;
                f->Edge = -1; // 初始化每个面的边索引为 -1，表示尚未设置边界信息
            }
            #endregion

            #region 面平面构建
            CreateFacesPlanes(ref nativeHull, ref hullDefine);
            #endregion

            var edgeMap = new Dictionary<(int v1, int v2), int>();// (int v1, int v2) 是边的顶点索引 ，int 是边的索引（在 NativeHalfEdge 数组中的位置），用于快速查找边界关系
            var edgesList = new NativeHalfEdge[10000]; // 临时边列表

            #region 遍历每个面
            for (int i = 0; i < hullDefine.FaceCount; i++)
            {
                #region 遍历顶点构建边
                NativeFaceDefine faceDef = hullDefine.FacesNative[i];
                int vertexCount = faceDef.VertexCount;
                Debug.Assert(vertexCount >= 3);

                int* vertices = faceDef.Vertices;
                var faceHalfEdges = new List<int>();

                for (int j = 0; j < vertexCount; j++)
                {
                    int v1 = vertices[j];
                    int v2 = vertices[(j + 1) % vertexCount];

                    // 检查边是否已存在（顺序正向）
                    bool edgeFound12 = edgeMap.TryGetValue((v1, v2), out int iter12);
                    // 也检查反向是否存在
                    bool edgeFound21 = edgeMap.ContainsKey((v2, v1));

                    Debug.Assert(edgeFound12 == edgeFound21);

                    if (edgeFound12)
                    {// 如果边已存在，说明这是另一面的共享边

                        int e12 = iter12;

                        if (edgesList[e12].Face == -1)
                        {// 如果边还没有绑定过面，则绑定当前面

                            edgesList[e12].Face = i;
                        }
                        else
                        {// 如果边已经绑定了面，则说明两个面试图共享方向相同的边，错误！

                            throw new Exception("两个共享边不能有相同顺序的相同顶点");
                        }

                        // 如果当前面尚未绑定主边，则绑定
                        if (nativeHull.Faces[i].Edge == -1)
                        {
                            nativeHull.Faces[i].Edge = e12;
                        }

                        // 添加这条边索引到当前面的半边序列中
                        faceHalfEdges.Add(e12);
                    }
                    else
                    {
                        int e12 = nativeHull.EdgeCount++;
                        int e21 = nativeHull.EdgeCount++;

                        if (nativeHull.Faces[i].Edge == -1)
                        {
                            nativeHull.Faces[i].Edge = e12;
                        }

                        faceHalfEdges.Add(e12);

                        // 初始化 e12（v1 → v2）
                        edgesList[e12].Prev = -1;
                        edgesList[e12].Next = -1;
                        edgesList[e12].Twin = e21;
                        edgesList[e12].Face = i;
                        edgesList[e12].Origin = v1;

                        // 初始化 e21（v2 → v1）
                        edgesList[e21].Prev = -1;
                        edgesList[e21].Next = -1;
                        edgesList[e21].Twin = e12;
                        edgesList[e21].Face = -1;
                        edgesList[e21].Origin = v2;

                        // 添加到边映射表，便于查重和匹配
                        edgeMap[(v1, v2)] = e12;
                        edgeMap[(v2, v1)] = e21;
                    }
                }
                #endregion

                #region 链接边环结构
                // 连接当前面的所有半边，使其形成闭环
                for (int j = 0; j < faceHalfEdges.Count; j++)
                {
                    int e1 = faceHalfEdges[j];
                    int e2 = faceHalfEdges[(j + 1) % faceHalfEdges.Count];

                    edgesList[e1].Next = e2;
                    edgesList[e2].Prev = e1;
                }
                #endregion
            }
            #endregion

            #region 写入数据
            nativeHull.EdgesNative = new Common.NativeArrayNoLeakDetection<NativeHalfEdge>(nativeHull.EdgeCount, Allocator.Persistent);
            for (int i = 0; i < nativeHull.EdgeCount; i++)
            {
                nativeHull.EdgesNative[i] = edgesList[i];
            }
            nativeHull.Edges = (NativeHalfEdge*)nativeHull.EdgesNative.GetUnsafePtr();
            #endregion
        }

        /// <summary>
        /// 为每个面构建对应的平面方程（包含法线和偏移）
        /// 用于后续碰撞检测中的面裁剪、投影等操作
        /// </summary>
        public unsafe static void CreateFacesPlanes(ref NativeHull nativeHull, ref NativeHullDefine hullDefine)
        {
            // 创建存储面平面的 NativeArray，使用 Persistent 分配器，确保长期可用
            nativeHull.PlanesNative = new Common.NativeArrayNoLeakDetection<NativePlane>(hullDefine.FaceCount, Allocator.Persistent);
            nativeHull.Planes = (NativePlane*)nativeHull.PlanesNative.GetUnsafePtr();

            for (int i = 0; i < hullDefine.FaceCount; i++)
            {
                NativeFaceDefine faceDef = hullDefine.FacesNative[i];
                int vertexCount = faceDef.VertexCount;

                // 一个面必须至少由 3 个顶点构成
                Debug.Assert(vertexCount >= 3, "输入网格必须至少有3个顶点");

                int* indices = faceDef.Vertices;
                 
                // 初始化法线和质心（中心点）
                float3 normal = default;
                float3 centroid = default;

                for (int j = 0; j < vertexCount; j++)
                {
                    int j1 = indices[j];
                    int j2 = indices[(j + 1) % vertexCount];

                    float3 v1, v2;
                    try
                    {// 尝试从顶点数组中获取 v1 和 v2，若失败则打印异常

                        v1 = hullDefine.VerticesNative[j1];
                        v2 = hullDefine.VerticesNative[j2];
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        throw;
                    }

                    normal += Newell(v1, v2); // 使用 Newell 方法累加计算法线
                    centroid += v1; // 累加顶点位置以计算质心
                }

                centroid /= vertexCount; // 计算质心位置（平均值）
                var normalizedNormal = math.normalize(normal); // 归一化法线向量

                // 设置当前面的法线和偏移值
                // plane.Normal：面法线
                // plane.Offset：法线与中心点点积（也可理解为点到原点的距离）
                nativeHull.Planes[i].Normal = normalizedNormal;
                nativeHull.Planes[i].Offset = math.dot(normalizedNormal, centroid); // 计算平面方程的偏移量
            }

            float3 Newell(float3 a, float3 b)
            {
                return new float3(
                    (a.y - b.y) * (a.z + b.z), // x 分量
                    (a.z - b.z) * (a.x + b.x), // y 分量
                    (a.x - b.x) * (a.y + b.y)  // z 分量
                );
            }
        }

        private static Dictionary<float3, List<DetailedFaceDefine>> GroupByNormal(IList<DetailedFaceDefine> faces)
        {
            var res = new Dictionary<float3, List<DetailedFaceDefine>>();
            foreach (var face in faces)
            {
                if (!res.TryGetValue(face.Normal, out List<DetailedFaceDefine> val))
                {
                    res[face.Normal] = new List<DetailedFaceDefine>() { face };
                    continue;
                }
                val.Add(face);
            }
            return res;
        }

        // 按共享顶点分组
        // 输入参数 groupedFaces 是一个字典，键是法线方向（float3），值是所有具有该法线的面（DetailedFaceDefine 列表）。
        // 这些面之前已经按法线归类，这里要进一步把法线相同且有“顶点连接”的面归并在一起。
        private static List<List<DetailedFaceDefine>> GroupBySharedVertex(Dictionary<float3, List<DetailedFaceDefine>> faces)
        {
            var res = new List<List<DetailedFaceDefine>>();
            foreach (var faceKv in faces)
            {
                // 临时 map，每个元素包含：
                // - 一个 HashSet<int>：用于记录当前面组中所有顶点的索引（用于判断是否与其它面共享）
                // - 一个面列表 List<DetailedFaceDefine>：存储当前组的所有面
                var map = new List<(HashSet<int> Key, List<DetailedFaceDefine> Value)>();
                foreach (DetailedFaceDefine faceDefine in faceKv.Value)
                {
                    // 从map中找一个组，该组符合条件：faceDefine的任一顶点索引在该组的Key中（即共享顶点）
                    var group = map.FirstOrDefault(group => faceDefine.Indices.Any(group.Key.Contains));
                    if (group.Key != null)
                    {
                        foreach (var indice in faceDefine.Indices)
                        {
                            group.Key.Add(indice);
                        }
                        group.Value.Add(faceDefine);
                    }
                    else
                    {
                        var newGroup = (new HashSet<int>(faceDefine.Indices), new List<DetailedFaceDefine>() { faceDefine });
                        map.Add(newGroup);
                    }
                }
                res.AddRange(map.Select(group => group.Value));
            }
            return res;
        }

        private static float3 RoundVertex(Vector3 vertex)
        {
            return new float3(
                (float)Math.Round(vertex.x, 3),
                (float)Math.Round(vertex.y, 3),
                (float)Math.Round(vertex.z, 3)
            );
        }
    }
}
