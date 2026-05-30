using System.Diagnostics;

namespace UnityNativeHull
{
    /// <summary>
    /// 两个面环之间的边结构，也叫对冲边。
    /// </summary>
    [DebuggerDisplay("NativeHalfEdge: Origin={Origin}, Face={Face}, Twin={Twin}, [Prev{Prev} Next={Next}]")]
    public struct NativeHalfEdge
    { 
        /// <summary>
       /// 面环中前一个边的索引。
       /// </summary>
        public int Prev;

        /// <summary>
        /// 面环中下一个边的索引。
        /// </summary>
        public int Next;

        /// <summary>
        /// 对边，与此边相对的另一条边的索引（在不同的面环中）。
        /// </summary>
        public int Twin;

        /// <summary>
        /// 该边所属面的索引。
        /// </summary>
        public int Face;

        /// <summary>
        /// 该边起点的顶点索引。
        /// </summary>
        public int Origin;
    }
}
