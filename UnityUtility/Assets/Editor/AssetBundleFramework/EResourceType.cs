namespace AssetBundleFramework
{
    /// <summary>
    /// 资源引用类型枚举
    /// 用于区分资源在打包过程中的不同角色和处理方式
    /// Direct类型资源由配置直接指定，Dependency类型资源由依赖关系推导
    /// 影响打包规则的应用逻辑和后缀验证策略
    /// </summary>
    public enum EResourceType
    {
        /// <summary>
        /// 直接资源类型
        /// 由BuildSetting配置中直接指定的资源
        /// 享有最高优先级，直接参与打包规则匹配
        /// 是构建依赖关系图的起点资源
        /// </summary>
        Direct,

        /// <summary>
        /// 依赖资源类型
        /// 通过资源引用关系自动发现的间接资源
        /// 需要通过后缀验证才能参与打包
        /// 优先级低于Direct资源，避免重复打包
        /// </summary>
        Dependency = 2,


        /// <summary>
        /// 生成资源类型
        /// 构建过程中动态生成的文件资源
        /// 如描述文件、配置文件等辅助性资源
        /// 通常不直接参与常规打包流程
        /// </summary>
        Generate,
    }
}