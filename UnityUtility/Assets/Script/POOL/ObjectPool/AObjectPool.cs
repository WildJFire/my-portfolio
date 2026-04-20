using System.Collections;
using System.Collections.Generic;
using CommonUtility;

namespace POOL.ObjectPool
{
    public abstract class AObjectPool<T> : MonoSingleton<MonoObjectPool<T>>, IObjectPool<T>
    {
        private Queue<T> _pool;
        private int _count;
        private float _lastClearTime = 0;
        private float _clearInterval = 5000;

        public void Init(int count)
        {
            _count = count;
        }

        public abstract T Get();
        public abstract void Release(T obj);
        public abstract void ClearAll();
        public abstract void Clear(int count);
        public abstract IEnumerator OnUpdate();
        
    }
}