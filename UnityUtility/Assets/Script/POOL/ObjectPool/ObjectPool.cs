using System.Collections;

namespace POOL.ObjectPool
{
    public class ObjectPool<T> : AObjectPool<T>
    {
        public override T Get()
        {
            throw new System.NotImplementedException();
        }

        public override void Release(T obj)
        {
            throw new System.NotImplementedException();
        }

        public override void ClearAll()
        {
            throw new System.NotImplementedException();
        }

        public override void Clear(int count)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerator OnUpdate()
        {
            throw new System.NotImplementedException();
        }
    }
}