using System;

namespace UIFramework
{
    /// <summary>
    /// 所有的UI界面必须实现的接口，统一风格
    /// </summary>
    public interface IScreenController
    {
        /// <summary>
        /// 界面ID，唯一标识一个界面
        /// </summary>
        string ScreenId { get; set; }

        /// <summary>
        /// 界面是否可见
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// 显示界面
        /// </summary>
        /// <param name="props"></param>
        void Show(IScreenProperties props = null);

        /// <summary>
        /// 隐藏界面
        /// </summary>
        /// <param name="animate"></param>
        void Hide(bool animate = true);

        /// <summary>
        /// 当界面进入过渡动画完成时的回调
        /// </summary>
        Action<IScreenController> InTransitionFinished { get; set; }
        /// <summary>
        /// 当界面退出过渡动画完成时的回调
        /// </summary>
        Action<IScreenController> OutTransitionFinished { get; set; }
        /// <summary>
        /// 关闭
        /// </summary>
        Action<IScreenController> CloseRequest { get; set; }
        /// <summary>
        /// 界面销毁时的回调
        /// </summary>
        Action<IScreenController> ScreenDestroyed { get; set; }
    }

    public interface IPanelController : IScreenController
    {
        /// <summary>
        /// 层级
        /// </summary>
        PanelPriority Priority { get; }
    }

    /// <summary>
    /// 所有的窗口必须实现的接口
    /// </summary>
    public interface IWindowController : IScreenController
    {
        bool HideOnForegroundLost { get; }
        bool IsPopup { get; }
        WindowPriority WindowPriority { get; }
    }
}