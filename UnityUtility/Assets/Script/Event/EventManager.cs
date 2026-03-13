using System;
using System.Collections.Generic;
using CommonUtility;

namespace Event
{
    /// <summary>
/// 核心事件管理器，采用单例模式
/// 提供基于字符串键的事件订阅、取消订阅和触发功能
/// 注意：每个事件名只能有一个处理器（后订阅的会覆盖前订阅）
/// </summary>
public class EventManager : Singleton<EventManager>
{
    /// <summary>
    /// 事件字典，存储事件名对应的处理器
    /// key: 事件名称，value: 接收object[]参数的回调函数
    /// </summary>
    private Dictionary<string, Action<object[]>> _events;

    /// <summary>
    /// 初始化事件管理器
    /// </summary>
    public void Init()
    {
        _events = new Dictionary<string, Action<object[]>>();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="listener">事件处理器回调函数</param>
    public void Subscribe(string eventName, Action<object[]> listener)
    {
        if (_events.ContainsKey(eventName))
        {
            // 如果已存在同名事件，替换旧的处理器（单处理器设计）
            _events[eventName] = listener;
            //LogHelper.Log("EventManager: Event " + eventName + " has been replaced.", LogUser.EventManager);
        }
        else
        {
            _events.Add(eventName, listener);
            //LogHelper.Log("EventManager: Event " + eventName + " has been subscribed.", LogUser.EventManager);
        }
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <param name="eventName">要取消订阅的事件名称</param>
    public void Unsubscribe(string eventName)
    {
        if (_events.ContainsKey(eventName))
        {
            _events.Remove(eventName);
            //LogHelper.Log("EventManager: Event " + eventName + " has been unsubscribed.", LogUser.EventManager);
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName">要触发的事件名称</param>
    /// <param name="parameters">事件参数数组</param>
    public void TriggerEvent(string eventName, params object[] parameters)
    {
        if (_events.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent?.Invoke(parameters);
            //LogHelper.Log("EventManager: Event " + eventName + " has been triggered.", LogUser.EventManager);
        }
    }
}
}
