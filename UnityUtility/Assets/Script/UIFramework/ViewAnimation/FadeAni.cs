using System;
using UnityEngine;

namespace UIFramework
{
    /// <summary>
    /// 渐入动画，同样愿意自己封装DoTween的也行
    /// </summary>
    public class FadeAni : AniComponent
    {
        /// <summary>
        /// 渐变的持续时间，单位秒
        /// </summary>
        [SerializeField] 
        private float fadeDuration = 0.5f;
        [SerializeField] 
        private bool fadeOut = false;

        private CanvasGroup canvasGroup;
        /// <summary>
        /// 完成渐变的时间
        /// </summary>
        private float timer;
        private Action currentAction;
        private Transform currentTarget;

        private float startValue;
        private float endValue;

        private bool shouldAnimate;

        public override void Animate(Transform target, Action callWhenFinished)
        {
            if (currentAction != null)
            {// 如果上一个动画还在播放，先结束它，调用这个回调函数来通知它结束了。

                canvasGroup.alpha = endValue;
                currentAction();
            }

            canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            if (fadeOut)
            {// 如果是淡出动画，1-0

                startValue = 1f;
                endValue = 0f;
            }
            else
            {// 如果是淡入动画，0-1

                startValue = 0f;
                endValue = 1f;
            }

            currentAction = callWhenFinished;
            timer = fadeDuration;

            canvasGroup.alpha = startValue;
            shouldAnimate = true;
        }

        private void Update()
        {
            if (!shouldAnimate)
            {
                return;
            }

            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(endValue, startValue, timer / fadeDuration);
            }
            else
            {
                canvasGroup.alpha = 1f;
                currentAction?.Invoke();   

                currentAction = null;
                shouldAnimate = false;
            }
        }
    }
}