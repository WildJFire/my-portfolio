using System;
using System.Diagnostics;
using Unity.Burst;

namespace UnityNativeHull
{
    [DebuggerDisplay("TestShape: Id = {Id}")]
    [BurstCompile]// 这个特性告诉 Burst 编译器对这个结构体进行优化编译，以提高性能。
    //IEquatable是为了重载Equals判等操作，而IComparable是为了CompareTo比较
    public class TestShape : IEquatable<TestShape>, IComparable<TestShape>
    {
        public int Id;

        public NativeHull Hull;

        public int CompareTo(TestShape other)
        {
            return Id.CompareTo(other.Id);
        }

        public bool Equals(TestShape other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is TestShape shape && shape.Equals(this);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
