using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework
{
    internal abstract class AResource : CustomYieldInstruction, IResource
    {
        /// <summary>
        /// Asset对应的url
        /// </summary>
        public string url { get; set; }

        //是否加载完成
        internal bool done { get; set; }


        /// <summary>
        /// 引用的bundle
        /// </summary>
        internal ABundle Bundle { get; set; }
        
        internal ResourceAwaiter Awaiter{ get; set; }

        /// <summary>
        /// 加载完成的资源
        /// </summary>
        public virtual Object Asset { get; protected set; }

        /// <summary>
        /// 引用计数器
        /// </summary>
        internal int Reference { get; set; }

        /// <summary>
        /// 依赖资源
        /// </summary>
        internal AResource[] dependencies { get; set; }

        internal Action<AResource> FinishedCallback { get; set; }

        #region 抽象函数

        /// <summary>
        /// 加载资源
        /// </summary>
        internal abstract void Load();

        /// <summary>
        /// 卸载资源
        /// </summary>
        internal abstract void Unload();

        internal abstract void LoadAsset();

        public abstract T GetAsset<T>() where T : Object;

        #endregion

        public void ReduceReference()
        {
            Reference--;
            if (Reference < 0)
            {
                throw new Exception($"{GetType()}.{nameof(ReduceReference)}() less than 0,{nameof(url)}:{url}");
            }
        }

        /// <summary>
        /// 刷新异步资源（当同步资源的依赖包含异步是，需要立即返回）
        /// </summary>
        internal void FreshAsyncAsset()
        {
            if (done)
            {
                return;
            }

            if (dependencies != null)
            {
                for (int i = 0; i < dependencies.Length; ++i)
                {
                    AResource resource = dependencies[i];
                    // 递归刷新依赖的异步资源
                    resource.FreshAsyncAsset();
                }
            }

            // 如果是异步资源，立即加载
            if (this is AResourceAsync)
            {
                LoadAsset();
            }
        }

        public Object GetAsset()
        {
            return Asset;
        }

        internal void AddReference()
        {
            ++Reference;
        }

        public GameObject Instantiate()
        {
            Object obj = Asset;
            if (!obj)
            {
                return null;
            }

            if (!(obj is GameObject))
            {
                return null;
            }

            return Object.Instantiate(obj) as GameObject;
        }

        public GameObject Instantiate(bool autoUnload)
        {
            throw new NotImplementedException();
        }

        public GameObject Instantiate(Vector3 position, Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public GameObject Instantiate(Vector3 position, Quaternion rotation, bool autoUnload)
        {
            throw new NotImplementedException();
        }

        public GameObject Instantiate(Transform parent, bool instanceInWorldSpace)
        {
            Object obj = Asset;
            if (!obj)
            {
                return null;
            }

            if (!(obj is GameObject))
            {
                return null;
            }

            return Object.Instantiate(obj, parent, instanceInWorldSpace) as GameObject;
        }

        public GameObject Instantiate(Transform parent, bool instantiateInWorldSpace, bool autoUnload)
        {
            throw new NotImplementedException();
        }
    }
}