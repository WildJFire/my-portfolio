namespace AssetBundleFramework
{
    /// <summary>
    /// 异步 AssetBundle 抽象基类
    /// 继承自 ABundle，添加 Update 方法用于轮询异步加载进度
    /// </summary>
    internal abstract class ABundleAsync : ABundle
    {
        /// <summary>
        /// 更新异步加载状态
        /// 每帧调用以检查加载进度和完成状态
        /// </summary>
        /// <returns>是否加载完成，true 表示完成，false 表示仍在加载中</returns>
        internal abstract bool Update();
    }
}