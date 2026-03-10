using System.IO;
using AssetBundleFramework;
using UnityEngine;

/// <summary>
/// 回调模式测试脚本
/// 演示如何使用 ResourceManager 的回调机制加载和实例化资源
/// 适用于 UI 系统初始化等需要按顺序加载多个资源的场景
/// </summary>
public class Test_Callback : MonoBehaviour
{
    
    /// <summary>
    /// AssetBundle 文件路径前缀
    /// 用于构建完整的资源访问路径
    /// </summary>
    private string _prefixPath { get; set; }
    
    /// <summary>
    /// 当前运行平台标识
    /// 根据 Unity 编辑器或不同构建目标自动确定
    /// </summary>
    private string _platForm { get; set; }

    #region  事件函数
    
    /// <summary>
    /// Unity 生命周期方法 - 启动时执行一次
    /// 初始化平台信息和 ResourceManager，并启动资源加载流程
    /// </summary>
    void Start()
    {

        _platForm  = GetPlatform();
        // 构建 AssetBundle 根目录路径
        _prefixPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../AssetBundle")).Replace("\\", "/");
        _prefixPath += $"/{_platForm}";
        // 初始化 ResourceManager
        ResourceManager.Instance.Initialize(GetPlatform(), GetFileUrl, false, 0);
        Initialize();
    }
    
    /// <summary>
    /// Unity 生命周期方法 - 每帧调用
    /// 更新 ResourceManager 以处理异步加载任务
    /// </summary>
    void Update()
    {
        ResourceManager.Instance.Update();
    }
    
    private void LateUpdate()
    {
        ResourceManager.Instance.LateUpdate();
    }

    #endregion
    

    /// <summary>
    /// 初始化资源加载流程
    /// 使用嵌套回调方式依次加载 UI 根节点和子 UI 预制体
    /// 演示了依赖式资源加载的典型用法
    /// </summary>
    private void Initialize()
    {
        // 加载 UI 根节点预制体
        ResourceManager.Instance.LoadWithCallback("Assets/AssetBundle/UI/UIRoot.prefab", true, uiRootResource =>
        {
            uiRootResource.Instantiate();
            
            Transform uiParent = GameObject.Find("Canvas").transform;
            // UI 根节点加载完成后，继续加载子 UI
            ResourceManager.Instance.LoadWithCallback("Assets/AssetBundle/UI/TestUI.prefab", true, testUIResource =>
            {
                testUIResource.Instantiate(uiParent, false);
            });
        });
    }

    /// <summary>
    /// 获取资源的完整文件路径
    /// 将相对路径转换为绝对路径供 ResourceManager 使用
    /// </summary>
    /// <param name="url">资源的相对路径</param>
    /// <returns>资源的绝对路径</returns>
    private string GetFileUrl(string url)
    {
        return $"{_prefixPath}/{url}";
    }
    

   
    
    /// <summary>
    /// 根据当前运行环境获取平台标识符
    /// 支持 Windows、Android、iOS 等主流平台
    /// </summary>
    /// <returns>平台名称字符串</returns>
    /// <exception cref="System.Exception">遇到未支持的平台时抛出异常</exception>
    private string GetPlatform()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                return "Windows";
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.IPhonePlayer:
                return "iOS";
            default:
                throw new System.Exception($"未支持的平台:{Application.platform}");
        }
    }

}
