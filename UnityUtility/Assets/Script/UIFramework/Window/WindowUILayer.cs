using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIFramework
{
    /// <summary>
    /// 这个layer层控制所有的窗口.
    /// 有显示记录和队列的，并且一次只显示一个
    /// </summary>
    public class WindowUILayer : UILayer<IWindowController>
    {
        /// <summary>
        /// 这是一个“辅助”层级，以便显示优先级更高的窗口。
        /// </summary>
        [SerializeField]
        private WindowParaLayer priorityParaLayer = null;
        private Queue<WindowHistoryEntry> windowQueue;
        private Stack<WindowHistoryEntry> windowHistory;
        private HashSet<IScreenController> screensTransitioning;
        /// <summary>
        /// 当前显示的窗口
        /// </summary>
        public IWindowController CurrentWindow { get; private set; }
        public event Action RequestScreenBlock;
        public event Action RequestScreenUnblock;
        /// <summary>
        /// 是否有窗口正在进行过渡动画
        /// </summary>
        private bool IsScreenTransitionInProgress
        {
            get { return screensTransitioning.Count != 0; }
        }

        public override void Initialize()
        {
            base.Initialize();
            registeredScreens = new Dictionary<string, IWindowController>();
            windowQueue = new Queue<WindowHistoryEntry>();
            windowHistory = new Stack<WindowHistoryEntry>();
            screensTransitioning = new HashSet<IScreenController>();
        }

        protected override void ProcessScreenRegister(string screenId, IWindowController controller)
        {
            base.ProcessScreenRegister(screenId, controller);
            controller.InTransitionFinished += OnInAnimationFinished;
            controller.OutTransitionFinished += OnOutAnimationFinished;
            controller.CloseRequest += OnCloseRequestedByWindow;
        }

        protected override void ProcessScreenUnregister(string screenId, IWindowController controller)
        {
            base.ProcessScreenUnregister(screenId, controller);
            controller.InTransitionFinished -= OnInAnimationFinished;
            controller.OutTransitionFinished -= OnOutAnimationFinished;
            controller.CloseRequest -= OnCloseRequestedByWindow;
        }
        public override void ShowScreen(IWindowController screen)
        {
            ShowScreen<IWindowProperties>(screen, null);
        }

        public override void ShowScreen<TProp>(IWindowController screen, TProp properties)
        {
            IWindowProperties windowProp = properties as IWindowProperties;

            if (ShouldEnqueue(screen, windowProp))
            {// 应该入队
                EnqueueWindow(screen, properties);
            }
            else
            {// 直接显示
                DoShow(screen, windowProp);
            }
        }

        public override void HideScreen(IWindowController screen)
        {
            if (screen == CurrentWindow)
            {
                windowHistory.Pop();
                // 添加过渡效果
                AddTransition(screen);
                screen.Hide();

                CurrentWindow = null;

                if (windowQueue.Count > 0)
                {// 队列里有等待的窗口，显示下一个
                    ShowNextInQueue();
                }
                else if (windowHistory.Count > 0)
                {// 没有等待的窗口了，显示历史记录里上一个
                    ShowPreviousInHistory();
                }
            }
            else
            {
                Debug.LogError(
                    string.Format(
                        "[WindowUILayer] Hide requested on WindowId {0} but that's not the currently open one ({1})! Ignoring request.",
                        screen.ScreenId, CurrentWindow != null ? CurrentWindow.ScreenId : "current is null"));
            }
        }

        /// <summary>
        /// 隐藏所有窗口，并清空历史记录和队列。
        /// </summary>
        /// <param name="shouldAnimateWhenHiding">是否在隐藏时播放动画</param>
        public override void HideAll(bool shouldAnimateWhenHiding = true)
        {
            base.HideAll(shouldAnimateWhenHiding);
            CurrentWindow = null;
            priorityParaLayer.RefreshDarken();
            windowHistory.Clear();
        }

        /// <summary>
        /// 如果是弹窗，放在优先级层级里，否则放在正常层级里
        /// </summary>
        public override void ReparentScreen(IScreenController controller, Transform screenTransform)
        {
            IWindowController window = controller as IWindowController;

            if (window == null)
            {
                Debug.LogError("[WindowUILayer] Screen " + screenTransform.name + " is not a Window!");
            }
            else
            {
                if (window.IsPopup)
                {// 如果是弹窗，放在优先级层级里
                    priorityParaLayer.AddScreen(screenTransform);
                    return;
                }
            }

            base.ReparentScreen(controller, screenTransform);
        }

        private void EnqueueWindow<TProp>(IWindowController screen, TProp properties) where TProp : IScreenProperties
        {
            windowQueue.Enqueue(new WindowHistoryEntry(screen, (IWindowProperties)properties));
        }

        /// <summary>
        /// 判断窗口是否应该入队
        /// </summary>
        /// <param name="controller">窗口控制器</param>
        /// <param name="windowProp">窗口属性</param>
        /// <returns></returns>
        private bool ShouldEnqueue(IWindowController controller, IWindowProperties windowProp)
        {
            if (CurrentWindow == null && windowQueue.Count == 0)
            {// 没有当前窗口，也没有等待的窗口，直接显示
                return false;
            }

            if (windowProp != null && windowProp.SuppressPrefabProperties)
            {// 如果覆写了Prefab里的属性

                // 如果这个窗口的优先级不是直接打开，就入队
                return windowProp.WindowQueuePriority != WindowPriority.ForceForeground;
            }

            if (controller.WindowPriority != WindowPriority.ForceForeground)
            {// 如果这个窗口的优先级不是直接打开，就入队
                return true;
            }

            return false;
        }

        /// <summary>
        /// 显示历史窗口
        /// </summary>
        private void ShowPreviousInHistory()
        {
            if (windowHistory.Count > 0)
            {
                WindowHistoryEntry window = windowHistory.Pop();
                DoShow(window);
            }
        }

        /// <summary>
        /// 显示队列里的下一个窗口
        /// </summary>
        private void ShowNextInQueue()
        {
            if (windowQueue.Count > 0)
            {
                WindowHistoryEntry window = windowQueue.Dequeue();
                DoShow(window);
            }
        }

        private void DoShow(IWindowController screen, IWindowProperties properties)
        {
            DoShow(new WindowHistoryEntry(screen, properties));
        }

        private void DoShow(WindowHistoryEntry windowEntry)
        {
            if (CurrentWindow == windowEntry.Screen)
            {// 如果当前窗口就是要显示的窗口，警告一下，因为这会导致历史记录里有重复的窗口，可能会导致不一致的行为。建议如果需要多次打开同一个窗口（例如：当实现一个警告消息弹出时），它在触发继续流程的玩家输入后自己关闭。

                Debug.LogWarning(
                    string.Format(
                        "[WindowUILayer] The requested WindowId ({0}) is already open! This will add a duplicate to the " +
                        "history and might cause inconsistent behaviour. It is recommended that if you need to open the same" +
                        "screen multiple times (eg: when implementing a warning message pop-up), it closes itself upon the player input" +
                        "that triggers the continuation of the flow."
                        , CurrentWindow.ScreenId));
            }
            else if (CurrentWindow != null
                     && CurrentWindow.HideOnForegroundLost
                     && !windowEntry.Screen.IsPopup)
            {// 如果当前窗口不为空，并且不会被直接打开的覆盖掉，并且要显示的窗口不是弹窗，就先隐藏当前窗口

                CurrentWindow.Hide();
            }

            windowHistory.Push(windowEntry);
            AddTransition(windowEntry.Screen);

            if (windowEntry.Screen.IsPopup)
            {
                priorityParaLayer.DarkenBG();
            }

            windowEntry.Show();

            CurrentWindow = windowEntry.Screen;
        }

        /// <summary>
        /// 进入动画结束后，移除过渡效果
        /// </summary>
        /// <param name="screen"></param>
        private void OnInAnimationFinished(IScreenController screen)
        {
            RemoveTransition(screen);
        }

        /// <summary>
        /// 退出动画结束后，移除过渡效果，如果是弹窗，还要刷新优先级层级的暗化效果
        /// </summary>
        /// <param name="screen"></param>
        private void OnOutAnimationFinished(IScreenController screen)
        {
            RemoveTransition(screen);
            var window = screen as IWindowController;
            if (window.IsPopup)
            {
                priorityParaLayer.RefreshDarken();
            }
        }

        private void OnCloseRequestedByWindow(IScreenController screen)
        {
            HideScreen(screen as IWindowController);
        }

        private void AddTransition(IScreenController screen)
        {
            screensTransitioning.Add(screen);

            RequestScreenBlock?.Invoke();

        }

        private void RemoveTransition(IScreenController screen)
        {
            screensTransitioning.Remove(screen);
            if (!IsScreenTransitionInProgress)
            {
                RequestScreenUnblock?.Invoke();
            }
        }
    }
}