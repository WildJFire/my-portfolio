using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AssetBundleFramework
{
    /// <summary>
    /// 资源管理器核心类
    /// 负责资源的加载、卸载、引用计数和生命周期管理
    /// 提供同步和异步两种加载方式，支持编辑器模式和真机模式
    /// 采用单例模式，全局唯一实例
    /// </summary>
    public class ResourceManager : CommonUtility.Singleton<ResourceManager>, IResourceManager
    {
        /// <summary>
        /// 主清单 AssetBundle 文件名常量
        /// 包含资源配置、Bundle 映射和依赖关系的核心描述文件
        /// </summary>
        private const string MANIFEST_BUNDLE = "manifest.ab";
        
        /// <summary>
        /// 资源描述文件在 Manifest 中的路径
        /// 存储资源路径到 ID 的映射关系
        /// </summary>
        private const string RESOURCE_ASSET_NAME = "Assets/Temp/Resource.bytes";
        
        /// <summary>
        /// Bundle 描述文件在 Manifest 中的路径
        /// 存储 Bundle 名称到资源列表的映射
        /// </summary>
        private const string BUNDLE_ASSET_NAME = "Assets/Temp/Bundle.bytes";
        
        /// <summary>
        /// 依赖描述文件在 Manifest 中的路径
        /// 存储资源间的依赖关系
        /// </summary>
        private const string DEPENDENCY_ASSET_NAME = "Assets/Temp/Dependency.bytes";

        /// <summary>
        /// 资源到 Bundle 的映射字典
        /// 多个资源可能对应同一个 Bundle
        /// key=资源路径，value=该资源所在的 Bundle 路径
        /// 用于快速查找资源对应的 Bundle
        /// </summary>
        internal Dictionary<string, string> ResourceBundleDict = new();

        /// <summary>
        /// 资源依赖关系字典
        /// 多个资源可能依赖同一个资源
        /// key=资源路径，value=该资源依赖的所有资源路径列表
        /// 用于加载资源时自动加载其依赖
        /// </summary>
        internal Dictionary<string, List<string>> ResourceDependencyDict = new();

        /// <summary>
        /// 已加载资源缓存字典
        /// key=资源路径，value=已加载的资源对象
        /// 避免重复加载，实现资源共享
        /// </summary>
        private Dictionary<string, AResource> _resourceDic = new();

        /// <summary>
        /// 待释放资源链表
        /// 存储引用计数归零但尚未卸载的资源
        /// 延迟卸载以确保安全
        /// </summary>
        private LinkedList<AResource> _needUnloadList = new();

        /// <summary>
        /// 异步加载资源列表
        /// 存储正在异步加载中的资源对象
        /// 每帧 Update 检查其加载进度
        /// </summary>
        private List<AResourceAsync> _asyncList = new();

        /// <summary>
        /// 是否使用编辑器模式
        /// true=使用 AssetDatabase 直接加载，false=从 AssetBundle 加载
        /// 编辑器模式下不依赖配置文件
        /// </summary>
        private bool _editor;

        #region 加载相关
        /// <summary>
        /// 内部加载资源的核心方法
        /// 处理资源缓存、依赖加载和引用计数管理
        /// </summary>
        /// <param name="url">资源路径</param>
        /// <param name="async">是否异步加载</param>
        /// <param name="dependency">是否为依赖资源（true 时不触发完成回调）</param>
        /// <returns>加载的资源对象</returns>
        private AResource LoadInternal(string url, bool async, bool dependency)
        {
            AResource resource = null;
            // 尝试从缓存字典中获取已加载的资源
            if (_resourceDic.TryGetValue(url, out resource))
            {
                //从缓存中找到，如果引用已归零则从待卸载列表移除
                if (resource.Reference == 0)
                {
                    _needUnloadList.Remove(resource);
                }

                // 引用计数加 1
                resource.AddReference();
                return resource;
            }

            // 缓存未命中，创建新的资源对象
            if (_editor)
            {
                // 编辑器模式使用 EditorResource
                resource = new EditorResource();
            }
            else if (async)
            {
                // 异步模式创建 ResourceAsync
                ResourceAsync resourceAsync = new ResourceAsync();
                _asyncList.Add(resourceAsync);
                resource = resourceAsync;
            }
            else
            {
                // 同步模式创建 Resource
                resource = new Resource();
            }

            
            // 设置资源 URL 并添加到缓存字典
            resource.url = url;
            _resourceDic.Add(url, resource);
            
            //加载依赖
            ResourceDependencyDict.TryGetValue(url, out List<string> dependencies);
            if (dependencies != null && dependencies.Count > 0)
            {
                // 初始化依赖数组
                resource.dependencies = new AResource[dependencies.Count];
                for (int i = 0; i < dependencies.Count; i++)
                {
                    string dependencyUrl = dependencies[i];
                    // 递归加载依赖资源（dependency=true 表示这是依赖资源，不触发完成回调）
                    AResource dependencyresource = LoadInternal(dependencyUrl, async, true);
                    resource.dependencies[i] = dependencyresource;
                }
            }
            // 引用计数初始化为 1
            resource.AddReference();
            // 调用具体资源的加载逻辑
            resource.Load();

            return resource;
        }

        /// <summary>
        /// 使用回调方式加载资源
        /// 支持同步和异步加载，加载完成后自动触发回调
        /// </summary>
        /// <param name="url">资源路径</param>
        /// <param name="async">是否异步加载</param>
        /// <param name="callback">加载完成的回调函数，参数为加载的资源对象</param>
        public void LoadWithCallback(string url, bool async, Action<IResource> callback)
        {
            // 调用内部加载方法获取资源对象
            AResource resource = LoadInternal(url, async, false);
            // 已加载完成立即回调
            if (resource.done)
            {
                callback?.Invoke(resource);
            }
            else
            {
                // 未完成则注册回调，完成后触发
                resource.FinishedCallback += callback;
            }
        }
        
        public IResource Load(string url, bool async)
        {
            return LoadInternal(url, async, false);
        }

        public ResourceAwaiter LoadWithAwaiter(string url)
        {
            AResource resource  = LoadInternal(url, true, false);
            if (resource.done)
            {
                if (resource.Awaiter == null)
                {
                    resource.Awaiter = new();
                    resource.Awaiter.SetResult(resource);
                }
                return resource.Awaiter;
            }
           
            if (resource.Awaiter == null)
                resource.Awaiter = new ResourceAwaiter();

            return resource.Awaiter;
        }

        #endregion
        
        /// <summary>
        /// 初始化资源管理系统
        /// 加载配置文件并构建资源映射关系，仅在非编辑器模式下执行
        /// </summary>
        /// <param name="platform">目标平台名称（Windows、Android、iOS 等）</param>
        /// <param name="getFileCallback">获取资源真实路径的回调函数</param>
        /// <param name="editor">是否使用编辑器模式，true 时直接从 AssetDatabase 加载</param>
        /// <param name="offset">Bundle 加载偏移量，用于特殊需求</param>
        public void Initialize(string platform, Func<string, string> getFileCallback, bool editor, ulong offset)
        {
            _editor = editor;
            // 编辑器模式下不需要加载配置文件，直接返回
            if (editor)
            {
                return;
            }

            // 初始化 Bundle 管理器
            BundleManager.Instance.Initialize(platform, getFileCallback, offset);

            string assetBundleManifest = getFileCallback.Invoke(MANIFEST_BUNDLE);
            AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(assetBundleManifest, 0, offset);

            // 从 Manifest Bundle 中加载资源配置信息
            TextAsset resourceTextAsset = manifestAssetBundle.LoadAsset<TextAsset>(RESOURCE_ASSET_NAME);
            TextAsset bundleTextAsset = manifestAssetBundle.LoadAsset<TextAsset>(BUNDLE_ASSET_NAME);
            TextAsset dependencyTextAsset = manifestAssetBundle.LoadAsset<TextAsset>(DEPENDENCY_ASSET_NAME);


            byte[] resourceBytes = resourceTextAsset.bytes;
            byte[] bundleBytes = bundleTextAsset.bytes;
            byte[] dependencyBytes = dependencyTextAsset.bytes;

            // 卸载 Manifest Bundle 释放内存
            manifestAssetBundle.Unload(true);
            manifestAssetBundle = null;

            Dictionary<ushort, string> assetsDict = new Dictionary<ushort, string>();

            #region 读资源信息

            {
                // 读取资源列表，建立 ID 到路径的映射
                MemoryStream resourceSB = new MemoryStream(resourceBytes);
                BinaryReader resourceBR = new BinaryReader(resourceSB);
                ushort resourceCount = resourceBR.ReadUInt16();
                for (int i = 0; i < resourceCount; i++)
                {
                    string assetUrl = resourceBR.ReadString();
                    assetsDict.Add((ushort)i, assetUrl);
                }
            }

            #endregion
            
            #region 读 bundle 信息
            
            {
                ResourceBundleDict.Clear();
                MemoryStream bundleSB = new MemoryStream(bundleBytes);
                BinaryReader bundleBR = new BinaryReader(bundleSB);
                ushort bundleCount = bundleBR.ReadUInt16();
                for (int i = 0; i < bundleCount; i++)
                {
                    string bundleUrl = bundleBR.ReadString();
                    ushort resourceCount = bundleBR.ReadUInt16();
                    for (int j = 0; j < resourceCount; j++)
                    {
                        ushort id = bundleBR.ReadUInt16();
                        string assetUrl = assetsDict[id];
                        // 建立资源到 Bundle 的映射
                        ResourceBundleDict.Add(assetUrl, bundleUrl);
                    }
                }
            }

            #endregion

            #region 读依赖信息

            {
                ResourceDependencyDict.Clear();
                MemoryStream dependencySB = new MemoryStream(dependencyBytes);
                BinaryReader dependencyBR = new BinaryReader(dependencySB);
                ushort dependencyCount = dependencyBR.ReadUInt16();
                //获得依赖链个数
                for (int i = 0; i < dependencyCount; i++)
                {
                    ushort resourceDependencyCount = dependencyBR.ReadUInt16();
                    ushort assetId = dependencyBR.ReadUInt16();
                    string assetUrl = assetsDict[assetId];
                    List<string> dependencyList = new List<string>(resourceDependencyCount);
                    for (int j = 1; j < resourceDependencyCount; j++)
                    {
                        ushort dependencyAssetId = dependencyBR.ReadUInt16();
                        string dependencyUrl = assetsDict[dependencyAssetId];
                        dependencyList.Add(dependencyUrl);
                    }

                    ResourceDependencyDict.Add(assetUrl, dependencyList);
                }
            }

            #endregion
        }


        /// <summary>
        /// 每帧更新异步加载状态
        /// 检查所有异步资源的加载进度，移除已完成的资源
        /// </summary>
        public void Update()
        {
            // 更新 Bundle 管理器的异步加载状态
            BundleManager.Instance.Update();
            for (int i = 0; i < _asyncList.Count; i++)
            {
                AResourceAsync resourceAsync = _asyncList[i];
                // 检查异步资源是否加载完成
                if (resourceAsync.Update())
                {
                    // 加载完成，从异步列表中移除
                    _asyncList.RemoveAt(i);
                    i--;

                    if (resourceAsync.Awaiter != null)
                    {
                        resourceAsync.Awaiter.SetResult(resourceAsync);
                    }
                }
            }
        }

        /// <summary>
        /// 延迟卸载资源
        /// 在 LateUpdate 中批量处理引用归零的资源卸载
        /// 确保当帧不再访问这些资源
        /// </summary>
        public void LateUpdate()
        {
            if (_needUnloadList.Count != 0)
            {
                while (_needUnloadList.Count > 0)
                {
                    AResource resource = _needUnloadList.First.Value;
                    _needUnloadList.Remove(resource);
                    if (resource == null)
                    {
                        continue;
                    }
                    // 从缓存字典中移除
                    _resourceDic.Remove(resource.url);
                    // 调用资源的卸载逻辑
                    resource.Unload();
                    // 递归卸载依赖资源
                    if (resource.dependencies != null)
                    {
                        for (int i = 0; i < resource.dependencies.Length; i++)
                        {
                            AResource dependency = resource.dependencies[i];
                            Unload(dependency);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 卸载指定资源
        /// 减少引用计数，当计数归零时加入待卸载队列
        /// </summary>
        /// <param name="resource">要卸载的资源对象</param>
        /// <exception cref="ArgumentException">当资源为 null 时抛出异常</exception>
        public void Unload(IResource resource)
        {
            if (resource == null)
            {
                throw new ArgumentException($"{nameof(ResourceManager)}.{nameof(Unload)}() {nameof(resource)} is null");
            }

            AResource aResource = resource as AResource;
            // 引用计数减 1
            aResource.ReduceReference();
            // 引用归零时加入待卸载队列
            if (aResource.Reference == 0)
            {
                WillUnload(aResource);
            }
        }

        /// <summary>
        /// 标记资源为待卸载状态
        /// 添加到待卸载链表，等待 LateUpdate 统一处理
        /// </summary>
        /// <param name="bundle">待卸载的资源</param>
        private void WillUnload(AResource bundle)
        {
            _needUnloadList.AddLast(bundle);
        }

    }
}