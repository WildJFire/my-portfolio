using UnityEngine;

namespace AssetBundleFramework
{
    /// <summary>
    /// 资源接口定义
    /// 提供资源获取和实例化的标准方法
    /// 所有资源类都必须实现此接口
    /// </summary>
    public interface IResource
    {
        /// <summary>
        /// 获取加载完成的资源对象
        /// </summary>
        /// <returns>UnityEngine.Object 类型的资源对象</returns>
        Object GetAsset();

        T GetAsset<T>() where T : Object;

        /// <summary>
        /// 实例化资源为 GameObject
        /// 仅对 GameObject 类型的资源有效
        /// </summary>
        /// <returns>实例化的 GameObject，如果资源不是 GameObject 则返回 null</returns>
        GameObject Instantiate();

        GameObject Instantiate(bool autoUnload);
        
        GameObject Instantiate(Vector3 position, Quaternion rotation);
        
        GameObject Instantiate(Vector3 position, Quaternion rotation, bool autoUnload);
        
        /// <summary>
        /// 实例化资源为 GameObject 并设置父节点
        /// 支持指定是否在世界空间中实例化
        /// </summary>
        /// <param name="parent">父节点 Transform</param>
        /// <param name="instanceInWorldSpace">是否在世界空间中实例化</param>
        /// <returns>实例化的 GameObject，如果资源不是 GameObject 则返回 null</returns>
        GameObject Instantiate(Transform parent, bool instanceInWorldSpace);

        GameObject Instantiate(Transform parent, bool instantiateInWorldSpace, bool autoUnload);
    }
}