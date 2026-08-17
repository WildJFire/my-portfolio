using System;
using UnityEngine;
using System.Collections.Generic;

namespace UIFramework
{
    /// <summary>
    /// 基础的UI Layer层
    /// </summary>
    public abstract class UILayer<TScreen> : MonoBehaviour where TScreen : IScreenController
    {
        /// <summary>
        /// 注册的界面列表，key是界面ID，value是界面实例
        /// </summary>
        protected Dictionary<string, TScreen> registeredScreens;
        

        /// <summary>
        /// 显示界面
        /// </summary>
        /// <param name="screen">界面类型参数</param>
        public abstract void ShowScreen(TScreen screen);

        /// <summary>
        /// 显示一个界面，带一些参数
        /// </summary>
        /// <param name="screen">界面类型参数</param>
        /// <param name="properties">属性参数</param>
        /// <typeparam name="TProps">属性类型</typeparam>
        public abstract void ShowScreen<TProps>(TScreen screen, TProps properties) where TProps : IScreenProperties;

        /// <summary>
        /// 隐藏界面
        /// </summary>
        /// <param name="screen">界面类型参数</param>
        public abstract void HideScreen(TScreen screen);

        /// <summary>
        /// 初始化Layer层
        /// </summary>
        public virtual void Initialize()
        {
            registeredScreens = new Dictionary<string, TScreen>();
        }

        /// <summary>
        /// 将传进来的界面节点设置为当前层的子节点
        /// </summary>
        /// <param name="controller">界面的controller</param>
        /// <param name="screenTransform">界面节点</param>
        public virtual void ReparentScreen(IScreenController controller, Transform screenTransform)
        {
            screenTransform.SetParent(this.transform, false);
        }

        #region 显示相关

        /// <summary>
        /// 根据id去找界面的controller,并且显示出来
        /// </summary>
        /// <param name="screenId">界面Id</param>
        public void ShowScreenById(string screenId)
        {
            if (registeredScreens.TryGetValue(screenId, out TScreen ctl))
            {// 找到就显示
                ShowScreen(ctl);
            }
            else
            {
                Debug.LogError("[AUILayerController] Screen ID " + screenId + " not registered to this layer!");
            }
        }

        /// <summary>
        /// 根据界面id显示具体的controller,带上具体的属性参数
        /// </summary>
        /// <param name="screenId">界面id</param>
        /// <param name="properties">属性参数</param>
        /// <typeparam name="TProps">属性类型</typeparam>
        public void ShowScreenById<TProps>(string screenId, TProps properties) where TProps : IScreenProperties
        {
            TScreen ctl;
            if (registeredScreens.TryGetValue(screenId, out ctl))
            {
                ShowScreen(ctl, properties);
            }
            else
            {
                Debug.LogError("[AUILayerController] Screen ID " + screenId + " not registered!");
            }
        }

        /// <summary>
        /// 根据id隐藏界面
        /// </summary>
        /// <param name="screenId">界面id</param>
        public void HideScreenById(string screenId)
        {
            TScreen ctl;
            if (registeredScreens.TryGetValue(screenId, out ctl))
            {
                HideScreen(ctl);
            }
            else
            {
                Debug.LogError("[AUILayerController] Could not hide Screen ID " + screenId + " as it is not registered to this layer!");
            }
        }

        /// <summary>
        /// 根据id看是否注册了
        /// </summary>
        /// <param name="screenId">界面id</param>
        public bool IsScreenRegistered(string screenId)
        {
            return registeredScreens.ContainsKey(screenId);
        }

        /// <summary>
        /// 隐藏所有界面
        /// </summary>
        /// <param name="shouldAnimateWhenHiding">隐藏的时候是否需要动画</param>
        public virtual void HideAll(bool shouldAnimateWhenHiding = true)
        {
            foreach (var screen in registeredScreens)
            {
                screen.Value.Hide(shouldAnimateWhenHiding);
            }
        }

        #endregion

        #region 注册相关
        /// <summary>
        /// 注册界面的controller带上明确的界面id
        /// </summary>
        /// <param name="screenId">界面id</param>
        /// <param name="controller">界面controller</param>
        public void RegisterScreen(string screenId, TScreen controller)
        {
            if (!registeredScreens.ContainsKey(screenId))
            {
                ProcessScreenRegister(screenId, controller);
            }
            else
            {
                Debug.LogError("[AUILayerController] Screen controller already registered for id: " + screenId);
            }
        }

        /// <summary>
        /// 注册界面
        /// </summary>
        /// <param name="screenId">界面ID</param>
        /// <param name="controller">界面实例</param>
        protected virtual void ProcessScreenRegister(string screenId, TScreen controller)
        {
            controller.ScreenId = screenId;
            registeredScreens.Add(screenId, controller);
            controller.ScreenDestroyed += OnScreenDestroyed;
        }

        /// <summary>
        /// 取消注册界面
        /// </summary>
        /// <param name="screenId">界面ID</param>
        /// <param name="controller">界面实例</param>
        protected virtual void ProcessScreenUnregister(string screenId, TScreen controller)
        {
            controller.ScreenDestroyed -= OnScreenDestroyed;
            registeredScreens.Remove(screenId);
        }

        /// <summary>
        /// 根据id取消注册界面的controller
        /// </summary>
        /// <param name="screenId">界面id</param>
        /// <param name="controller">被取消的界面controller</param>
        public void UnregisterScreen(string screenId, TScreen controller)
        {
            if (registeredScreens.ContainsKey(screenId))
            {// 存在就取消注册

                ProcessScreenUnregister(screenId, controller);
            }
            else
            {
                Debug.LogError("[AUILayerController] Screen controller not registered for id: " + screenId);
            }
        }

        /// <summary>
        /// 当界面被销毁时，取消注册界面
        /// </summary>
        /// <param name="screen">界面实例</param>
        private void OnScreenDestroyed(IScreenController screen)
        {
            if (!string.IsNullOrEmpty(screen.ScreenId)
                && registeredScreens.ContainsKey(screen.ScreenId))
            {// 存在就取消注册
                UnregisterScreen(screen.ScreenId, (TScreen)screen);
            }
        }
        #endregion

    }
}