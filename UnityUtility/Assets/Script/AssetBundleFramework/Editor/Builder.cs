using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using CommonUtility;
using HotUpdate;

namespace AssetBundleFramework
{
    public static class Builder
    {
        #region 进度条配置

        /// <summary>
        /// 收集打包规则文件进度范围：0% - 20%
        /// </summary>
        public static readonly Vector2 collectRuleFileProgress = new(0, 0.2f);

        /// <summary>
        /// 获取资源依赖关系进度范围：20% - 40%
        /// </summary>
        public static readonly Vector2 getDependencyProgress = new(0.2f, 0.4f);

        /// <summary>
        /// 收集Bundle信息进度范围：40% - 50%
        /// </summary>
        public static readonly Vector2 collectBundleInfoProgress = new(0.4f, 0.5f);

        /// <summary>
        /// 生成构建信息进度范围：50% - 60%
        /// </summary>
        public static readonly Vector2 generateBuildInfoProgress = new(0.5f, 0.6f);

        /// <summary>
        /// 构建AssetBundle进度范围：60% - 70%
        /// </summary>
        public static readonly Vector2 buildBundleProgress = new(0.6f, 0.7f);

        /// <summary>
        /// 清理多余文件进度范围：70% - 90%
        /// </summary>
        public static readonly Vector2 clearBundleProgress = new(0.7f, 0.9f);

        public static readonly Vector2 buildManifestProgress = new(0.9f, 1);

        #endregion

        #region 性能分析器

        /// <summary>
        /// 根构建分析器：监控整个AssetBundle构建过程
        /// </summary>
        private static readonly Profiler _buildProfiler = new(nameof(Builder));
        
        /// <summary>
        /// 构建版本文件
        /// </summary>
        private static readonly Profiler _buildVersionFileProfiler = _buildProfiler.CreateChild(nameof(BuildVersionFile));

        /// <summary>
        /// 加载构建设置分析器
        /// </summary>
        private static readonly Profiler _loadBuildSetingProfiler = _buildProfiler.CreateChild(nameof(LoadSetting));

        /// <summary>
        /// 平台切换分析器
        /// </summary>
        private static readonly Profiler _switchPlatformProfiler = _buildProfiler.CreateChild(nameof(SwitchPlatform));

        /// <summary>
        /// 资源收集主分析器
        /// </summary>
        private static readonly Profiler _collectProfiler = _buildProfiler.CreateChild(nameof(Collect));

        /// <summary>
        /// 收集构建设置文件分析器
        /// </summary>
        private static readonly Profiler _CollectBuildSettingFileProfiler =
            _buildProfiler.CreateChild("CollectBuildSettingFile");

        /// <summary>
        /// 收集依赖关系分析器
        /// </summary>
        private static readonly Profiler _CollectDependencyProfiler =
            _buildProfiler.CreateChild(nameof(CollectDependency));

        /// <summary>
        /// 收集Bundle信息分析器
        /// </summary>
        private static readonly Profiler _CollectBundleProfiler = _buildProfiler.CreateChild(nameof(CollectBundle));

        /// <summary>
        /// 生成清单文件分析器
        /// </summary>
        private static readonly Profiler _generateManifestProfiler =
            _collectProfiler.CreateChild(nameof(GenerateManifest));

        /// <summary>
        /// 构建Bundle分析器
        /// </summary>
        private static readonly Profiler _buildBundleProfiler = _collectProfiler.CreateChild(nameof(BuildBundle));

        /// <summary>
        /// 清理Bundle分析器
        /// </summary>
        private static readonly Profiler _clearBundleProfiler = _collectProfiler.CreateChild(nameof(ClearAssetBundle));

        private static readonly Profiler _buildManifestProfiler = _collectProfiler.CreateChild(nameof(BuildManifest));

        #endregion

        /// <summary>
        /// 当前构建配置实例
        /// 存储从XML配置文件加载的打包设置信息
        /// 包含资源路径、打包规则、后缀配置等关键参数
        /// </summary>
        public static BuildSetting buildSetting { get; private set; }

        /// <summary>
        /// AssetBundle文件标准后缀名
        /// 用于标识AssetBundle文件类型，便于文件识别和管理
        /// </summary>
        public const string BUNDLE_SUFFIX = ".ab";
        /// <summary>
        /// AssetBundle清单文件后缀名
        /// Unity自动生成的元数据文件，包含Bundle的依赖关系和哈希信息
        /// </summary>
        public const string BUNDLE_MANIFEST_SUFFIX = ".manifest";
        
        /// <summary>
        /// 主清单文件名称
        /// 用于存储资源配置、Bundle映射和依赖关系的核心描述文件
        /// </summary>
        public const string MANIFEST = "manifest";

        /// <summary>
        /// 并行处理配置选项
        /// 设置最大并发度为CPU核心数的2倍，平衡性能和系统负载
        /// 用于文件清理、资源处理等可并行化的操作
        /// </summary>
        public static readonly ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2
        };


