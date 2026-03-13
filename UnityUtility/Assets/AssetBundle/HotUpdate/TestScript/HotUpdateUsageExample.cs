using UnityEngine;
using HotUpdate;

namespace AssetBundleFramework.Demo
{
    /// <summary>
    /// HybridCLR 热更新使用示例
    /// </summary>
    public class HotUpdateTest : MonoBehaviour
    {
        void Start()
        {
            // 初始化 HybridCLR 管理器
            HybirdCLRManager.Instance.Initialize();
            
            Debug.Log("===== 热更新使用示例 =====");
            
            // 方式 1: 直接调用封装好的泛型方法
            string result = HybirdCLRManager.Instance.InvokeStaticMethod<string>(
                "HotUpdateTest.TestClass", 
                "TestMethod"
            );
            Debug.Log($"调用结果：{result}");
            
            // 方式 2: 获取类型后自行操作
            System.Type testType = HybirdCLRManager.Instance.GetHotUpdateType("HotUpdateTest.TestClass");
            if (testType != null)
            {
                Debug.Log($"成功获取类型：{testType.FullName}");
                // 可以继续创建实例、调用方法等
            }
        }
    }
}
