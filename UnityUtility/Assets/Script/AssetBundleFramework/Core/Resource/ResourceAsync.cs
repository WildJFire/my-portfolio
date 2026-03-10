using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework
{
    /// <summary>
    /// 异步资源加载实现类
    /// 提供非阻塞式的资源加载机制，通过 Update 方法轮询进度
    /// 适用于需要显示加载进度或大型资源的场景
    /// </summary>
    internal class ResourceAsync : AResourceAsync
    {
        public override bool keepWaiting => !done;

        /// <summary>
        /// Unity 异步资源加载请求对象
        /// 用于跟踪资源从 Bundle 中的加载进度
        /// </summary>
        private AssetBundleRequest _assetBundleRequest;

        /// <summary>
        /// 卸载资源及其依赖的 Bundle
        /// 清理资源对象、回调和相关请求
        /// </summary>
        /// <exception cref="Exception">当 Bundle 为空时抛出异常</exception>
        internal override void Unload()
        {
            if (Bundle == null)
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(Unload)}() {nameof(Bundle)} is null");
            }

            // 非 GameObject 类型的资源需要手动卸载
            if (base.Asset != null && !(base.Asset is GameObject))
            {
                Resources.UnloadAsset(base.Asset);
                Asset = null;
            }

            _assetBundleRequest = null;
            BundleManager.Instance.Unload(Bundle);
            Bundle = null;
            FinishedCallback = null;
            Awaiter = null;

        }

        /// <summary>
        /// 从 Bundle 中实际加载资源对象
        /// 处理流式场景和普通资源的加载逻辑
        /// </summary>
        /// <exception cref="Exception">当 Bundle 为空时抛出异常</exception>
        internal override void LoadAsset()
        {
            if (Bundle == null)
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAsset)}() {nameof(Bundle)} not null");
            }

            // 非流式场景从异步请求中获取资源
            if (!Bundle.isStreamedSceneAssetBundle)
            {
                if (_assetBundleRequest != null)
                {
                    Asset = _assetBundleRequest.asset;
                }
                else
                {
                    Asset = Bundle.LoadAsset(url, typeof(Object));
                }
            }

            done = true;

            if (FinishedCallback != null)
            {
                Action<AResource> tempCallback = FinishedCallback;
                FinishedCallback = null;
                tempCallback.Invoke(this);
            }
        }

        /// <summary>
        /// 获取加载的资源对象，支持类型转换
        /// 针对 Sprite 类型进行特殊处理，确保从正确的 Bundle 中加载
        /// </summary>
        /// <typeparam name="T">期望返回的资源类型，继承自 UnityEngine.Object</typeparam>
        /// <returns>类型转换后的资源对象，如果类型不匹配或加载失败则返回 null</returns>
        /// <remarks>
        /// 当请求 Sprite 类型但当前资源不是 Sprite 时，会卸载原有资源并重新从 Bundle 加载
        /// 其他类型直接进行类型转换
        /// </remarks>
        public override T GetAsset<T>()
        {
            Object tempAsset = Asset;
            Type type = typeof(T);

            // 判断请求的资源类型是否为 Sprite
            if (type == typeof(Sprite))
            {
                // 当前资源已经是 Sprite 类型，直接返回
                if (Asset is Sprite)
                {
                    return tempAsset as T;
                }
                else
                {
                    //- 当需要从`Texture2D`转换为`Sprite`时，需要先卸载旧的`Texture2D`
                    //- 避免内存泄漏（`GameObject`不能通过`Resources.UnloadAsset`卸载）
                    if (tempAsset && !(tempAsset is GameObject))
                    {
                        Resources.UnloadAsset(tempAsset);
                    }

                    // 从 Bundle 中重新加载指定类型的资源
                    Asset = Bundle.LoadAsset(url, type);
                    return Asset as T;
                }
                //- 同一个图片资源，有时需要作为`Texture2D`使用（如RawImage）
                //- 有时需要作为`Sprite`使用（如Image组件）
                //- Unity的AssetBundle加载时，如果以`typeof(Object)`加载，可能返回`Texture2D`而不是`Sprite`
            }
            else
            {
                // 非 Sprite 类型，直接进行类型转换并返回
                return tempAsset as T;
            }
        }

        /// <summary>
        /// 加载资源所在的 AssetBundle
        /// 异步加载 Bundle 并触发完成回调
        /// </summary>
        /// <exception cref="Exception">当 URL 为空或 Bundle 已存在时抛出异常</exception>
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(Load)}() {nameof(url)} is null");
            }

            if (Bundle != null)
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(Load)}() {nameof(Bundle)} not null");
            }

            if (!ResourceManager.Instance.ResourceBundleDict.TryGetValue(url, out string bundleUrl))
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAsset)}() {nameof(bundleUrl)} is null");
            }

            Bundle = BundleManager.Instance.LoadAsync(bundleUrl);
        }

        /// <summary>
        /// 更新异步加载状态
        /// 检查依赖项、Bundle 和自身资源加载进度
        /// </summary>
        /// <returns>是否加载完成，true 表示完成，false 表示仍在加载中</returns>
        internal override bool Update()
        {
            if (done)
            {
                return true;
            }

            // 等待所有依赖资源加载完成
            if (dependencies != null)
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    if (!dependencies[i].done)
                    {
                        return false;
                    }
                }
            }

            // 等待 Bundle 加载完成
            if (!Bundle.done)
            {
                return false;
            }

            // Bundle 完成后启动资源异步加载
            if (_assetBundleRequest == null)
            {
                LoadAssetAsync();
            }

            // 等待资源加载完成
            if (_assetBundleRequest != null && !_assetBundleRequest.isDone)
            {
                return false;
            }

            LoadAsset();
            return true;
        }

        /// <summary>
        /// 启动资源的异步加载
        /// 创建 AssetBundleRequest 对象用于跟踪进度
        /// </summary>
        /// <exception cref="Exception">当 Bundle 为空时抛出异常</exception>
        internal override void LoadAssetAsync()
        {
            if (Bundle == null)
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAssetAsync)}() {nameof(Bundle)} not null");
            }

            _assetBundleRequest = Bundle.LoadAssetAsync(url, typeof(Object));
        }
    }
}