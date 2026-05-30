using System.Diagnostics;

namespace UnityNativeHull
{
    
    [DebuggerDisplay("NativeFace: Edge = {Edge}")]
    public struct NativeFace
    {
        /// <summary>
        /// 该面的起始边的索引
        /// </summary>
        public int Edge;
    }
}
