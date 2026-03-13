using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;

namespace AssetBundleFramework
{
    /// <summary>
    /// 构建配置类，定义AssetBundle打包的核心规则和参数
    /// 实现ISupportInitialize接口，支持XML序列化和初始化验证
    /// 管理资源路径映射、打包策略、后缀过滤等关键配置信息
    /// 是整个AssetBundle构建系统的配置中心
    /// </summary>
    public class BuildSetting : ISupportInitialize
    {
        /// <summary>
        /// 项目名称标识
        /// 用于区分不同项目的构建配置，在日志和报告中显示
        /// </summary>
        [DisplayName("项目名称")]
        [XmlAttribute("ProjectName")]
        public string ProjectName { get; set; }

        /// <summary>
        /// 全局资源后缀列表
        /// 定义项目中所有需要打包的资源文件后缀
        /// 支持多种资源类型，如.prefab、.png、.mat等
        /// </summary>
        [DisplayName("后缀列表")]
        [XmlAttribute("SuffixList")]
        public List<string> SuffixList { get; set; } = new();

        /// <summary>
        /// AssetBundle构建输出根目录
        /// 所有构建产物将输出到此目录下的平台子目录中
        /// 支持相对路径和绝对路径，构建时会自动转换为绝对路径
        /// </summary>
        [DisplayName("打包文件目录文件夹")]
        [XmlAttribute("BuildRoot")]
        public string BuildRoot { get; set; }

        /// <summary>
        /// 构建项列表
        /// 定义具体的资源打包规则，包括路径、类型、后缀等详细配置
        /// 每个BuildItem代表一组相关的打包配置
        /// </summary>
        [DisplayName("打包选项")]
        [XmlElement("BuildItem")]
        public List<BuildItem> Items { get; set; } = new();

        /// <summary>
        /// 构建项字典缓存
        /// 以资源路径为键，BuildItem为值的快速查找字典
        /// 用于优化路径匹配和规则查找性能
        /// 在EndInit方法中自动构建
        /// </summary>
        [XmlIgnore] 
        public Dictionary<string, BuildItem> ItemDic = new();

        /// <summary>
        /// 初始化开始回调方法
        /// ISupportInitialize接口的一部分，在反序列化开始时调用
        /// 当前实现为空，预留用于未来扩展
        /// </summary>
        public void BeginInit()
        {
        }

        /// <summary>
        /// 初始化完成回调方法
        /// ISupportInitialize接口的一部分，在反序列化完成后执行
        /// 负责验证配置数据的有效性，处理路径标准化和构建索引
        /// 确保所有配置项都符合构建要求
        /// </summary>
        public void EndInit()
        {
            // 将构建根路径转换为绝对路径，并统一使用正斜杠
            BuildRoot = Path.GetFullPath(BuildRoot.Replace("\\", "/"));

            // 清空字典，准备重新构建索引
            ItemDic.Clear();

            // 遍历所有构建项进行验证和处理
            for (int i = 0; i < Items.Count; i++)
            {
                BuildItem item = Items[i];

                // 对于目录类型或全部类型的打包项，验证目录是否存在
                if (item.BundleType == EBundleType.All || item.BundleType == EBundleType.Directory)
                {
                    if (!Directory.Exists(item.AssetPath))
                    {
                        throw new Exception($"不存在资源路径,path:{item.AssetPath}");
                    }
                }

                // 解析后缀列表，将用 | 分隔的后缀拆分为数组
                string[] prefixes = item.Suffix.Split("|");
                for (int j = 0; j < prefixes.Length; j++)
                {
                    // 去除前后空格并添加到后缀集合中
                    string prefix = prefixes[j].Trim();
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        item.Suffixes.Add(prefix);
                    }
                }

                // 检查是否有重复的资源路径
                if (ItemDic.ContainsKey(item.AssetPath))
                {
                    throw new Exception($"重复的资源路径,path:{item.AssetPath}");
                }

                // 将构建项添加到字典中，以资源路径为键
                ItemDic.Add(item.AssetPath, item);
            }
        }

        /// <summary>
        /// 收集所有需要打包的文件路径集合
        /// 主要功能：分析打包配置规则，处理路径包含关系，递归收集符合条件的所有资源文件
        /// 自动处理路径标准化、去重和过滤逻辑
        /// 是AssetBundle构建流程的第一步核心操作
        /// </summary>
        /// <returns>去重后的完整文件路径HashSet集合</returns>
        public HashSet<string> Collect()
        {
            //获取进度条显示的进度范围
            float min = Builder.collectRuleFileProgress.x; //最小进度值
            float max = Builder.collectRuleFileProgress.y; //最大进度值
            EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "正在搜集打包规则资源", min);

            //第一步：处理路径包含关系，避免重复打包
            //算法：如果路径A包含路径B，则在打包A时需要忽略B
            for (int i = 0; i < Items.Count; i++)
            {
                BuildItem item_i = Items[i];
                //只处理Direct类型的资源（直接指定的资源路径）
                if (item_i.ResourceType != EResourceType.Direct)
                {
                    continue;
                }

                //检查当前项与其他项的路径包含关系
                for (int j = 0; j < Items.Count; j++)
                {
                    BuildItem item_j = Items[j];
                    //确保不是同一个项，且都是Direct类型
                    if (i != j && item_j.ResourceType == EResourceType.Direct)
                    {
                        //如果item_j的路径被item_i的路径包含
                        if (item_j.AssetPath.StartsWith(item_i.AssetPath, StringComparison.InvariantCulture))
                        {
                            //将被包含的路径添加到忽略列表中
                            item_i.IgnorePaths.Add(item_j.AssetPath);
                        }
                    }
                }
            }

