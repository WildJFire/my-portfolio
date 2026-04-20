using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AssetBundleFramework;
using CommonUtility;
using UnityEngine;
using UnityEngine.Networking;

namespace HotUpdate
{
    public class HotUpdateManager : MonoSingleton<HotUpdateManager>
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        public static string BaseUrl = "http://127.0.0.1:5858";
#elif UNITY_ANDROID
    public static string _sBaseUrl = "http://127.0.0.1:5858";
#elif UNITY_IPHONE
    public static string _sBaseUrl = "http://127.0.0.1:5858";
#endif

        #region 生命周期

        /// <summary>
        /// 当单个文件下载完成，触发时会传入下载文件的大小
        /// </summary>
        public Action<float> OnOneFileDownload = (f) =>
        {
            Debug.Log("OnOneFileDownload 触发");
        };
        
        /// <summary>
        /// 当开始下载时，触发时会传入下载文件的总大小
        /// </summary>
        public Action<float> OnStartDownload = (f) =>
        {
            Debug.Log("OnStartDownload 触发");
        };
        
        /// <summary>
        /// 当下载完成时
        /// </summary>
        public Action OnEndDownload = () => {
            Debug.Log("OnEndDownload 触发");
        };
        
        #endregion

        /// <summary>
        /// 版本文件名
        /// </summary>
        private string _sABVersionName = "";

        /// <summary>
        /// 本地版本信息缓存路径
        /// </summary>
        private string _sVersionLocalFilePath = "";

        /// <summary>
        /// 同时下载的最大数量
        /// </summary>
        private int _maxDownloader = 5;

        /// <summary>
        /// 所需下载资源总大小
        /// </summary>
        private float _downloadTotalSize = 0;

        /// <summary>
        /// 当前已下载资源的大小
        /// </summary>
        private float _currentDownloadedSize = 0;

        /// <summary>
        /// 所有需要下载的AB包
        /// </summary>
        private List<ABVersionItem> _allNeedDownloadABPack = new();

        /// <summary>
        /// 客户端版本信息
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, ABVersionItem> _clientVersionInfo = null;

        /// <summary>
        /// AB包下载器
        /// </summary>
        private List<ABDownloader> _allDownloadedABPack = new();

        protected override void OnAwake()
        {
            string sPlatformStr = ABUtil.GetABPackPathPlatformStr();
            _sABVersionName = sPlatformStr + ABUtil.sABVersionName;
            _sVersionLocalFilePath = Application.persistentDataPath + _sABVersionName;
            IOUtils.CreateDirectoryOfFile(_sVersionLocalFilePath);
        }

        /// <summary>
        /// 开始热更
        /// </summary>
        public void StartHotUpdate()
        {
            Debug.Log("开始热更 >>>>>> ");
            StartCoroutine(DownloadAllABPackVersion());
        }

        /// <summary>
        /// 获取服务端的AB包版本信息
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownloadAllABPackVersion()
        {
            string versionUrl = $"{BaseUrl}/{_sABVersionName}";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(versionUrl))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    switch (webRequest.result)
                    {
                        case UnityWebRequest.Result.ConnectionError:
                            Debug.LogError("网络连接错误");
                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            Debug.LogError($"HTTP 协议错误：{webRequest.responseCode}");
                            break;
                        case UnityWebRequest.Result.DataProcessingError:
                            Debug.LogError("数据处理错误");
                            break;
                        default:
                            Debug.LogError($"未知错误：{webRequest.result}");
                            break;
                    }

