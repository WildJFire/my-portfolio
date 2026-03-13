using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace AssetBundleFramework
{
    /// <summary>
    /// 构建项配置类，定义单个资源或目录的打包规则
    /// 包含资源路径、打包类型、资源类型、后缀过滤等详细配置
    /// 是BuildSetting中Items列表的基本组成单元
    /// 支持XML序列化，可通过配置文件进行批量管理
    /// </summary>
    public class BuildItem
    {
        /// <summary>
        /// AssetBundle打包粒度类型
        /// 定义资源被打包的精细程度：File（文件级）、Directory（目录级）或All（全部）
        /// 影响最终生成的Bundle数量和大小分布
        /// </summary>
        [DisplayName("ab颗粒类型")]
        [XmlAttribute("BundleType")]
        public EBundleType BundleType { get; set; } = EBundleType.File;
        
        /// <summary>
        /// 资源引用类型
        /// 区分Direct（直接指定的资源）和Dependency（间接依赖的资源）
        /// 影响打包规则的应用方式和后缀验证逻辑
        /// </summary>
        [DisplayName("资源类型")]
        [XmlAttribute("ResourceType")]
        public EResourceType ResourceType { get; set; } = EResourceType.Direct;
        
        /// <summary>
        /// 资源路径配置
        /// 可以是具体文件路径或目录路径，支持相对路径
        /// 作为路径匹配的基础，决定哪些资源受此规则影响
        /// </summary>
        [DisplayName("资源目录")]
        [XmlAttribute("AssetPath")]
        public string AssetPath { get; set; }

        /// <summary>
        /// 资源后缀过滤配置
        /// 支持多个后缀用竖线(|)分隔，如".prefab|.png|.mat"
        /// 用于精确控制哪些类型的资源参与打包
        /// </summary>
        [DisplayName("资源后缀")]
        [XmlAttribute("Suffix")]
        public string Suffix { get; set; } = ".prefab";
        
        /// <summary>
        /// 忽略路径列表
        /// 运行时动态生成，存储被当前规则包含但需要排除的子路径
        /// 主要用于解决路径包含关系导致的重复打包问题
        /// </summary>
        [XmlIgnore]
        public List<string> IgnorePaths { get; set; } = new ();
        
        /// <summary>
        /// 解析后的后缀数组
        /// 运行时从Suffix字符串解析得到，提高匹配效率
        /// 去除空格并标准化格式，便于快速查找
        /// </summary>
        [XmlIgnore]
        public List<string> Suffixes { get; set; } = new ();
        /// <summary>
        /// 使用计数器
        /// 记录此构建项在打包过程中被应用的次数
        /// 主要用于统计和调试目的，帮助分析打包策略效果
        /// </summary>
        [XmlIgnore]
        public int Count { get; set; }
    }
}