namespace AssetBundleFramework
{
    /// <summary>
    /// 异步资源抽象基类
    /// 继承自 AResource，添加异步加载和更新机制
    /// 用于支持非阻塞式的资源加载流程
    /// </summary>
    internal abstract class AResourceAsync : AResource
    {
        
        /// <summary>
        /// 更新异步加载状态
        /// 每帧调用以检查依赖项和自身加载进度
        /// </summary>
        /// <returns>是否加载完成，true 表示完成，false 表示仍在加载中</returns>
        internal abstract bool Update();
        
        
        /// <summary>
        /// 异步加载资源
        /// 启动资源的异步加载流程，不阻塞主线程
        /// </summary>
        internal abstract void LoadAssetAsync();

    }
}