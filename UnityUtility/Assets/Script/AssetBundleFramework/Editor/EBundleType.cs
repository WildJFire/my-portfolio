namespace AssetBundleFramework
{
    /// <summary>
    /// AssetBundle打包粒度类型枚举
    /// 定义资源被打包的精细程度，直接影响Bundle的数量和大小
    /// File粒度最小但数量最多，All粒度最大但数量最少
    /// 需要根据项目需求和资源特点选择合适的粒度策略
    /// </summary>
    public enum EBundleType
    {
        /// <summary>
        /// 文件级打包粒度（最小颗粒度）
        /// 每个文件单独打包成一个AssetBundle
        /// 优点：更新精准，只更新变化的文件
        /// 缺点：Bundle数量多，管理复杂，可能影响加载性能
        /// 适用于：高频更新的小资源文件
        /// </summary>
        File,
        
        /// <summary>
        /// 目录级打包粒度（中等颗粒度）
        /// 同一目录下的所有资源打包成一个AssetBundle
        /// 优点：Bundle数量适中，相关资源集中管理
        /// 缺点：目录内任一文件更新都需要重新下载整个Bundle
        /// 适用于：功能模块化、关联性强的资源组
        /// </summary>
        Directory,
        
        /// <summary>
        /// 全部打包粒度（最大颗粒度）
        /// 将指定路径下的所有资源打包成一个AssetBundle
        /// 优点：Bundle数量最少，管理简单
        /// 缺点：任何小改动都需要重新下载大文件
        /// 适用于：低频更新、整体性强的大型资源包
        /// </summary>
        All
    }
}