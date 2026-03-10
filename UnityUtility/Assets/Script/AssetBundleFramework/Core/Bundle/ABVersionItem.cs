namespace AssetBundleFramework
{
    public class ABVersionItem
    {
        /// <summary>
        /// 资源名称
        /// </summary>
        private string _abName;
        public string ABName { get => _abName; set => _abName = value; }

        /// <summary>
        /// 版本md5值
        /// </summary>
        private string _md5;
        public string Md5 { get => _md5; set => _md5 = value; }

        /// <summary>
        /// 文件大小
        /// </summary>
        private int _size;
        public int Size { get => _size; set => _size = value; }
    }
}