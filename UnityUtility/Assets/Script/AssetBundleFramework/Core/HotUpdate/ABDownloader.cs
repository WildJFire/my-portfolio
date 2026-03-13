using System.Collections;
using System.IO;
using AssetBundleFramework;
using CommonUtility;
using UnityEngine;
using UnityEngine.Networking;

namespace HotUpdate
{
    /// <summary>
    /// AB 包下载器，负责从远程服务器下载 AssetBundle 资源包并保存到本地
    /// </summary>
    public class ABDownloader
    {
        /// <summary>
        /// 当前下载器下载的 AB 包版本信息
        /// </summary>
        private ABVersionItem _abVersionItem;

        /// <summary>
        /// 标记当前是否正在执行下载任务
        /// </summary>
        private bool _isDownloading = false;

        /// <summary>
        /// 获取当前是否正在下载
        /// </summary>
        public bool IsDownloading
        {
            get => _isDownloading;
        }

        /// <summary>
        /// 获取当前待下载资源的体积大小（单位：字节）
        /// </summary>
        /// <returns>返回 AB 包的资源大小，如果未设置版本信息则返回 0</returns>
        public float GetDownloadResSize()
        {
            return _abVersionItem?.Size ?? 0;
        }

        /// <summary>
        /// 从远程服务器下载指定的 AB 包资源到本地持久化目录
        /// </summary>
        /// <param name="abVersionItem">AB 包版本信息，包含资源名称、文件大小和 MD5 校验值</param>
        /// <returns>协程迭代器，用于控制下载流程的异步执行</returns>
        public IEnumerator DownloadABPack(ABVersionItem abVersionItem)
        {
            // 保存待下载的资源信息并设置下载状态
            _abVersionItem = abVersionItem;
            _isDownloading = true;
                    
            // 构建远程资源 URL
            string url = $"{HotUpdateManager.BaseUrl}/{_abVersionItem.ABName}";
            UnityWebRequest request = UnityWebRequest.Get(url);
                        
            // 发送下载请求并等待响应
            yield return request.SendWebRequest();
                        
            // 检查下载结果是否成功
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"获取 AB 包 {url} 错误");
                            
                // 根据错误类型输出详细的错误信息
                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError("网络连接错误");
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError($"HTTP 协议错误：{request.responseCode}");
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("数据处理错误");
                        break;
                    default:
                        Debug.LogError($"未知错误：{request.result}");
                        break;
                }
                
                // 下载失败，退出协程
                yield break;
            }
            
            // 构建本地保存路径（使用 Application.persistentDataPath）
            string packPath = Path.Combine(Application.persistentDataPath, _abVersionItem.ABName.Replace('\\', '/'));            Debug.Log($"下载 AB 包 {url} 成功，保存路径：{packPath}");
                        
            // 确保文件目录存在
            IOUtils.CreateDirectoryOfFile(packPath);
                        
            // 如果文件不存在则创建空文件
            if (!File.Exists(packPath))
            {
                File.Create(packPath).Dispose();
            }
            
            // 将下载的数据写入文件
            File.WriteAllBytes(packPath, request.downloadHandler.data);
            // 更新客户端的 AB 包版本信息
            HotUpdateManager.Instance.UpdateClientABInfo(_abVersionItem);
            
            // 重置下载状态
            _isDownloading = false;
            
            // 通知热更新管理器开始下一个 AB 包的下载
            HotUpdateManager.Instance.ChangeDownloadNextABPack(this);

        }
    }
}