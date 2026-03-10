using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleFramework
{
    internal class BundleManager : CommonUtility.Singleton<BundleManager>
    {
        /// <summary>
        /// 加载bundle开始的偏移
        /// </summary>
        internal ulong Offset { get; private set; }

        /// <summary>
        /// 获取资源真实路径回调
        /// </summary>
        private Func<string, string> _getFileCallback;

        /// <summary>
        /// bundle依赖管理信息
        /// </summary>
        private AssetBundleManifest _assetBundleManifest;

        /// <summary>
        /// 所有已加载的bundle
        /// </summary>
        private Dictionary<string, ABundle> _bundleDic = new ();

        /// <summary>
        /// 异步加载的 Bundle 临时列表
        /// 保存正在异步加载中的 BundleAsync 对象，等待其完成
        /// </summary>
        private List<ABundleAsync> _asyncList = new List<ABundleAsync>();

        /// <summary>
        /// 需要释放的bundle
        /// </summary>
        private LinkedList<ABundle> _needUnloadList = new LinkedList<ABundle>();

        /// <summary>
        /// 初始化 Bundle 管理器
        /// 加载 AssetBundleManifest 并设置路径回调和偏移量
        /// </summary>
        /// <param name="platform">目标平台名称</param>
        /// <param name="getFileCallback">获取资源真实路径的回调函数</param>
        /// <param name="offset">Bundle 加载偏移量</param>
        /// <exception cref="Exception">当 AssetBundleManifest 加载失败时抛出异常</exception>
        public void Initialize(string platform, Func<string, string> getFileCallback, ulong offset)
        {
            _getFileCallback = getFileCallback;
            Offset = offset;

            string assetBundleManifest = getFileCallback.Invoke(platform);
            AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(assetBundleManifest);
            var objects = manifestAssetBundle.LoadAllAssets();
            if (objects.Length <= 0)
            {
                throw new Exception($"{nameof(BundleManager)}.{nameof(Initialize)}() AssetBundleManifest load fail");
            }
            _assetBundleManifest = objects[0] as AssetBundleManifest;
        }
        
        /// <summary>
        /// 同步加载 Bundle
        /// </summary>
        /// <param name="bundleUrl">Bundle 的路径</param>
        /// <returns>加载的 Bundle 对象</returns>
        internal ABundle Load(string bundleUrl)
        {
            return LoadInternal(bundleUrl, false);
        }

        /// <summary>
        /// 卸载 Bundle
        /// 减少引用计数，当计数归零时延迟卸载
        /// </summary>
        /// <param name="bundle">要卸载的 Bundle 对象</param>
        /// <exception cref="ArgumentException">当 Bundle 为 null 时抛出异常</exception>
        internal void Unload(ABundle bundle)
        {
            if (bundle == null)
            {
                throw new ArgumentException($"{nameof(BundleManager)}.{nameof(Unload)}() bundle is null");
            }

            // 引用计数减 1
            bundle.ReduceReference();
            // 引用归零时标记为待卸载
            if (bundle.Reference == 0)
            {
                WillUnload(bundle);
            }
        }

        /// <summary>
        /// 标记 Bundle 为待卸载状态
        /// 添加到待卸载链表，等待 LateUpdate 统一处理
        /// </summary>
        /// <param name="bundle">待卸载的 Bundle</param>
        private void WillUnload(ABundle bundle)
        {
            _needUnloadList.AddLast(bundle);
        }

        /// <summary>
        /// 内部加载 Bundle 的核心方法
        /// 处理缓存查找、依赖加载和引用计数管理
        /// </summary>
        /// <param name="bundleUrl">Bundle 路径</param>
        /// <param name="async">是否异步加载</param>
        /// <returns>加载的 Bundle 对象</returns>
        public ABundle LoadInternal(string bundleUrl, bool async)
        {
            ABundle bundle;
            // 尝试从缓存字典中获取已加载的 Bundle
            if (_bundleDic.TryGetValue(bundleUrl, out bundle))
            {
                // 从缓存中找到，如果引用已归零则从待卸载列表移除
                if (bundle.Reference <= 0)
                {
                    _needUnloadList.Remove(bundle);
                }

                // 引用计数加 1
                bundle.AddReference();
                return bundle;
            }

            // 缓存未命中，创建新的 Bundle 对象
            if (async)
            {
                bundle = new BundleAsync();
                _asyncList.Add(bundle as ABundleAsync);
            }
            else
            {
                bundle = new Bundle();
            }
            bundle.url = bundleUrl;


            // 添加到缓存字典
            _bundleDic.Add(bundleUrl, bundle);

            // 加载依赖 Bundle
            string[] dependencies = _assetBundleManifest.GetAllDependencies(bundleUrl);
            if (dependencies != null && dependencies.Length > 0)
            {
                bundle.dependencies = new ABundle[dependencies.Length];
                for (int i = 0; i < dependencies.Length; i++)
                {
                    string dependency = dependencies[i];
                    // 递归加载依赖 Bundle
                    ABundle dependencyBundle = LoadInternal(dependency, async);
                    bundle.dependencies[i] = dependencyBundle;
                }
            }
            // 引用计数初始化为 1
            bundle.AddReference();
            // 调用具体 Bundle 的加载逻辑
            bundle.Load();
            return bundle;
        }

        /// <summary>
        /// 获取 Bundle 的绝对路径
        /// 通过回调函数将相对路径转换为完整文件路径
        /// </summary>
        /// <param name="url">Bundle 的相对路径</param>
        /// <returns>Bundle 的绝对路径</returns>
        /// <exception cref="Exception">当路径回调为 null 时抛出异常</exception>
        internal string GetFileUrl(string url)
        {
            if (_getFileCallback == null)
            {
                throw new Exception($"{nameof(BundleManager)}.{nameof(GetFileUrl)}() {nameof(_getFileCallback)} is null");
            }
            
            //交给外部处理
            return _getFileCallback.Invoke(url);
        }

        /// <summary>
        /// 异步加载 Bundle
        /// </summary>
        /// <param name="bundleUrl">Bundle 路径</param>
        /// <returns>加载的 BundleAsync 对象</returns>
        public ABundle LoadAsync(string bundleUrl)
        {
            return LoadInternal(bundleUrl, true);
        }
        
        /// <summary>
        /// 每帧更新异步加载的 Bundle
        /// 检查加载进度并移除已完成的 Bundle
        /// </summary>
        public void Update()
        {
            for (int i = 0; i < _asyncList.Count; i++)
            {
                // 检查异步 Bundle 是否加载完成
                if (_asyncList[i].Update())
                {
                    // 加载完成，从异步列表中移除
                    _asyncList.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// 延迟卸载 Bundle
        /// 在 LateUpdate 中批量处理引用归零的 Bundle 卸载
        /// 确保当帧不再访问这些 Bundle
        /// </summary>
        public void LateUpdate()
        {
            if (_needUnloadList.Count == 0)
            {
                return;
            }

            while (_needUnloadList.Count > 0)
            {
                ABundle bundle = _needUnloadList.First.Value;
                _needUnloadList.RemoveFirst();
                if (bundle == null)
                {
                    continue;
                }

                // 从缓存字典中移除
                _bundleDic.Remove(bundle.url);

                // 处理未完成的异步 Bundle
                if (!bundle.done && bundle is BundleAsync)
                {
                    BundleAsync bundleAsync = bundle as BundleAsync;
                    if (_asyncList.Contains(bundleAsync))
                    {
                        _asyncList.Remove(bundleAsync);
                    }
                }
                // 调用 Bundle 的卸载逻辑
                bundle.Unload();
                // 递归卸载依赖 Bundle
                if (bundle.dependencies != null)
                {
                    for (int i = 0; i < bundle.dependencies.Length; i++)
                    {
                        ABundle temp = bundle.dependencies[i];
                        Unload(temp);
                    }
                }
            }
        }
    }
}