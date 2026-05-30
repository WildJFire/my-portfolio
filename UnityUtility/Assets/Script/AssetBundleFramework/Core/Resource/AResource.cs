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

        /// <summary>
        /// 实例化资源为GameObject
        /// </summary>
        /// <returns>实例化的GameObject，如果资源不是GameObject或资源不存在则返回null</returns>
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

        /// <summary>
        /// 实例化资源为GameObject
        /// </summary>
        /// <param name="autoUnload">是否在实例化后自动减少引用计数</param>
        /// <returns>实例化的GameObject，如果资源不是GameObject或资源不存在则返回null</returns>
        public GameObject Instantiate(bool autoUnload)
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

            GameObject instance = Object.Instantiate(obj) as GameObject;
            if (autoUnload)
            {
                ReduceReference();
            }
            return instance;
        }

        /// <summary>
        /// 在指定位置和旋转下实例化资源为GameObject
        /// </summary>
        /// <param name="position">实例化的位置</param>
        /// <param name="rotation">实例化的旋转</param>
        /// <returns>实例化的GameObject，如果资源不是GameObject或资源不存在则返回null</returns>
        public GameObject Instantiate(Vector3 position, Quaternion rotation)
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

            return Object.Instantiate(obj, position, rotation) as GameObject;
        }

        /// <summary>
        /// 在指定位置和旋转下实例化资源为GameObject
        /// </summary>
        /// <param name="position">实例化的位置</param>
        /// <param name="rotation">实例化的旋转</param>
        /// <param name="autoUnload">是否在实例化后自动减少引用计数</param>
        /// <returns>实例化的GameObject，如果资源不是GameObject或资源不存在则返回null</returns>
        public GameObject Instantiate(Vector3 position, Quaternion rotation, bool autoUnload)
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

            GameObject instance = Object.Instantiate(obj, position, rotation) as GameObject;
            if (autoUnload)
            {
                ReduceReference();
            }
            return instance;
        }

        /// <summary>
        /// 在指定父物体下实例化资源为GameObject
        /// </summary>
        /// <param name="parent">父物体Transform</param>
        /// <param name="instanceInWorldSpace">是否在世界空间中实例化</param>
        /// <returns>实例化的GameObject，如果资源不是GameObject或资源不存在则返回null</returns>
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

        /// <summary>
        /// 在指定父物体下实例化资源为GameObject
        /// </summary>
        /// <param name="parent">父物体Transform</param>
        /// <param name="instantiateInWorldSpace">是否在世界空间中实例化</param>
        /// <param name="autoUnload">是否在实例化后自动减少引用计数</param>
        /// <returns>实例化的GameObject，如果资源不是GameObject或资源不存在则返回null</returns>
        public GameObject Instantiate(Transform parent, bool instantiateInWorldSpace, bool autoUnload)
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

            GameObject instance = Object.Instantiate(obj, parent, instantiateInWorldSpace) as GameObject;
            if (autoUnload)
            {
                ReduceReference();
            }
            return instance;
        }
    }
}