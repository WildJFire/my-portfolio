using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CommonUtility;
using UnityEngine;

namespace HotUpdate
{
    /// <summary>
    /// HybridCLR 热更新管理器
    /// </summary>
    public class HybirdCLRManager: MonoSingleton<HybirdCLRManager>
    {
        #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        public static string platfrom = "Windows";
#elif UNITY_ANDROID
    public static string platfrom = "Android";
#elif UNITY_IPHONE
    public static string platfrom = "IOS";
#endif
        /// <summary>
        /// 热更新 DLL 文件路径 (在 StreamingAssets 或 PersistentData 目录)
        /// </summary>
        private string _hotUpdateDllPath => Path.Combine(Application.persistentDataPath, $"AssetBundle/{platfrom}/HotUpdate");

        /// <summary>
        /// 加载的热更新程序集
        /// </summary>
        private Dictionary<string ,Assembly> _hotUpdateAssemblies = new ();

        /// <summary>
        /// 初始化并加载热更新 DLL
        /// </summary>
        public void Initialize()
        {
            Debug.Log("开始初始化 HybridCLR 热更新...");
            
            // 检查 DLL 文件是否存在
            if (!Directory.Exists(_hotUpdateDllPath))
            {
                Debug.LogError($"未找到热更新 DLL 文件夹：{_hotUpdateDllPath}");
                return;
            }

            try
            {
                DirectoryInfo info = new DirectoryInfo(_hotUpdateDllPath);
                foreach (FileInfo file in info.GetFiles("*", SearchOption.AllDirectories))
                {
                    // 读取 DLL 字节数据
                    byte[] dllBytes = File.ReadAllBytes(file.FullName);

                    Assembly dll =  Assembly.Load(dllBytes);
                    // 加载程序集
                    _hotUpdateAssemblies.Add(dll.GetName().Name, dll);
                
                    Debug.Log($"成功加载热更新程序集：{file.FullName}");

                }
               
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载热更新 DLL 失败：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取热更新程序集中的类型
        /// </summary>
        /// <param name="typeName">完整类型名 (包含命名空间)</param>
        /// <returns>类型对象</returns>
        public Type GetHotUpdateType(string typeName)
        {
            _hotUpdateAssemblies.TryGetValue(typeName, out Assembly assembly);
            return assembly?.GetType(typeName);
        }

        /// <summary>
        /// 调用热更新程序集的静态方法
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="typeName">类型全名</param>
        /// <param name="methodName">方法名</param>
        /// <param name="parameters">方法参数</param>
        /// <returns>方法返回值</returns>
        public T InvokeStaticMethod<T>(string typeName, string methodName, params object[] parameters)
        {
            Type type = GetHotUpdateType(typeName);
            if (type == null)
            {
                Debug.LogError($"未找到类型：{typeName}");
                return default;
            }

            MethodInfo method = type.GetMethod(methodName);
            if (method == null)
            {
                Debug.LogError($"未找到方法：{methodName}");
                return default;
            }

            return (T)method.Invoke(null, parameters);
        }
    }
}