using Common;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityNativeHull
{
    public struct FaceQueryResult
    {
        public int Index;// 面的索引
        public float Distance;// 面的距离
    }

    public struct EdgeQueryResult
    {
        public int Index1;// hull1 的边索引
        public int Index2;// hull2 的边索引
        public float Distance;// 边间的距离
    }

    public struct CollisionInfo
    {
        public bool IsColliding;// 是否发生碰撞   
        public FaceQueryResult Face1;// 第一个面的查询结果
        public FaceQueryResult Face2;// 第二个面的查询结果
        public EdgeQueryResult Edge;// 边查询结果
    }

    class HullCollision
    {
        public static CollisionInfo GetDebugCollisionInfo(RigidTransform transform1, NativeHull hull1, RigidTransform transform2, NativeHull hull2)
        {
            CollisionInfo result = default;
            QueryFaceDistance(out result.Face1, transform1, hull1, transform2, hull2);
            QueryFaceDistance(out result.Face2, transform2, hull2, transform1, hull1);
            // 主要是物体很有可能两个凸起的地方错开，实际没相交，单纯计算面就相交了。所以必须还检测边
            QueryEdgeDistance(out result.Edge, transform1, hull1, transform2, hull2);
            result.IsColliding = result.Face1.Distance < 0 && result.Face2.Distance < 0 && result.Edge.Distance < 0;
            return result;
        }

        /// <summary>
        /// 查询两个体之间的面距离，主要就是再找分离轴，通过每个面法线（即 plane.Normal）作为潜在的分离轴来进行计算的
        /// </summary>
        /// <param name="result"></param>
        /// <param name="transform1"></param>
        /// <param name="hull1"></param>
        /// <param name="transform2"></param>
        /// <param name="hull2"></param>
        public static unsafe void QueryFaceDistance(out FaceQueryResult result, RigidTransform transform1, NativeHull hull1, RigidTransform transform2, NativeHull hull2)
        {
            // 在第二个体的局部空间中进行计算
            RigidTransform transform = math.mul(math.inverse(transform2), transform1);
            result.Distance = -float.MaxValue;
            result.Index = -1;

            //实际上这个for就是再选一组分离轴，挨个算。遍历 hull1 的每一个面，每个面的法线 plane.Normal 被当作一个分离轴
            for (int i = 0; i < hull1.FaceCount; i++)
            {                
                // 获取面平面
                NativePlane plane = transform * hull1.GetPlane(i);
                // 获取支撑点，注意这里是用的hull2调用的hull1面的法线，
                // 也就是说目的是找到 hull2 在这个法线反方向上最远的点（即最靠近 hull1 面的点）。
                // 可以进到这个GetSupport函数一看便知，不断地dot点乘这个内法线，值最大的就是距离最近的。返回值是点的坐标
                //  这里是获取参数方向最远的点，-plane.Normal就是hull2中沿着hull1面法线的反方向的最远的点
                float3 support = hull2.GetSupport(-plane.Normal);
                // 计算面到支撑点的距离，法线目前就当做是探索的分离轴，distance 就是这个轴上 hull2 的投影值
                // support 点沿着分离轴的投影值 - hull1 当前面的投影值,也就是这两个投影区间的距离。
                //  这里算的距离就是沿着plane.Normal方向的距离，要是小于0就说明在这个点在hull1面所在平面的内侧就碰撞了
                float distance = plane.Distance(support);

                // 当 distance > 0 时，表示：
                // support 落在了 hull1 这个面所在平面的“外面”；也就是在这个轴上存在投影间隙；
                // 也就是 SAT 中的 "分离轴存在"。

                // 当 distance <= 0 时，表示：
                // support 落在了 hull1 面平面内侧或上面；
                // 在这个轴上投影区间重叠；需要继续检测其它轴。

                //更新最大距离和面索引，所以保存好最大的就行了，知道有存在的即可了，全都小于0那就是真分不开了。
                if (distance > result.Distance)
                {
                    result.Distance = distance;
                    result.Index = i;
                }
            }
        }

        // 查询两个体之间的边距离
        public static unsafe void QueryEdgeDistance(out EdgeQueryResult result, RigidTransform transform1, NativeHull hull1, RigidTransform transform2, NativeHull hull2)
        {
            // 在第二个体的局部空间中进行计算
            RigidTransform transform = math.mul(math.inverse(transform2), transform1);

            float3 C1 = transform.pos;  // 获取第一个刚体的位移

            result.Distance = -float.MaxValue;  // 初始化最远距离
            result.Index1 = -1;  // 初始化边1的索引
            result.Index2 = -1;  // 初始化边2的索引

            for (int i = 0; i < hull1.EdgeCount; i+=2)
            {
                NativeHalfEdge* edge1 = hull1.GetEdgePtr(i);  // 获取第i条边
                NativeHalfEdge* twin1 = hull1.GetEdgePtr(i + 1);

                Debug.Assert(edge1->Twin == i + 1 && twin1->Twin == i);  // 确保对偶关系

                float3 P1 = math.transform(transform, hull1.GetVertex(edge1->Origin)); 
                float3 Q1 = math.transform(transform, hull1.GetVertex(twin1->Origin));  
                float3 E1 = Q1 - P1;  // 计算边1的向量

                float3 U1 = math.rotate(transform, hull1.GetPlane(edge1->Face).Normal);  // 获取边1所在面的法线
                float3 V1 = math.rotate(transform, hull1.GetPlane(twin1->Face).Normal);

                for (int j = 0; j < hull2.EdgeCount; j+=2)
                {
                    NativeHalfEdge* edge2 = hull2.GetEdgePtr(j);  // 获取第j条边
                    NativeHalfEdge* twin2 = hull2.GetEdgePtr(j + 1);

                    Debug.Assert(edge2->Twin == j + 1 && twin2->Twin == j);  // 确保对偶关系

                    float3 P2 = hull2.GetVertex(edge2->Origin);  // 边2起点
                    float3 Q2 = hull2.GetVertex(twin2->Origin);  // 边2终点
                    float3 E2 = Q2 - P2;  // 边2向量

                    float3 U2 = hull2.GetPlane(edge2->Face).Normal;  // 面法线向量
                    float3 V2 = hull2.GetPlane(twin2->Face).Normal;  // 面法线向量

                    if (IsMinkowskiFace(U1, V1, -E1, -U2, -V2, -E2))
                    {
                        float distance = Project(P1, E1, P2, E2, C1);
                        if (distance > result.Distance)
                        {
                            result.Distance = distance;
                            result.Index1 = i;
                            result.Index2 = j;
                        }
                    }
                }
            }
        }

        // 判断两个边是否在Minkowski空间中形成面
        //这里比较精髓，先去看文档里Minkowski差的概念，我们没有必要构建真实的差集，利用特性去算即可。
        public static bool IsMinkowskiFace(float3 A, float3 B, float3 B_x_A, float3 C, float3 D, float3 D_x_C)
        {
            // 检查AB和CD是否在单位球上相交
            float CBA = math.dot(C, B_x_A); // C 在 E1 的哪一侧？
            float DBA = math.dot(D, B_x_A); // D 在 E1 的哪一侧？
            float ADC = math.dot(A, D_x_C); // A 在 E2 的哪一侧？
            float BDC = math.dot(B, D_x_C); // B 在 E2 的哪一侧？

            // 如果C在E1的一侧，D在另一侧，并且A在E2的一侧，B在另一侧，那么它们形成一个Minkowski面。就像 >< 交叉一样
            return CBA * DBA < 0 &&  
                   ADC * BDC < 0 &&  
                   CBA * BDC > 0;    // 如果是<<或>>，则不形成Minkowski面，反之如果是><，则形成Minkowski面
        }

        /// <summary>
        /// 对两个边向量（分别属于两个物体）构造一个分离轴，
        /// 并将第二个物体相对于第一个物体在该轴上的投影距离作为分离度量返回。
        /// 如果两个边近似平行，则认为无法构造有效分离轴，返回一个极小值（表示忽略此轴）。
        /// </summary>
        /// <param name="P1">第一个物体的边的起点（世界坐标）</param>
        /// <param name="E1">第一个物体的边向量</param>
        /// <param name="P2">第二个物体的边的起点（世界坐标）</param>
        /// <param name="E2">第二个物体的边向量</param>
        /// <param name="C1">第一个物体的质心（用于决定分离轴方向）</param>
        /// <returns>
        /// 返回值为在构造出的分离轴（E1 × E2）方向上，第二个边相对于第一个边的投影距离：
        ///若为正，表示存在间隙；
        /// — 若为负，表示重叠（即发生碰撞）；
        /// — 若返回极小值（-float.MaxValue），表示两边近似平行，不适合作为分离轴。
        /// </returns>
        public static float Project(float3 P1, float3 E1, float3 P2, float3 E2, float3 C1)
        {
            // 步骤1：计算候选分离轴 —— 两条边的叉积方向
            float3 E1xE2 = math.cross(E1, E2);

            // 步骤2：判断是否平行（叉积近似为0）
            float kTol = 0.005f;
            float L = math.length(E1xE2); // 得到叉积向量的长度，即 sin(θ)·|E1|·|E2|

            // 如果两条边几乎平行（sinθ 很小），则这个分离轴无效，跳过处理
            // 这个判断条件等价于：sin(θ) < kTol，
            // 即：|E1 x E2| < kTol * |E1||E2| 因为 0 <= sin(θ) <= 1，所以∣E1×E2∣≤∣E1∣∣E2∣
            if (L < kTol * math.sqrt(math.lengthsq(E1) * math.lengthsq(E2)))
            {
                return -float.MaxValue; // 返回极小值，表示忽略此轴
            }

            // 步骤3：归一化分离轴
            float3 N = (1 / L) * E1xE2; 

            // 步骤4：确保分离轴方向从第一个物体指向第二个物体
            // 用第一个边的起点 P1 到物体1质心 C1 的向量与 N 做点乘判断方向
            // 若为负数，说明 N 朝向物体内部，应当翻转
            if (math.dot(N, P1 - C1) < 0)
            {
                N = -N; 
            }


            // 步骤5：将两个边的起点在分离轴方向上做投影
            // 表达式 math.dot(N, P2 - P1) 即为第二条边（P2）到第一条边（P1）在分离轴方向 N 上的距离。
            // 由于 N 已经被调整为从物体1指向物体2，所以：
            // 如果结果为正数 → 有间隙；如果为负数 → 发生重叠。
            return math.dot(N, P2 - P1);
        }
    }
}
