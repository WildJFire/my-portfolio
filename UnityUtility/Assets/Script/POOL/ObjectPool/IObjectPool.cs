using UnityEngine;

namespace POOL.ObjectPool
{
    public interface IObjectPool<T> where T : MonoBehaviour
    {
        public void Init(GameObject prefab, int count = 10);
        public PoolItem<T> Get( bool active = true);
        public void Release(PoolItem<T> obj);
        public void ClearAll();
        public void ClearOne(bool immediate = false);
    }
}