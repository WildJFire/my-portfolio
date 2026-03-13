using System;

namespace AssetBundleFramework
{
    public interface IResourceManager
    {
        public void LoadWithCallback(string url, bool async, Action<IResource> callback);
        public IResource Load(string url, bool async);
        public ResourceAwaiter LoadWithAwaiter(string url);
        public void Unload(IResource resource);
    }
}