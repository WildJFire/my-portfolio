using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace AssetBundleFramework
{
    /// <summary>
    /// XML工具类，提供XML序列化和反序列化的便捷方法
    /// 主要用于配置文件的读取和解析，支持泛型类型安全操作
    /// 自动处理文件不存在、格式错误等异常情况
    /// 是AssetBundle构建系统配置管理的基础工具类
    /// </summary>
    public static class XmlUtility
    {
        /// <summary>
        /// 从XML文件读取并反序列化为指定类型的对象
        /// 支持泛型类型安全操作，自动处理文件IO异常和XML格式错误
        /// 采用try-finally模式确保文件流正确关闭
        /// 主要用于读取BuildSetting等配置文件
        /// </summary>
        /// <typeparam name="T">目标对象类型，必须是引用类型</typeparam>
        /// <param name="fileName">XML文件的完整路径</param>
        /// <returns>反序列化成功的对象实例，如果读取失败则返回default(T)</returns>
        public static T Read<T>(string fileName) where T : class
        {
            FileStream fs = null;
            //检查文件是否存在，不存在则直接返回默认值
            if (!File.Exists(fileName))
            {
                return default(T);
            }
            try
            {
                //创建XML序列化器实例
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                //打开文件流
                fs = File.Open(fileName, FileMode.Open);
                //创建XML读取器
                XmlReader reader = XmlReader.Create(fs);
                //执行反序列化操作
                T instance = (T)serializer.Deserialize(reader);
                //关闭读取器和文件流
                reader.Close();
                fs.Close();
                return instance;
            }
            catch (Exception e)
            {
                //发生异常时确保文件流被正确关闭
                if (fs != null)
                {
                    fs.Close();
                }
                //返回默认值表示读取失败
                return default(T);
            }
        }
    }
}