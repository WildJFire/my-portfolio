using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework
{
    internal abstract class ABundle
    {
        internal AssetBundle AssetBundle { get; set; }

        internal string url { get; set; }

        /// <summary>
        /// 是否是场景
        /// </summary>
        internal bool isStreamedSceneAssetBundle { get; set; }

        internal int Reference { get; set; }

        //bundle是否加载完成
        internal bool done { get; set; }

        /// <summary>
        /// bundle依赖
        /// </summary>
        internal ABundle[] dependencies { get; set; }
        
        /// <summary>
        /// 加载bundle
        /// </summary>
        internal abstract void Load();

        /// <summary>
        /// 卸载bundle
        /// </summary>
        internal abstract void Unload();

        
        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>指定名字的资源</returns>
        internal abstract Object LoadAsset(string name, Type type);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>AssetBundleRequest</returns>
        internal abstract AssetBundleRequest LoadAssetAsync(string name, Type type);
        
        internal void AddReference()
        {
            ++Reference;
        }

        internal void ReduceReference()
        {
            --Reference;
            if (Reference < 0)
            {
                throw new Exception($"{GetType()}.{nameof(ReduceReference)}() less than 0,{nameof(url)}:{url}");
            }
        }
    }
}