using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Common;

namespace UnityNativeHull
{
    public static class HullOperations
    {
        [BurstCompile]
        public struct TryGetContact : IBurstRefAction<NativeManifold, RigidTransform, NativeHull, RigidTransform, NativeHull>
        {
            public void Execute(ref NativeManifold arg1, RigidTransform arg2, NativeHull arg3, RigidTransform arg4, NativeHull arg5)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
