using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CommonUtility;

namespace HotUpdate
{
    public class MD5Manager : Singleton<MD5Manager> 
    {
        public string GetABPackEncryptVersion(string path)
        {
            try
            {
                using FileStream fs = new FileStream(path, FileMode.Open);
                byte[] bytes = new byte[fs.Length];
                
                // 计算MD5值
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] orgMd5 = md5.ComputeHash(fs);
                
                StringBuilder sb = new StringBuilder();
                foreach (byte b in orgMd5)
                {
                    //转换为16进制字符串，最低两位数字符，不够就补0
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取文件md5值失败 path:{path} error:{ex.Message}");
            }
        }
    }
}