        /// <summary>
        /// AssetBundle构建选项配置
        /// 定义了打包过程中的压缩算法、构建模式和其他高级选项
        /// 确保构建的一致性和可预测性
        /// </summary>
        public readonly static BuildAssetBundleOptions buildAssetBundleOptions =
            BuildAssetBundleOptions.ChunkBasedCompression | // 使用LZ4压缩算法，提供良好的压缩比和解压速度
            BuildAssetBundleOptions.DeterministicAssetBundle | // 确保相同内容产生相同的Bundle，便于增量更新
            BuildAssetBundleOptions.StrictMode | // 严格模式，遇到警告即视为错误
            BuildAssetBundleOptions.DisableLoadAssetByFileName | // 禁用通过文件名直接加载资源，必须通过Bundle加载
            BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension; // 禁用带扩展名的文件名加载


        #region 路径配置

        /// <summary>
        /// 最终AssetBundle输出目录路径
        /// 存放构建完成的AssetBundle文件，按平台分类存储
        /// 路径格式：{buildRoot}/{platform}/
        /// </summary>
        public static string BuildPath { get; set; }

        /// <summary>
        /// 临时工作目录路径
        /// 用于存放构建过程中的临时文件，包括资源配置、Bundle映射和依赖关系文件
        /// 构建完成后会自动清理这些临时文件
        /// </summary>
        public readonly static string tempPath =
            Path.GetFullPath(Path.Combine(Application.dataPath, "Temp")).Replace("\\", "/");

        /// <summary>
        /// 打包配置文件路径
        /// XML格式的构建配置文件，定义了资源打包规则、路径映射和构建选项
        /// 默认位于项目根目录下的BuildSetting.xml文件
        /// </summary>
        public readonly static string buildSettingPath = Path.GetFullPath("BuildSetting.xml").Replace("\\", "/");

        /// <summary>
        /// 临时构建目录路径
        /// Unity BuildPipeline的工作目录，在此处生成临时的AssetBundle文件
        /// 构建完成后会自动清理此目录中的所有文件
        /// </summary>
        public static readonly string tempBuildPath =
            Path.GetFullPath(Path.Combine(Application.dataPath, "../TempBuild")).Replace("\\", "/");

        /// <summary>
        /// 资源描述文件路径（文本格式）
        /// 存储资源路径到唯一ID的映射关系，便于人类阅读和调试
        /// 格式：每行包含ID和资源路径，用制表符分隔
        /// </summary>
        public static readonly string resourcePath_Text = $"{tempPath}/Resource.txt";

        /// <summary>
        /// 资源描述文件路径（二进制格式）
        /// 存储资源路径到唯一ID的映射关系，优化存储空间和加载性能
        /// 格式：ushort(资源总数) + string[](资源路径数组)
        /// </summary>
        public static string resourcePath_Binary = $"{tempPath}/Resource.bytes";

        /// <summary>
        /// Bundle描述文件路径（文本格式）
        /// 存储AssetBundle名称到包含资源列表的映射关系
        /// 格式：Bundle名称 + 资源路径列表（缩进显示层次结构）
        /// </summary>
        public static readonly string bundlePath_Text = $"{tempPath}/Bundle.txt";

        /// <summary>
        /// Bundle描述文件路径（二进制格式）
        /// 存储AssetBundle名称到包含资源ID列表的映射关系
        /// 格式：ushort(Bundle总数) + [string(Bundle名称) + ushort(资源数) + ushort[](资源ID数组)]
        /// </summary>
        public static readonly string bundlePath_Binary = $"{tempPath}/Bundle.bytes";

        /// <summary>
        /// 资源依赖描述文件路径（文本格式）
        /// 存储资源间的依赖关系，便于调试和分析
        /// 格式：每行第一个为资源路径，后续为该资源依赖的所有资源路径
        /// </summary>
        public static readonly string dependencyPath_Text = $"{tempPath}/Dependency.txt";

        /// <summary>
        /// 资源依赖描述文件路径（二进制格式）
        /// 存储资源间的依赖关系，优化存储效率
        /// 格式：ushort(依赖链总数) + [ushort(链长度) + ushort[](资源ID链)]
        /// </summary>
        public static readonly string dependencyPath_Binary = $"{tempPath}/Dependency.bytes";

        #endregion


        /// <summary>
        /// 当前构建目标平台常量
        /// 根据Unity编辑器的编译宏定义自动选择对应的目标平台
        /// 支持Windows、iOS、Android等主流平台
        /// 影响输出目录结构和平台特定的构建选项
        /// </summary>
#if UNITY_IOS
        private const string PLATFORM = "iOS";
#elif UNITY_ANDROID
        private const string PLATFORM = "Android";
#else
        private const string PLATFORM = "Windows";
#endif

