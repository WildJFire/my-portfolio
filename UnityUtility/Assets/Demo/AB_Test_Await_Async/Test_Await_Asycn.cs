using System.IO;
using AssetBundleFramework;
using UnityEngine;

public class Test_Await_Asycn : MonoBehaviour
{
    private string PrefixPath { get; set; }
    private string Platform { get; set; }

    private void Start()
    {
        Platform = GetPlatform();
        PrefixPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../AssetBundle")).Replace("\\", "/");
        PrefixPath += $"/{Platform}";
        ResourceManager.Instance.Initialize(GetPlatform(), GetFileUrl, false, 0);

        Initialize();
    }

    private async void Initialize()
    {
        ResourceAwaiter uiRootAwaiter = ResourceManager.Instance.LoadWithAwaiter("Assets/AssetBundle/UI/UIRoot.prefab");
        await uiRootAwaiter;
        uiRootAwaiter.GetResult().Instantiate();
        Transform uiParent = GameObject.Find("Canvas").transform;
        ResourceAwaiter testResourceAwaiter = ResourceManager.Instance.LoadWithAwaiter("Assets/AssetBundle/UI/TestUI.prefab");
        await testResourceAwaiter;
        testResourceAwaiter.GetResult().Instantiate(uiParent,  false);
    }

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

    private string GetFileUrl(string assetUrl)
    {
        return $"{PrefixPath}/{assetUrl}";
    }

    private void Update()
    {
        ResourceManager.Instance.Update();
    }

    private void LateUpdate()
    {
        ResourceManager.Instance.LateUpdate();
    }
}