using System;

namespace AssetBundleFramework
{
    public class ResourceAwaiter : IAwaiter<IResource>, IAwaitable<ResourceAwaiter, IResource>
    {
        public IResource result{ get; private set;}
        public bool IsCompleted { get; private set;}
        private Action _continuation;

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation.Invoke();
            }
            else
            {
                _continuation = continuation;
            }
        }

        public IResource GetResult()
        {
            return result;
        }

        public ResourceAwaiter GetAwaiter()
        {
            return this;
        }

        internal void SetResult(IResource result)
        {
            IsCompleted = true;
            this.result = result;
            Action continuationTemp = _continuation;
            continuationTemp?.Invoke();
            _continuation = null;
        }
    }
}