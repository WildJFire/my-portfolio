using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework
{
    internal class Bundle : ABundle
    {
        internal override void Load()
        {
            if (AssetBundle)
            {
                throw new Exception($"{nameof(Bundle)}.{nameof(Load)}() {nameof(AssetBundle)} not null, url: {url}");
            }

            string file = BundleManager.Instance.GetFileUrl(url);
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(
                    $"{nameof(Bundle)}.{nameof(Load)}() {nameof(file)} not exist, url: {url}");
            }
#endif
            // 从文件加载 AssetBundle
            AssetBundle = AssetBundle.LoadFromFile(file,0,BundleManager.Instance.Offset);
            isStreamedSceneAssetBundle = AssetBundle.isStreamedSceneAssetBundle;
            done = true;
        }

        internal override void Unload()
        {
            if (AssetBundle)
            {
                AssetBundle.Unload(true);
            }
            done = false;
            Reference = 0;
            AssetBundle = null;
            isStreamedSceneAssetBundle = false;
        }

        internal override Object LoadAsset(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"{nameof(Bundle)}.{nameof(LoadAsset)}() {nameof(name)} is null");
            }

            if (AssetBundle == null)
            {
                throw new Exception(
                    $"{nameof(Bundle)}.{nameof(LoadAsset)}() {nameof(AssetBundle)} is null, url: {url}");
            }

            return AssetBundle.LoadAsset(name, type);
        }

        internal override AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"{nameof(Bundle)}.{nameof(LoadAssetAsync)}() is null");
            }

            if (AssetBundle == null)
            {
                throw new Exception(
                    $"{nameof(Bundle)}.{nameof(LoadAssetAsync)}() Bundle  is null");
            }
            
            return AssetBundle.LoadAssetAsync(name, type);
        }
    }
}