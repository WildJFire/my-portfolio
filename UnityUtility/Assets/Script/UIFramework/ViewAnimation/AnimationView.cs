using System;
using System.Collections;
using UnityEngine;

namespace UIFramework.Examples
{
    /// <summary>
    /// 具体动画实现，自己封装DoTween之类的都行，这个随意。
    /// </summary>
    public class AnimationView : AniComponent
    {
        [SerializeField]
        private AnimationClip clip = null;
        [SerializeField]
        private bool playReverse = false;

        /// <summary>
        /// 如果上一个动画还在播放，先结束它，调用这个回调函数来通知它结束了。
        /// </summary>
        private Action previousCallbackWhenFinished;

        /// <summary>
        /// 播放动画，如果上一个动画还在播放，先结束它
        /// </summary>
        /// <param name="target"></param>
        /// <param name="callWhenFinished"></param>
        public override void Animate(Transform target, Action callWhenFinished)
        {
            FinishPrevious();
            var targetAnimation = target.GetComponent<Animation>();
            if (targetAnimation == null)
            {
                Debug.LogError("[LegacyAnimationScreenTransition] No Animation component in " + target);
                callWhenFinished?.Invoke();
                return;
            }

            targetAnimation.clip = clip;
            StartCoroutine(PlayAnimationRoutine(targetAnimation, callWhenFinished));
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="targetAnimation"></param>
        /// <param name="callWhenFinished"></param>
        /// <returns></returns>
        private IEnumerator PlayAnimationRoutine(Animation targetAnimation, Action callWhenFinished)
        {
            previousCallbackWhenFinished = callWhenFinished;
            foreach (AnimationState state in targetAnimation)
            {
                state.time = playReverse ? state.clip.length : 0f;
                state.speed = playReverse ? -1f : 1f;
            }

            targetAnimation.Play(PlayMode.StopAll);
            yield return new WaitForSeconds(targetAnimation.clip.length);
            // 动画结束，调用回调函数
            FinishPrevious();
        }

        /// <summary>
        /// 如果上一个动画还在播放，先结束它，调用这个方法来通知它结束了，并结束协程。
        /// </summary>
        private void FinishPrevious()
        {
            previousCallbackWhenFinished?.Invoke();
            previousCallbackWhenFinished = null;
            StopAllCoroutines();
        }
    }
}