using System;
using System.IO;
using AssetBundleFramework;
using HotUpdate;
using UIComponent;
using UnityEngine;


public class GameRoot : MonoBehaviour
{
    public ProgressBar progressBar;
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
    
    
    void Start()
    {
        _platForm  = GetPlatform();
        // 构建 AssetBundle 根目录路径
        _prefixPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "AssetBundle")).Replace("\\", "/");
        _prefixPath += $"/{_platForm}";
        HotUpdateManager.Instance.OnEndDownload += OnStart;
        HotUpdateManager.Instance.StartHotUpdate();
    }

    void OnStart()
    {
        GameObject uiTest = null;
        Debug.Log("OnStart 开始加载资源");
        HybirdCLRManager.Instance.Initialize();
        ResourceManager.Instance.Initialize(GetPlatform(), GetFileUrl, false, 0);
        ResourceManager.Instance.LoadWithCallback("Assets/AssetBundle/UI/UIRoot.prefab", true,
            resource =>
            {
                resource.Instantiate();
                Transform uiParent = GameObject.Find("Canvas").transform;
                ResourceManager.Instance.LoadWithCallback("Assets/AssetBundle/UI/TestHotUpdateUI.prefab",
                    true,
                    resource =>
                    {
                        uiTest = resource.Instantiate(uiParent, false);
                        Type type = HybirdCLRManager.Instance.GetHotUpdateType("HotUpdateTest");
                        if (type != null)
                        {
                            uiTest.AddComponent(type);
                        }
                        else
                        {
                            Debug.LogError($"未找到类型 HotUpdateTest");
                        }
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
        return Path.Combine(_prefixPath, url).Replace("\\", "/");
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
}