            //第二步：收集所有符合条件的文件
            HashSet<string> files = new HashSet<string>(); //使用HashSet自动去重
            
            //遍历所有打包项
            for (int i = 0; i < Items.Count; i++)
            {
                BuildItem item = Items[i];

                //更新进度条显示 - 使用线性插值计算当前进度
                //注意：这里存在除零风险，当Items.Count<=1时可能出现问题
                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包规则资源",
                    min + (max - min) * ((float)i / (Items.Count - 1)));

                //只处理Direct类型的资源
                if (item.ResourceType != EResourceType.Direct)
                {
                    continue;
                }
                
                //获取指定路径下的所有文件
                List<string> tempFiles = Builder.GetFiles(item.AssetPath, null, item.Suffixes.ToArray());

                //过滤并添加有效文件
                for (int j = 0; j < tempFiles.Count; j++)
                {
                    string file = tempFiles[j];
                    //检查文件是否在忽略列表中
                    if (IsIgnore(item.IgnorePaths, file))
                    {
                        continue; //跳过被忽略的文件
                    }

                    files.Add(file); //添加到结果集合中
                }

                //更新第二个进度条
                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "收集打包设置资源", (float)(i + 1) / Items.Count);
            }

            return files;
        }

        /// <summary>
        /// 检查文件是否在忽略路径列表中
        /// 使用前缀匹配算法判断文件路径是否应该被忽略
        /// 主要用于处理路径包含关系，避免重复打包或打包冲突
        /// </summary>
        /// <param name="ignoreList">需要检查的忽略路径列表，通常是上级目录路径</param>
        /// <param name="file">要检查的文件完整路径</param>
        /// <returns>文件是否在忽略列表中，true表示需要忽略该文件，false表示可以正常处理</returns>
        public bool IsIgnore(List<string> ignoreList, string file)
        {
            for (int i = 0; i < ignoreList.Count; i++)
            {
                string ignorePath = ignoreList[i];
                if (string.IsNullOrEmpty(ignorePath))
                {
                    continue;
                }

                if (file.StartsWith(ignorePath, StringComparison.InvariantCulture))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 根据资源URL查找最匹配的构建项配置
        /// 使用最长前缀匹配算法，确保路径更具体的规则具有更高优先级
        /// 这是确定资源打包策略的核心方法
        /// </summary>
        /// <param name="assetUrl">资源的完整路径URL</param>
        /// <returns>匹配的BuildItem配置项，如果没有匹配项则返回null</returns>
        public BuildItem GetBuildItem(string assetUrl)
        {
            BuildItem item = null;
            for (int i = 0; i < Items.Count; i++)
            {
                BuildItem tempItem = Items[i];
                //检查资源路径是否以构建项路径开头（前缀匹配）
                if (assetUrl.StartsWith(tempItem.AssetPath, StringComparison.InvariantCulture))
                {
                    //找到优先级最高的rule，路径越长优先级越高
                    //这样确保更具体的路径规则覆盖通用规则
                    if (item == null || item.AssetPath.Length < tempItem.AssetPath.Length)
                    {
                        item = tempItem;
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// 根据资源URL和类型确定对应的AssetBundle名称
        /// 根据配置的打包规则（File、Directory、All）生成标准化的Bundle名称
        /// 处理依赖资源的后缀验证和名称格式化
        /// </summary>
        /// <param name="url">资源的完整路径URL</param>
        /// <param name="kvValue">资源类型（Direct或Dependency）</param>
        /// <returns>对应的AssetBundle名称，如果不符合打包规则则返回null</returns>
        /// <exception cref="Exception">当无法确定Bundle名称时抛出异常</exception>
        public string GetBundleName(string url, EResourceType kvValue)
        {
            BuildItem item = GetBuildItem(url);

            if (item == null)
            {
                return null;
            }

            string name = "";

            //对于依赖资源，需要验证文件后缀是否符合打包要求
            if (item.ResourceType == EResourceType.Dependency)
            {
                string extension = Path.GetExtension(url).ToLower();
                bool exist = false;
                for (int i = 0; i < item.Suffixes.Count; i++)
                {
                    if (item.Suffixes[i] == extension)
                    {
                        exist = true;
                    }
                }

                //如果依赖资源的后缀不在允许列表中，则不打包
                if (!exist)
                {
                    return null;
                }
            }

            //根据打包类型生成对应的Bundle名称
            switch (item.BundleType)
            {
                case EBundleType.All:
                    //All类型：使用目录路径作为Bundle名称
                    name = item.AssetPath;
                    if (item.AssetPath[^1] != '/')
                    {
                        name = item.AssetPath.Substring(0, item.AssetPath.Length - 1);
                    }

                    name = $"{name}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                case EBundleType.Directory:
                    //Directory类型：使用资源所在目录作为Bundle名称
                    name = $"{url.Substring(0, url.LastIndexOf('/'))}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                case EBundleType.File:
                    //File类型：使用完整文件路径作为Bundle名称（最细粒度）
                    name = $"{url}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                default:
                    throw new Exception($"无法获取{url}的BundleName");
            }
            item.Count++;

            return name;
        }
    }
}