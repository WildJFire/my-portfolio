using Common;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnityNativeHull
{
    /// <summary>
    /// 表示一个原生的凸体数据结构，包含顶点、边、面等信息，并提供了资源管理和访问功能。
    /// </summary>
    public unsafe class NativeHull : System.IDisposable
    {
        public int VertexCount; // 顶点数量
        public int EdgeCount;   // 边数量
        public int FaceCount;   // 面数量

        // Unity 为 NativeArray、NativeList 等 Native 容器 提供了一种叫做 泄漏检测（Leak Detection） 的机制，
        // 以下用于告诉 NativeArray 在构造时跳过 LeakDetection 注册，用来提高速度。
        public NativeArrayNoLeakDetection<float3> VerticesNative;//顶点数组
        public NativeArrayNoLeakDetection<NativePlane> PlanesNative;//平面数组
        public NativeArrayNoLeakDetection<NativeHalfEdge> HalfEdgesNative;//半边数组
        public NativeArrayNoLeakDetection<NativeFace> FacesNative;//面数组

        // 顶点指针
        [NativeDisableUnsafePtrRestriction]
        public float3* Vertices;

        // 面指针
        [NativeDisableUnsafePtrRestriction]
        public NativeFace* Faces;

        // 半边指针
        [NativeDisableUnsafePtrRestriction]
        public NativeHalfEdge* HalfEdges;

        // 平面指针
        [NativeDisableUnsafePtrRestriction]
        public NativePlane* Planes;

        private int _isCreated;  // 标记该结构体是否已创建
        private int _isDisposed; // 标记该结构体是否已释放

        // 判断结构体是否已创建
        public bool IsCreated
        {
            get => _isCreated == 1;
            set => _isCreated = value ? 1 : 0;
        }

        // 判断结构体是否已释放
        public bool IsDisposed
        {
            get => _isDisposed == 1;
            set => _isDisposed = value ? 1 : 0;
        }

        public bool IsValid { get => IsCreated && !IsDisposed; }

        public void Dispose()
        {
            if (_isDisposed == 0)
            {
                _isDisposed = 1;
                if (VerticesNative.IsCreated) VerticesNative.Dispose();
                if (PlanesNative.IsCreated) PlanesNative.Dispose();
                if (HalfEdgesNative.IsCreated) HalfEdgesNative.Dispose();
                if (FacesNative.IsCreated) FacesNative.Dispose();

                Vertices = null;
                Planes = null;
                HalfEdges = null;
                Faces = null;
            }

        }

        public unsafe NativeHalfEdge* GetEdgePtr(int i) => HalfEdges + i;

        public unsafe NativeHalfEdge GetEdge(int index) => HalfEdges[index];
        public unsafe float3 GetVertex(int index) => VerticesNative[index];
        // 获取面指针
        public unsafe NativeFace* GetFacePtr(int index) => Faces + index;
        // 获取面（有限的真实的面，具体的矩形，三角形之类的面）
        public unsafe NativeFace GetFace(int index) => FacesNative[index];
        public unsafe NativePlane GetPlane(int i) => Planes[i];

        public unsafe float3 GetSupport(float3 direction) => Vertices[GetSupportIndex(direction)];

        /// <summary>
        /// GetSupportIndex 函数用于在给定方向上找到凸体中最远的顶点索引。
        /// 它通过计算每个顶点与方向的点积来确定哪个顶点在该方向上最远。
        /// </summary>
        /// <param name="direction">方向向量</param>
        /// <returns>最远顶点的索引</returns>
        private unsafe int GetSupportIndex(float3 direction)
        {
            int res = 0;
            float maxDot = math.dot(Vertices[0], direction);
            for(int i = 1; i < VertexCount; i++)
            {
                float dot = math.dot(Vertices[i], direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    res = i;
                }
            }
            return res;
        }

    }
}