        #region Build MenuItem

        /// <summary>
        /// Unity编辑器菜单项：构建Windows平台AssetBundle
        /// 提供一键式构建功能，简化开发者操作流程
        /// </summary>
        [MenuItem("Tool/ResourceBuild/Build")]
        public static void BuildWindows()
        {
            Build();
        }

        #endregion

        /// <summary>
        /// 切换Unity编辑器的构建目标平台
        /// 根据当前配置的PLATFORM常量，自动切换到对应的构建平台
        /// 确保后续的AssetBundle构建针对正确的目标平台
        /// </summary>
        public static void SwitchPlatform()
        {
            string platform = PLATFORM;

            switch (platform)
            {
                case "Windows":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                        BuildTarget.StandaloneWindows64);
                    break;
                case "iOS":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                    break;
                case "Android":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                    break;
            }
        }

        /// <summary>
        /// 加载并初始化构建配置文件
        /// 从指定路径读取XML格式的构建配置，验证配置有效性
        /// 并根据配置计算最终的构建输出路径
        /// </summary>
        /// <param name="path">构建配置文件的完整路径</param>
        /// <returns>加载成功的BuildSetting配置对象</returns>
        /// <exception cref="Exception">当配置文件加载失败或配置无效时抛出异常</exception>
        public static BuildSetting LoadSetting(string path)
        {
            buildSetting = XmlUtility.Read<BuildSetting>(path);
            if (buildSetting == null)
            {
                throw new Exception($"load BuildSetting error,path:{path}");
            }

            (buildSetting as ISupportInitialize)?.EndInit();

            BuildPath = Path.GetFullPath(buildSetting.BuildRoot).Replace("\\", "/");
            if (BuildPath.Length > 0 && BuildPath[^1] != '/')
            {
                BuildPath += '/';
            }

            BuildPath += $"{PLATFORM}/";
            return buildSetting;
        }

        /// <summary>
        /// AssetBundle构建系统的主入口和协调中枢
        /// 按照严格的五步流程执行完整构建：平台切换→配置加载→资源收集→Bundle构建→文件清理→清单生成
        /// 集成多层性能分析器，实时监控各阶段执行效率
        /// 提供详细的进度反馈和最终的构建报告
        /// 是整个AssetBundle自动化构建流程的控制中心
        /// </summary>
        private static void Build()
        {
            
            //启动根性能分析器
            _buildProfiler.Start();

            //第一步：切换到目标构建平台
            _switchPlatformProfiler.Start();
            SwitchPlatform();
            _switchPlatformProfiler.Stop();

            //第二步：加载打包配置文件
            _loadBuildSetingProfiler.Start();
            buildSetting = LoadSetting(buildSettingPath);
            _loadBuildSetingProfiler.Stop();

            //第三步：收集并分析所有需要打包的资源
            _collectProfiler.Start();
            var buildItems = Collect();
            _collectProfiler.Stop();

            //第四步：执行AssetBundle构建
            _buildBundleProfiler.Start();
            var manifest = BuildBundle(buildItems);
            _buildBundleProfiler.Stop();

            //第五步：清理构建过程中产生的多余文件
            _clearBundleProfiler.Start();
            ClearAssetBundle(BuildPath, buildItems);
            _clearBundleProfiler.Stop();

            //第六步：打包manifest文件
            _buildManifestProfiler.Start();
            BuildManifest();
            _buildManifestProfiler.Stop();
            
            _buildVersionFileProfiler.Start();
            BuildVersionFile();
            _buildVersionFileProfiler.Stop();

            //停止根性能分析器并输出结果
            _buildProfiler.Stop();
            EditorUtility.ClearProgressBar();
            Debug.Log($"打包结果:{_buildProfiler}");
        }

        private static void BuildVersionFile()
        {
            if (!Directory.Exists(BuildPath))
            {
                throw new Exception($"不存在构建目录, path:{BuildPath}");
            }

            StringBuilder sb = new();
            DirectoryInfo folder = new(BuildPath);
            FileInfo[] files = folder.GetFiles("*" , SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string path = file.FullName;
                string abName = path.Substring(path.IndexOf("AssetBundle"));
                string suffix = path.Substring(path.LastIndexOf('.') + 1);
                string md5 = MD5Manager.Instance.GetABPackEncryptVersion(path);
                string size = Mathf.Ceil(file.Length / 1024.0f).ToString();
                sb.AppendLine(ABUtil.GetABPackVersionStr(abName, md5, size));
            }

            string outName = Path.Combine(BuildPath, ABUtil.sABVersionName);
            IOUtils.CreatTextFile(outName, sb.ToString());
        }

        /// <summary>
        /// 构建主清单AssetBundle文件
        /// 将资源配置、Bundle映射和依赖关系三个核心描述文件打包成单独的AssetBundle
        /// 便于运行时统一加载和管理所有资源信息
        /// </summary>
        private static void BuildManifest()
        {
            //获取构建清单阶段的进度条显示范围
            float min = buildManifestProgress.x; //最小进度值
            float max = buildManifestProgress.y; //最大进度值

            //显示构建清单开始进度条
            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "将manifest打包成AssetBundle", min);

            //确保临时构建目录存在
            if (!Directory.Exists(tempBuildPath))
            {
                Directory.CreateDirectory(tempBuildPath);
            }

            //计算相对于项目根目录的相对路径前缀
            string prefix = Application.dataPath.Replace("/Assets", "/").Replace("\\", "/");
            
            //配置主清单AssetBundle的构建信息
            AssetBundleBuild manifest = new AssetBundleBuild();
            manifest.assetBundleName = $"{MANIFEST}{BUNDLE_SUFFIX}";
            manifest.assetNames = new string[3]
            {
                resourcePath_Binary.Replace(prefix, ""),  // 资源映射文件
                bundlePath_Binary.Replace(prefix, ""),    // Bundle配置文件
                dependencyPath_Binary.Replace(prefix, ""), // 依赖关系文件
            };

            //更新进度条到50%
            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "将manifest打包成AssetBundle",
                min + 0.5f * (max - min));

            //执行AssetBundle构建
            AssetBundleManifest assetBundleManifest = BuildPipeline.BuildAssetBundles(tempBuildPath, new[] { manifest },
                buildAssetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

            //复制生成的主清单文件到最终输出目录
            if (assetBundleManifest)
            {
                string manifestPath = $"{tempBuildPath}/{MANIFEST}{BUNDLE_SUFFIX}";
                string target = $"{BuildPath}/{MANIFEST}{BUNDLE_SUFFIX}";
                if (File.Exists(manifestPath))
                {
                    File.Copy(manifestPath, target);
                }
            }

            //清理临时构建目录
            if (!Directory.Exists(tempBuildPath))
            {
                Directory.Delete(tempBuildPath,  true);
            }

            //更新进度条并清理
            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "将manifest打包成AssetBundle", max);
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 清理构建目录中多余的AssetBundle文件
        /// 保留本次构建生成的有效文件，删除旧版本或其他无关文件
        /// 使用并行处理提高清理效率
        /// </summary>
        /// <param name="path">构建输出目录路径</param>
        /// <param name="bundleDic">本次构建的Bundle字典，用于确定需要保留的文件</param>
        private static void ClearAssetBundle(string path, Dictionary<string, List<string>> bundleDic)
        {
            //获取清理阶段的进度条显示范围
            float min = clearBundleProgress.x; //最小进度值
            float max = clearBundleProgress.y; //最大进度值

            //显示清理开始进度条
            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "清理多余的AssetBundle", min);

            //获取目录下所有文件
            List<string> fileList = GetFiles(path, null, null);
            HashSet<string> fileSet = new HashSet<string>(fileList);

            //从文件集合中移除本次构建需要保留的文件
            foreach (var bundle in bundleDic.Keys)
            {
                //保留Bundle文件
                fileSet.Remove($"{path}{bundle}");
                //保留对应的manifest文件
                fileSet.Remove($"{path}{bundle}{BUNDLE_MANIFEST_SUFFIX}");
            }

            //保留平台主Bundle文件
            fileSet.Remove($"{path}{PLATFORM}");
            fileSet.Remove($"{path}{PLATFORM}{BUNDLE_MANIFEST_SUFFIX}");

            //并行删除剩余的多余文件
            Parallel.ForEach(fileSet, parallelOptions, File.Delete);

            //更新进度条并清理
            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "清理多余的AssetBundle", max);
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 执行AssetBundle的实际构建过程
        /// 使用Unity的BuildPipeline API，根据预定义的配置和选项进行构建
        /// 返回构建结果的清单文件，包含所有Bundle的信息
        /// </summary>
        /// <param name="bundleDic">Bundle名称到资源列表的映射字典</param>
        /// <returns>AssetBundle构建清单，包含所有生成Bundle的信息</returns>
        private static AssetBundleManifest BuildBundle(Dictionary<string, List<string>> bundleDic)
        {
            //获取构建阶段的进度条显示范围
            float min = buildBundleProgress.x; //最小进度值
            float max = buildBundleProgress.y; //最大进度值

            //显示构建开始进度条
            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "打包AssetBundle", min);

            //确保输出目录存在
            if (!Directory.Exists(BuildPath))
            {
                Directory.CreateDirectory(BuildPath);
            }
            
            //执行实际的AssetBundle构建
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                BuildPath,
                GetBuilds(bundleDic),
                buildAssetBundleOptions,
                EditorUserBuildSettings.activeBuildTarget
            );

            //更新进度条到完成状态
            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "打包AssetBundle", max);

            return manifest;
        }

        /// <summary>
        /// 将Bundle字典转换为Unity BuildPipeline所需的AssetBundleBuild数组
        /// 每个AssetBundleBuild对象定义了一个Bundle的名称和包含的资源列表
        /// 这是调用BuildPipeline.BuildAssetBundles方法的必要步骤
        /// </summary>
        /// <param name="bundleDic">Bundle名称到资源路径列表的映射字典</param>
        /// <returns>AssetBundleBuild数组，可直接用于BuildPipeline.BuildAssetBundles方法</returns>
        private static AssetBundleBuild[] GetBuilds(Dictionary<string, List<string>> bundleDic)
        {
            int index = 0;
            AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[bundleDic.Count];
            foreach (var kv in bundleDic)
            {
                assetBundleBuilds[index++] = new AssetBundleBuild()
                {
                    assetBundleName = kv.Key,
                    assetNames = kv.Value.ToArray()
                };
            }

            return assetBundleBuilds;
        }

        /// <summary>
        /// AssetBundle构建信息收集的主协调方法
        /// 按照严格的顺序执行四个核心步骤：规则收集→依赖分析→类型标记→Bundle分配
        /// 整合打包设置、资源依赖关系和Bundle分配规则，生成最终的构建配置
        /// 内置完整的性能分析和进度跟踪机制
        /// </summary>
        /// <returns>AssetBundle名称到资源路径列表的最终映射字典，用于指导后续的Bundle构建</returns>
        private static Dictionary<string, List<string>> Collect()
        {
            //第一步：搜集打包设置中指定的直接资源文件列表
            _CollectBuildSettingFileProfiler.Start();
            HashSet<string> files = buildSetting.Collect(); //获取所有Direct类型的资源路径
            _CollectBuildSettingFileProfiler.Stop();

            //第二步：分析所有资源的依赖关系
            _CollectDependencyProfiler.Start();
            Dictionary<string, List<string>> dependencyDic = CollectDependency(files); //构建资源依赖映射
            _CollectDependencyProfiler.Stop();

            //第三步：标记资源类型（Direct直接受管资源 vs Dependency依赖资源）
            Dictionary<string, EResourceType> assetDic = new Dictionary<string, EResourceType>();
            // 被打包配置直接分析的资源，标记为Direct类型
            foreach (var url in files)
            {
                assetDic.Add(url, EResourceType.Direct);
            }

            // 标记所有依赖资源为Dependency类型
            // 已存在于assetDic中的资源说明是Direct类型，优先级更高
            foreach (var url in dependencyDic.Keys)
            {
                if (!assetDic.ContainsKey(url))
                {
                    assetDic.Add(url, EResourceType.Dependency);
                }
            }

            //第四步：根据打包规则将资源分配到对应的AssetBundle中
            _CollectBundleProfiler.Start();
            Dictionary<string, List<string>> bundleDic = CollectBundle(buildSetting, assetDic, dependencyDic);
            _CollectBundleProfiler.Stop();

            //第五步：生成描述文件供后续打包使用
            _generateManifestProfiler.Start();
            GenerateManifest(assetDic, bundleDic, dependencyDic); //创建资源、Bundle和依赖的描述文件
            _generateManifestProfiler.Stop();

            return bundleDic;
        }

        /// <summary>
        /// 生成AssetBundle构建所需的三种核心描述文件
        /// 包括资源映射文件、Bundle配置文件和依赖关系文件
        /// 每种文件同时生成人类可读的文本格式和高效的二进制格式
        /// 这些文件是运行时资源管理系统的重要基础数据
        /// </summary>
        /// <param name="assetDic">资源路径到资源类型的映射字典，区分Direct和Dependency资源</param>
        /// <param name="bundleDic">AssetBundle名称到包含资源列表的映射，定义了资源的打包策略</param>
        /// <param name="dependencyDic">资源路径到其依赖资源列表的映射，用于构建资源依赖图</param>
        private static void GenerateManifest(Dictionary<string, EResourceType> assetDic,
            Dictionary<string, List<string>> bundleDic, Dictionary<string, List<string>> dependencyDic)
        {
            //获取进度条显示范围
            float min = generateBuildInfoProgress.x; //最小进度值
            float max = generateBuildInfoProgress.y; //最大进度值

            //显示初始进度条
            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成资源描述信息", min);

            //生成临时存放文件的目录
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            //创建资源ID映射字典：用于后续用ID替代字符串，节省存储空间
            Dictionary<string, ushort> assetIdDic = new Dictionary<string, ushort>();

            #region 生成资源描述信息

            {
                //清理旧的资源描述文件
                if (File.Exists(resourcePath_Text))
                    File.Delete(resourcePath_Text);

                if (File.Exists(resourcePath_Binary))
                    File.Delete(resourcePath_Binary);

                //验证资源数量是否超出ushort范围限制（65535个）
                if (assetDic.Count > ushort.MaxValue)
                {
                    EditorUtility.ClearProgressBar();
                    throw new Exception($"资源数量超出限制:{assetDic.Count}");
                }

                //准备文本和二进制两种格式的写入器
                StringBuilder resourceSb = new StringBuilder(); //文本格式构建器
                MemoryStream resourceMS = new MemoryStream(); //二进制内存流
                BinaryWriter resourceBW = new BinaryWriter(resourceMS); //二进制写入器

                //写入资源总数
                resourceBW.Write((ushort)assetDic.Count);

                //获取并排序所有资源路径
                List<string> keys = new List<string>(assetDic.Keys);
                keys.Sort();

                //为每个资源分配唯一ID并写入描述信息
                for (int i = 0; i < keys.Count; i++)
                {
                    string assetUrl = keys[i];
                    assetIdDic.Add(assetUrl, (ushort)i); //建立路径到ID的映射
                    resourceSb.AppendLine($"{i}\t{assetUrl}"); //文本格式：ID+路径
                    resourceBW.Write(assetUrl); //二进制格式：直接写入字符串
                }

                //完成写入并保存文件
                resourceMS.Flush();
                byte[] buffer = resourceMS.ToArray(); //获取实际使用的数据
                resourceBW.Close();
                File.WriteAllText(resourcePath_Text, resourceSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(resourcePath_Binary, buffer);
            }

            #endregion

            //更新进度：完成资源描述生成
            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成Bundle描述信息", min + (max - min) / 3);

            #region 创建AssetBundle描述文件

            {
                //清理旧的Bundle描述文件
                if (File.Exists(bundlePath_Text))
                    File.Delete(bundlePath_Text);

                if (File.Exists(bundlePath_Binary))
                    File.Delete(bundlePath_Binary);

                //准备Bundle描述文件的写入器
                StringBuilder bundleSb = new StringBuilder();
                MemoryStream bundleMS = new MemoryStream();
                BinaryWriter bundleBW = new BinaryWriter(bundleMS);

                //写入Bundle总数
                bundleBW.Write((ushort)bundleDic.Count);

                //遍历每个Bundle，写入其包含的资源信息
                foreach (var kv in bundleDic)
                {
                    string bundleName = kv.Key;
                    List<string> assets = kv.Value;

                    //写入Bundle名称
                    bundleBW.Write(bundleName);
                    bundleSb.AppendLine(bundleName);

                    //写入该Bundle包含的资源数量
                    bundleBW.Write((ushort)assets.Count);

                    //写入每个资源的ID（使用ID替代完整路径以节省空间）
                    for (int i = 0; i < assets.Count; i++)
                    {
                        string assetUrl = assets[i];
                        //写入资源id,用id替换字符串可以节省内存
                        ushort assetId = assetIdDic[assetUrl];
                        bundleSb.AppendLine($"\t{assetUrl}");
                        bundleBW.Write(assetId);
                    }
                }

                //完成Bundle描述文件的写入
                bundleMS.Flush();
                byte[] buffer = bundleMS.ToArray();
                bundleBW.Close();

                File.WriteAllText(bundlePath_Text, bundleSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(bundlePath_Binary, buffer);
            }

            #endregion

            //更新进度：完成Bundle描述生成
            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成依赖描述信息", min + (max - min) * 0.8f);

            #region 生成资源依赖描述文件

            {
                //清理旧的依赖描述文件
                if (File.Exists(dependencyPath_Text))
                    File.Delete(dependencyPath_Text);

                if (File.Exists(dependencyPath_Binary))
                    File.Delete(dependencyPath_Binary);

                //准备依赖描述文件的写入器
                StringBuilder dependencySB = new StringBuilder();
                MemoryStream dependencyMS = new MemoryStream();
                BinaryWriter dependencyBW = new BinaryWriter(dependencyMS);

                //用于保存资源依赖链的中间结构
                List<List<ushort>> dependencyList = new List<List<ushort>>();

                //处理每个资源的依赖关系
                foreach (var kv in dependencyDic)
                {
                    List<string> dependencyAssets = kv.Value;
                    //跳过没有依赖的资源
                    if (dependencyAssets.Count <= 0)
                    {
                        continue;
                    }

                    string assetUrl = kv.Key;

                    //创建依赖ID列表，首先添加自身ID
                    List<ushort> ids = new List<ushort>();
                    ids.Add(assetIdDic[assetUrl]);

                    //构建文本显示内容
                    string content = assetUrl;

                    //添加所有依赖资源的ID
                    for (int i = 0; i < dependencyAssets.Count; i++)
                    {
                        string dependencyAssetUrl = dependencyAssets[i];
                        ids.Add(assetIdDic[dependencyAssetUrl]);
                        content += $"\t{dependencyAssetUrl}";
                    }

                    dependencySB.AppendLine(content);

                    //验证依赖数量是否超出byte范围限制（255个）
                    if (ids.Count > byte.MaxValue)
                    {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"{assetUrl} 资源依赖数量超出限制, 当前依赖数量为{ids.Count}");
                    }

                    dependencyList.Add(ids);
                }

                //写入依赖链总数
                dependencyBW.Write((ushort)dependencyList.Count);

                //写入每条依赖链的信息
                for (int i = 0; i < dependencyList.Count; i++)
                {
                    List<ushort> ids = dependencyList[i];
                    dependencyBW.Write((ushort)ids.Count); //写入该链的长度
                    for (int j = 0; j < ids.Count; j++)
                    {
                        dependencyBW.Write(ids[j]); //写入每个ID
                    }
                }

                //完成依赖描述文件的写入
                dependencyMS.Flush();
                byte[] buffer = dependencyMS.ToArray();
                dependencyBW.Close();

                File.WriteAllText(dependencyPath_Text, dependencySB.ToString(), Encoding.UTF8);
                File.WriteAllBytes(dependencyPath_Binary, buffer);
            }

            #endregion

            //刷新资源数据库，使新生成的文件在Unity中可见
            AssetDatabase.Refresh();
            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "完成文件生成", max);
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 根据构建配置将资源分配到对应的AssetBundle中
        /// 核心功能包括：资源类型验证、打包规则应用、Bundle名称生成
        /// 确保所有资源都符合预定义的打包策略，并建立完整的Bundle映射关系
        /// 这是AssetBundle构建流程中的关键分组步骤
        /// </summary>
        /// <param name="buildSetting">构建配置对象，包含所有打包规则和策略定义</param>
        /// <param name="assetDic">资源路径到资源类型的映射字典，区分Direct和Dependency资源</param>
        /// <param name="dependencyDic">资源依赖关系字典，用于依赖资源的后缀验证</param>
        /// <returns>AssetBundle名称到资源路径列表的映射字典，key=标准化的Bundle名称，value=该Bundle包含的资源完整路径列表</returns>
        /// <exception cref="Exception">当发现资源不在任何打包规则中时抛出异常，阻止无效构建</exception>
        private static Dictionary<string, List<string>> CollectBundle(BuildSetting buildSetting,
            Dictionary<string, EResourceType> assetDic, Dictionary<string, List<string>> dependencyDic)

        {
            //获取进度条显示范围
            float min = collectBundleInfoProgress.x; //最小进度值
            float max = collectBundleInfoProgress.y; //最大进度值

            //显示初始进度条
            EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "搜集bundle信息", min);

            //创建AssetBundle映射字典：key=Bundle名称，value=该Bundle包含的资源列表
            Dictionary<string, List<string>> bundleDic = new Dictionary<string, List<string>>();
            //存储不在打包规则内的外部资源列表
            List<string> notInRuleList = new List<string>();

            //遍历所有资源，按规则分配到对应的AssetBundle
            int index = 0;
            foreach (var kv in assetDic)
            {
                index++;
                string url = kv.Key; //资源路径
                //根据资源路径和类型获取对应的Bundle名称
                string bundleName = buildSetting.GetBundleName(url, kv.Value);

                //没有bundleName的资源为外部资源（不在任何打包规则中）
                if (bundleName == null)
                {
                    notInRuleList.Add(url); //记录违规资源
                    continue; //跳过此资源的处理
                }

                // 
                List<string> list;
                if (!bundleDic.TryGetValue(bundleName, out list))
                {
                    list = new List<string>();
                    bundleDic.Add(bundleName, list); //创建新的Bundle条目
                }

                //将资源添加到对应的Bundle列表中
                list.Add(url);

                //更新进度条显示
                EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "搜集bundle信息",
                    min + (max - min) * ((float)index / assetDic.Count));
            }

            //验证所有资源是否都在打包规则内
            if (notInRuleList.Count > 0)
            {
                //构建详细的错误信息
                string message = string.Empty;
                for (int i = 0; i < notInRuleList.Count; i++)
                {
                    message += "\n" + notInRuleList[i]; //每行显示一个违规资源
                }

                EditorUtility.ClearProgressBar(); //清理进度条
                //抛出异常，阻止构建过程继续
                throw new Exception($"资源不在打包规则中，或后缀不匹配！！！:{message}");
            }

            //对每个Bundle中的资源列表进行排序，确保打包一致性
            foreach (var list in bundleDic.Values)
            {
                list.Sort();
            }

            return bundleDic;
        }

        /// <summary>
        /// 递归分析文件集合的完整依赖关系网络
        /// 通过Unity的AssetDatabase API获取每个资源的直接依赖
        /// 自动处理依赖链的动态扩展，构建完整的资源依赖图
        /// 支持进度显示和文件类型过滤，确保只处理有效的资源文件
        /// </summary>
        /// <param name="files">要分析的起始文件集合，通常是Direct类型的资源</param>
        /// <returns>资源路径到其依赖列表的完整映射字典，key=资源完整路径，value=该资源直接依赖的资源路径列表</returns>
        private static Dictionary<string, List<string>> CollectDependency(ICollection<string> files)
        {
            //获取进度条显示范围
            float min = getDependencyProgress.x; //最小进度值
            float max = getDependencyProgress.y; //最大进度值

            //创建依赖关系字典：key=资源路径，value=该资源的依赖列表
            Dictionary<string, List<string>> dependencyDic = new Dictionary<string, List<string>>();

            //转换为List以便支持动态添加新发现的依赖资源
            //使用List而不是直接遍历ICollection，避免递归处理的复杂性
            List<string> fileList = new List<string>(files);

            //遍历所有文件（包括后续动态添加的依赖文件）
            for (int i = 0; i < fileList.Count; i++)
            {
                string assetUrl = fileList[i];

                //避免重复处理已分析过的资源
                if (dependencyDic.ContainsKey(assetUrl))
                    continue;

                //每处理10个文件更新一次进度条，平衡性能和用户体验
                if (i % 10 == 0)
                {
                    //进度计算：由于会动态添加文件，使用估算方式
                    //乘以3是为了预留动态添加文件的进度空间
                    float progress = min + (max - min) * ((float)i / (files.Count * 3));
                    EditorUtility.DisplayProgressBar($"{nameof(CollectDependency)}", "搜集依赖信息", progress);
                }

                //获取当前资源的直接依赖（不递归）
                string[] dependencies = AssetDatabase.GetDependencies(assetUrl, false);
                List<string> dependencyList = new List<string>(dependencies.Length);

                //过滤并处理依赖资源
                for (int ii = 0; ii < dependencies.Length; ii++)
                {
                    string tempAssetUrl = dependencies[ii];

                    //过滤掉无效或不需要打包的文件类型
                    string extension = Path.GetExtension(tempAssetUrl).ToLower();
                    if (string.IsNullOrEmpty(extension) || extension == ".cs" || extension == ".dll")
                        continue; //跳过无扩展名、脚本和程序集文件

                    //添加有效的依赖资源
                    dependencyList.Add(tempAssetUrl);

                    //如果这是新发现的依赖资源，添加到待处理列表中
                    //这里代替了GetDependencies的递归处理
                    if (!fileList.Contains(tempAssetUrl))
                        fileList.Add(tempAssetUrl);
                }

                //建立资源到其依赖列表的映射关系
                dependencyDic.Add(assetUrl, dependencyList);
            }

            return dependencyDic;
        }

        /// <summary>
        /// 递归搜索指定目录下符合条件的文件列表
        /// 支持灵活的前缀和后缀过滤机制，自动处理路径分隔符标准化
        /// 主要用于构建过程中筛选需要打包的资源文件
        /// </summary>
        /// <param name="path">要搜索的根目录路径，会递归搜索所有子目录</param>
        /// <param name="prefix">文件路径前缀过滤条件，只有路径以此前缀开头的文件才会被包含（可为null表示不过滤）</param>
        /// <param name="suffixes">文件后缀过滤条件数组，文件必须具有其中任意一个后缀才会被包含（可为空数组表示不过滤）</param>
        /// <returns>符合所有过滤条件的文件路径列表，路径统一使用正斜杠格式</returns>
        public static List<string> GetFiles(string path, string prefix, params string[] suffixes)
        {
            //获取指定路径下所有文件（递归搜索所有子目录）
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            List<string> result = new List<string>(files.Length);

            //遍历所有找到的文件
            for (int i = 0; i < files.Length; i++)
            {
                //统一路径分隔符为正斜杠，便于跨平台兼容
                string file = files[i].Replace("\\", "/");

                //前缀过滤：如果指定了前缀，则检查文件路径是否以该前缀开头
                if (prefix != null && !file.StartsWith(prefix, StringComparison.InvariantCulture))
                {
                    continue; //不符合前缀要求，跳过此文件
                }

                //后缀过滤：如果指定了后缀数组，则检查文件是否具有任一指定后缀
                if (suffixes != null && suffixes.Length > 0)
                {
                    bool exist = false;
                    //遍历所有指定的后缀
                    for (int j = 0; j < suffixes.Length; j++)
                    {
                        string suffix = suffixes[j];
                        //使用区域性无关的字符串比较，确保比较一致性
                        if (file.EndsWith(suffix, StringComparison.InvariantCulture))
                        {
                            exist = true;
                            break; //找到匹配的后缀即可退出循环
                        }
                    }

                    if (!exist)
                    {
                        continue; //没有匹配的后缀，跳过此文件
                    }
                }

                //通过所有过滤条件的文件添加到结果列表
                result.Add(file);
            }

            return result;
        }
    }
}