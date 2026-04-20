using UnityEngine;

namespace CommonUtility
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static GameObject _singletonRoot = null;
        public bool global = true;
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试在场景中查找
                    _instance = FindFirstObjectByType<T>();
                    InitParent();
                    // 如果场景中没有，则动态创建
                    if (_instance == null)
                    {
                        var go = new GameObject(typeof(T).Name);
                        go.name = typeof(T).Name;
                        go.transform.SetParent(_singletonRoot.transform);

                        _instance = go.AddComponent<T>();

                        if (_instance is MonoSingleton<T> singleton && singleton.global)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                    else if (_instance.gameObject.transform.parent != _singletonRoot.transform)
                    {
                        _instance.gameObject.transform.SetParent(_singletonRoot.transform);
                    }
                }

                return _instance;
            }
        }

        private static void InitParent()
        {
            _singletonRoot = GameObject.Find("SingletonRoot");
            if (_singletonRoot == null)
            {
                _singletonRoot = new GameObject("SingletonRoot");
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != gameObject.GetComponent<T>())
            {
                Destroy(gameObject);
                return;
            }

            _instance = gameObject.GetComponent<T>();
            if (global)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnAwake();
        }

        protected virtual void OnAwake()
        {
        }
    }
}