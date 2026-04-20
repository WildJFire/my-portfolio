using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework
{
    /// <summary>
    /// 异步 AssetBundle 实现类
    /// 提供非阻塞式的资源加载方式，通过 Update 方法轮询加载进度
    /// </summary>
    internal class BundleAsync : ABundleAsync
    {
        /// <summary>
        /// Unity 异步创建 AssetBundle 的请求对象
        /// 用于跟踪 Bundle 文件的加载进度和获取最终结果
        /// </summary>
        private AssetBundleCreateRequest _assetBundleCreateRequest;


        /// <summary>
        /// 更新异步加载状态
        /// 检查依赖项和自身加载进度，完成时自动卸载无引用的 Bundle
        /// </summary>
        /// <returns>是否加载完成，true 表示完成，false 表示仍在加载中</returns>
        internal override bool Update()
        {
            // 已完成则直接返回
            if (done)
            {
                return true;
            }

            // 等待所有依赖 Bundle 加载完成
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

            // 等待 AssetBundle 异步加载完成
            if (!_assetBundleCreateRequest.isDone)
            {
                return false;
            }

            done = true;
            
            AssetBundle = _assetBundleCreateRequest.assetBundle;
            isStreamedSceneAssetBundle = AssetBundle.isStreamedSceneAssetBundle;
            // 如果引用已归零，立即卸载
            if (Reference <= 0)
            {
                Unload();
            }
            
            return true;
        }

        /// <summary>
        /// 启动异步加载流程
        /// 验证文件存在性并创建 AssetBundleLoadFromFileAsync 请求
        /// </summary>
        /// <exception cref="Exception">当重复调用 Load 或文件不存在时抛出异常</exception>
        internal override void Load()
        {
            if (_assetBundleCreateRequest != null)
            {
                throw new Exception(
                    $"{nameof(BundleAsync)}.{nameof(Load)}() {nameof(_assetBundleCreateRequest)} not null, {this}");
            }

            string file = BundleManager.Instance.GetFileUrl(url);
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!File.Exists(file))
            {
                throw new Exception($"{nameof(BundleAsync)}.{nameof(Load)}() {nameof(file)} not exist, file: {file}");
            }
#endif
            // 创建异步加载请求
            _assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(file, 0, BundleManager.Instance.Offset);
        }

        /// <summary>
        /// 卸载 AssetBundle 并清理相关数据
        /// 处理了请求未完成和已完成两种情况的资源释放
        /// </summary>
        internal override void Unload()
        {
            if (AssetBundle)
            {
                AssetBundle.Unload(true);
            }
            else
            {
                // 请求未完成时，先获取已加载的 Bundle 再卸载
                if (_assetBundleCreateRequest != null)
                {
                    AssetBundle = _assetBundleCreateRequest.assetBundle;
                }
                if (AssetBundle)
                {
                    AssetBundle.Unload(true);
                }
            }
            _assetBundleCreateRequest = null;
            done = false;
            Reference = 0;
            AssetBundle = null;
            isStreamedSceneAssetBundle = false;
        }

        /// <summary>
        /// 从 AssetBundle 中加载指定资源
        /// 支持在异步加载过程中同步获取资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源类型</param>
        /// <returns>加载的资源对象</returns>
        /// <exception cref="Exception">当参数无效或请求对象为空时抛出异常</exception>
        internal override Object LoadAsset(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new Exception($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() {nameof(name)} is null");
            }

            if (_assetBundleCreateRequest == null)
            {
                throw new Exception(
                    $"{nameof(BundleAsync)}.{nameof(LoadAsset)}() {nameof(_assetBundleCreateRequest)} is null");
            }

            if (AssetBundle == null)
            {
                AssetBundle = _assetBundleCreateRequest.assetBundle;
            }

            return AssetBundle.LoadAsset(name, type);
        }

        /// <summary>
        /// 异步加载 AssetBundle 中的资源
        /// 返回 AssetBundleRequest 用于跟踪加载进度
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源类型</param>
        /// <returns>AssetBundleRequest 请求对象</returns>
        /// <exception cref="Exception">当参数无效或请求对象为空时抛出异常</exception>
        internal override AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new Exception($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() {nameof(name)} is null");
            }

            if (_assetBundleCreateRequest == null)
            {
                throw new Exception(
                    $"{nameof(BundleAsync)}.{nameof(LoadAsset)}() {nameof(_assetBundleCreateRequest)} is null");
            }

            if (AssetBundle == null)
            {
                AssetBundle = _assetBundleCreateRequest.assetBundle;
            }

            return AssetBundle.LoadAssetAsync(name, type);
        }
    }
}