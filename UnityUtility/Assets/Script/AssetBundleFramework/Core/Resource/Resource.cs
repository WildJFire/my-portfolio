using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework
{
    internal class Resource : AResource
    {
        public override bool keepWaiting => !done;
        
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"{nameof(Resource)}.{nameof(Load)}() {nameof(url)} is null");
            }

            if (Bundle != null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}() {nameof(Bundle)} not null");
            }

            // 查找资源所在的 Bundle 路径
            if (!ResourceManager.Instance.ResourceBundleDict.TryGetValue(url, out string bundleUrl))
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}() {nameof(bundleUrl)} is null");
            }
            // 加载 Bundle
            Bundle = BundleManager.Instance.Load(bundleUrl);
            LoadAsset();
        }

        internal override void Unload()
        {
            if (Bundle == null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Unload)}() {nameof(Bundle)} is null");
            }
            // 非 GameObject 类型的资源需要手动卸载
            if (Asset != null && !(Asset is GameObject))
            {
                Resources.UnloadAsset(base.Asset);
                Asset = null;
            }      
            
            // 卸载 Bundle
            BundleManager.Instance.Unload(Bundle);
            Bundle = null;
            FinishedCallback = null;
            Awaiter = null;
        }
        
        internal override void LoadAsset()
        {
            if (Bundle == null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(LoadAsset)}() {nameof(Bundle)} is null");
            }
            
            // 刷新异步资源（当同步资源的依赖包含异步时，需要立即返回）
            FreshAsyncAsset();
            // 从 Bundle 中加载资源
            Asset = Bundle.LoadAsset(url, typeof(Object));
            done = true;
            if (FinishedCallback != null)
            {
                var tempCallback = FinishedCallback;
                FinishedCallback = null;
                tempCallback.Invoke(this);
            }
        }

        public override T GetAsset<T>()
        {
            Object tempAsset = Asset;
            Type type = typeof(T);
            if (type == typeof(Sprite))
            {
                if (Asset is Sprite)
                {
                    return tempAsset as T;
                }
                else
                {
                    if (tempAsset && !(tempAsset is GameObject))
                    {
                        Resources.UnloadAsset(tempAsset);
                    }

                    Asset = Bundle.LoadAsset(url, type);
                    return Asset as T;
                }
            }
            else
            {
                return tempAsset as T;
            }
        }
    }
}