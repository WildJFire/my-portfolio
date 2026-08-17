using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace POOL.ObjectPool
{
    public class ObjectPool<T> : IObjectPool<T> where T : MonoBehaviour
    {
        /// <summary>
        /// 空闲池
        /// </summary>
        private List<PoolItem<T>> _poolItems;
        /// <summary>
        /// 初始化指定数量的对象池容量，默认10个，减少运行时的扩容次数，提升性能
        /// </summary>
        private int _count;
        private GameObject _prefab;
        private float _lastClearTime = 0;
        private GameObject _root;
        /// <summary>
        /// 定期清理的时间间隔，单位为毫秒，超过这个时间未被租用的对象将被销毁，释放内存资源，防止对象池无限增长
        /// </summary>
        private float _timeBetweenClears = 60000f; // 60秒

        public void Init(GameObject prefab, int count = 10)
        {
            _count = count;
            _prefab = prefab;
            _poolItems = new List<PoolItem<T>>(_count);
            _root = new GameObject($"Pool_{typeof(T).Name}");
            _root.transform.SetParent(null);
        }

        public PoolItem<T> Get(bool active = true)
        {
            if (_poolItems == null)
            {
                Debug.LogError("对象池未初始化！！！");
                return default;
            }
            if (_poolItems.Count > 0)
            {
                PoolItem<T> obj = _poolItems.LastOrDefault();
                obj.RentItem(active);
                _poolItems.Remove(_poolItems[^1]);
                return obj;
            }
            else
            {
                GameObject go = GameObject.Instantiate(_prefab);
                T component = go.GetComponent<T>();
                if (component == null)
                {
                    Debug.LogError("预制体上未找到指定类型的组件！！！");
                    return default;
                }
                PoolItem<T> obj = new PoolItem<T>(component, _poolItems.Count, _root);
                obj.RentItem(active);
                return obj;
            }
        }

        public void Release(PoolItem<T> obj)
        {
            if (obj == null)
            {
                Debug.LogError("释放对象不能为空！！！");
                return;
            }
            else if (!obj.IsRented)
            {//防止重复释放同一个对象，导致对象池中存在多个相同的对象，造成资源浪费和潜在的错误

                Debug.LogError("对象未被租出，无法释放！！！");
                return;
            }
            _poolItems.Add(obj);
            obj.MarkReturned();
        }

        public void ClearAll()
        {
            int count = _poolItems.Count;
            for(int i = 0; i < count; i++)
            {
                ClearOne(true);
            }
        }

        public void ClearOne(bool immediate = false)
        {
            PoolItem<T> obj = _poolItems.LastOrDefault();
            if (obj == null || obj.IsRented)
            {
                Debug.LogError("错误清除，对象非空或正在被租用！！！");
                return;
            }

            if (immediate || obj.IsIdleTimeout(TimeSpan.FromMilliseconds(1000)))
            {
                GameObject.Destroy(obj.Object.gameObject);
                _poolItems.Remove(obj);
            }
        }

        public void Update()
        {
            float currentTime = Time.time * 1000f; // 转换为毫秒
            if(_poolItems != null && _poolItems.Count > 0 && currentTime  - _lastClearTime > _timeBetweenClears)
            {
                ClearOne();
                _lastClearTime = currentTime;
            }
        }
    }
}