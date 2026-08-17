namespace UIFramework
{
    /// <summary>
    /// 枚举类型，用于定义窗口在打开时、在历史记录和队列中的行为
    /// </summary>
    public enum WindowPriority
    {
        ForceForeground = 0,// 强制前景：窗口会立即显示在所有其他窗口之上，并隐藏所有其他窗口。
        Enqueue = 1,// 入队列：窗口会被添加到一个队列中，等待前面的窗口关闭后才会显示。
    }
}
