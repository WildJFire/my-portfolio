using System.Collections;
using System.Collections.Generic;

namespace POOL.ObjectPool
{
    public interface IObjectPool<T>
    {
        public void Init(int count);
        public T Get();
        public void Release(T obj);
        public void ClearAll();
        public void Clear(int count);
        public IEnumerator OnUpdate();
    }
}