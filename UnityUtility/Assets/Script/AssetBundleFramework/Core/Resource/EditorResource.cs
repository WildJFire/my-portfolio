using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework
{
    /// <summary>
    /// 编辑器模式资源实现类
    /// 专用于 Unity 编辑器环境，直接使用 AssetDatabase 加载资源
    /// 不依赖 AssetBundle，便于开发调试阶段快速迭代
    /// </summary>
    internal class EditorResource : AResource
    {
        public override bool keepWaiting => !done;

        /// <summary>
        /// 加载资源（编辑器模式）
        /// 直接调用 LoadAsset 完成加载，无异步过程
        /// </summary>
        /// <exception cref="ArgumentException">当 URL 为空时抛出异常</exception>
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"{nameof(EditorResource)}.{nameof(Load)}() {nameof(url)} is null");
            }
            LoadAsset();
        }

        /// <summary>
        /// 卸载资源
        /// 清理非 GameObject 类型的资源对象和回调
        /// </summary>
        internal override void Unload()
        {
            // 非 GameObject 类型的资源需要手动卸载
            if (Asset != null && !(Asset is GameObject))
            {
                Resources.UnloadAsset(base.Asset);
                Asset = null;
            }      
            Asset = null;
            FinishedCallback = null;
            Awaiter = null;
        }

        /// <summary>
        /// 使用 AssetDatabase 加载资源
        /// 仅在编辑器环境下有效，直接根据路径加载资源对象
        /// </summary>
        internal override void LoadAsset()
        {
            #if UNITY_EDITOR
            Asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(url);
            #endif
            done = true;
            // 触发完成回调
            if (FinishedCallback != null)
            {
                Action<AResource> tempCallback = FinishedCallback;
                FinishedCallback = null;
                tempCallback.Invoke(this);
            }
        }
        
        public override T GetAsset<T>()
        {
            Object tempAsset = Asset;
            Type type = typeof(T);
            if (type == typeof(Sprite))
            {
                if (Asset is Sprite)
                {
                    return tempAsset as T;
                }
                else
                {
#if UNITY_EDITOR
                    if (tempAsset && !(tempAsset is GameObject))
                    {
                        Resources.UnloadAsset(tempAsset);
                    }

                    Asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(url);
#endif
                    return Asset as T;
                }
            }
            else
            {
                return tempAsset as T;
            }
        }
    }
}