                    yield break;
                }

                string sVersionData = webRequest.downloadHandler.text;
                Debug.Log("成功获取到版本相关数据 >>>> \n" + sVersionData);
                CheckNeedDownloadABPack(sVersionData);
            }
        }

        /// <summary>
        /// 检测需要下载
        /// </summary>
        /// <param name="sServerVersionData"></param>
        void CheckNeedDownloadABPack(string sServerVersionData)
        {
            Dictionary<string, ABVersionItem> items = ConvertToAllABPackDesc(sServerVersionData);

            if (File.Exists(_sVersionLocalFilePath)) //如果本地有版本信息，说明已经下载过了
            {
                string localVersion = File.ReadAllText(_sVersionLocalFilePath);
                _clientVersionInfo = ConvertToAllABPackDesc(localVersion);
                foreach (var item in items)
                {
                    if (!_clientVersionInfo.ContainsKey(item.Key)) //如果本地没有该包
                    {
                        _allNeedDownloadABPack.Add(item.Value);
                        _downloadTotalSize += item.Value.Size;
                    }
                    else if (item.Value.Md5 != _clientVersionInfo[item.Key].Md5)
                    {
                        _allNeedDownloadABPack.Add(item.Value);
                        _downloadTotalSize += item.Value.Size;
                    }
                }
            }
            else //如果本地没有版本信息，说明是第一次下载
            {
                _allNeedDownloadABPack.AddRange(items.Values);
                _downloadTotalSize = items.Sum(item => item.Value.Size);
            }

            StartDownloadAllABPack();
        }

        /// <summary>
        /// 开始下载所有所需下载的AB包资源
        /// </summary>
        /// <param name="list_allABPack"></param>
        void StartDownloadAllABPack()
        {
            OnStartDownload?.Invoke(_downloadTotalSize);
            int maxCount = _allNeedDownloadABPack.Count;
            if (maxCount <= 0)
            {
                HotUpdateEnd();
                return;
            }

            int nNeedCount = Mathf.Min(maxCount, _maxDownloader);
            for (int i = 0; i < nNeedCount; i++)
            {
                ABDownloader downloader = new ABDownloader();
                _allDownloadedABPack.Add(downloader);
                StartCoroutine(downloader.DownloadABPack(_allNeedDownloadABPack[0]));
                _allNeedDownloadABPack.RemoveAt(0); //当前数据结构时间复杂度较高，待优化
            }
        }

        /// <summary>
        /// 热更新结束，进入下一个阶段
        /// </summary>
        private void HotUpdateEnd()
        {
            StringBuilder sb = new();
            foreach (var item in _clientVersionInfo.Values)
            {
                sb.AppendLine(ABUtil.GetABPackVersionStr(item));
            }

            IOUtils.CreatTextFile(_sVersionLocalFilePath, sb.ToString());
            OnEndDownload?.Invoke();
            Debug.Log("热更新: 已完成所有的AB包下载, 进入下一个阶段 TODO");
        }

        /// <summary>
        /// 解析版本文件，返回一个文件列表
        /// </summary>
        /// <param name="content">由多行“包名 版本md5值 包大小”构成</param>
        /// <returns></returns>
        public Dictionary<string, ABVersionItem> ConvertToAllABPackDesc(string content)
        {
            Dictionary<string, ABVersionItem> items = new Dictionary<string, ABVersionItem>();
            content = content.Replace("\r", "");
            string[] lines = content.Split('\n');
            int i = 0;
            foreach (var line in lines)
            {
                Debug.Log($"第{i++}行数据 >>>> {line}");
                if (string.IsNullOrEmpty(line)) continue;
                string[] data = line.Split('|');
                items.Add(data[0], new ABVersionItem
                    { ABName = data[0], Md5 = data[1], Size = int.Parse(data[2]) }
                );
            }

            return items;
        }

        /// <summary>
        /// 更新本地缓存的AB包版本数据
        /// </summary>
        /// <param name="abVersionItem"></param>
        public void UpdateClientABInfo(ABVersionItem abVersionItem)
        {
            _clientVersionInfo ??= new Dictionary<string, ABVersionItem>();
            _clientVersionInfo[abVersionItem.ABName] = abVersionItem;
        }

        /// <summary>
        /// 切换下载下一个AB包
        /// </summary>
        /// <param name="abDownloader">需要切换的下载器</param>
        public void ChangeDownloadNextABPack(ABDownloader abDownloader)
        {
            float lastDownloadSize = abDownloader.GetDownloadResSize();
            _currentDownloadedSize += lastDownloadSize;
            OnOneFileDownload?.Invoke(lastDownloadSize);

            if (_allNeedDownloadABPack.Count > 0)
            {
                StartCoroutine(abDownloader.DownloadABPack(_allNeedDownloadABPack[0]));
                _allNeedDownloadABPack.RemoveAt(0);
            }
            else
            {
                bool isAllDownloadFinished = true;
                foreach (var item in _allDownloadedABPack)
                {
                    if (item.IsDownloading)
                    {
                        isAllDownloadFinished = false;
                    }
                }

                if (isAllDownloadFinished)
                {
                    HotUpdateEnd();
                }
            }
        }
    }
}