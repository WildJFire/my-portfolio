using System;
using UnityEngine;

namespace POOL.ObjectPool
{

    [Serializable]
    public class PoolItem<T> where T : MonoBehaviour
    {
        public T Object;                    // 实际对象引用
        public int PoolIndex;               // 唯一标识 _poolItems的索引
        public DateTime LastUsedTime;       // 最后使用时间（归还时更新）
        public DateTime CreatedTime;        // 创建时间
        public bool IsRented;               // 是否正在被租用
        public int UsageCount;              // 使用次数统计
        public GameObject Root;             // 对象所在的根节点

        public PoolItem(T obj, int id, GameObject root)
        {
            Object = obj;
            Root = root;
            PoolIndex = id;
            CreatedTime = DateTime.Now;
            LastUsedTime = DateTime.Now;
            IsRented = false;
            UsageCount = 0;
            this.Object.transform.SetParent(Root.transform);
        }

        public void RentItem(bool active = true)
        {
            IsRented = true;
            LastUsedTime = DateTime.Now;
            Object.gameObject.SetActive(active);
            MarkRented();
        }

        // 标记为租用
        public void MarkRented()
        {
            IsRented = true;
            UsageCount++;
        }

        // 标记为归还
        public void MarkReturned()
        {
            IsRented = false;
            LastUsedTime = DateTime.Now;
            Object.gameObject.SetActive(false);
            this.Object.transform.SetParent(Root.transform);
        }

        // 检查是否空闲超时
        public bool IsIdleTimeout(TimeSpan timeout)
        {
            return !IsRented && (DateTime.Now - LastUsedTime) > timeout;
        }
    }
}
