namespace UIFramework
{
    /// <summary>
    /// 界面属性的接口
    /// </summary>
    public interface IScreenProperties { }

    /// <summary>
    /// 面板属性的接口
    /// </summary>
    public interface IPanelProperties : IScreenProperties
    {
        PanelPriority Priority { get; set; }
    }

    /// <summary>
    /// 窗口属性的接口
    /// </summary>
    public interface IWindowProperties : IScreenProperties
    {
        /// <summary>
        /// 窗口优先级，决定了窗口在队列中的位置
        /// </summary>
        WindowPriority WindowQueuePriority { get; set; }
        /// <summary>
        /// 显示到最前面，隐藏其他窗口
        /// </summary>
        bool HideOnForegroundLost { get; set; }
        /// <summary>
        /// 是否是一个弹窗界面
        /// </summary>
        bool IsPopup { get; set; }
        /// <summary>
        /// 有没有覆写预制体的属性
        /// </summary>
        bool SuppressPrefabProperties { get; set; }
    }
}