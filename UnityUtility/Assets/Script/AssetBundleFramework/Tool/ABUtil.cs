using UnityEngine;

namespace AssetBundleFramework
{
    public static class ABUtil
    {
        /// <summary>
        /// AB包的后缀扩展名
        /// </summary>
        private static string _sABPackExName = ".ab";
        public static string ABPackExName { get { return _sABPackExName; } }

        /// <summary>
        /// 缓存AB包版本信息的文件名
        /// </summary>
        private static string _sABVersionName = "ABVersionFile.txt";
        public static string sABVersionName { get { return _sABVersionName; } }

        /// <summary>
        /// 获取AB包的版本信息字符串
        /// </summary>
        /// <param name="sABName">包名（含AssetBundle路径）</param>
        /// <param name="sFileVersionMd5">版本信息的MD5值</param>
        /// <param name="nFileSize">文件大小</param>
        /// <returns></returns>
        public static string GetABPackVersionStr(string sABName, string sFileVersionMd5, string sFileSize)
        {
            return $"{sABName}|{sFileVersionMd5}|{sFileSize}";
        }
        
        public static string GetABPackVersionStr(ABVersionItem item)
        {
            return $"{item.ABName}|{item.Md5}|{item.Size}";
        }


        /// <summary>
        /// 获取不同平台AB包存放路径的字符串
        /// </summary>
        /// <returns></returns>
        public static string GetABPackPathPlatformStr()
        {
            RuntimePlatform platform = Application.platform;
            string platformStr = "/AssetBundle/";
            if (platform == RuntimePlatform.WindowsEditor || platform == RuntimePlatform.WindowsPlayer)
            {
                platformStr += "Windows/";
            }
            else if (platform == RuntimePlatform.Android)
            {
                platformStr += "Android/";
            }
            else if (platform == RuntimePlatform.IPhonePlayer)
            {
                platformStr += "iOS/";
            }

            return platformStr;
        }
    }
}