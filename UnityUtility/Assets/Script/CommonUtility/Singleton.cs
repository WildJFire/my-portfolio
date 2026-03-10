namespace CommonUtility
{
    public class Singleton<T> where T : class, new()
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                // 首次访问时创建实例（懒加载）
                if (_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        
        }
    }
}