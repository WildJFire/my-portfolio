using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIComponent
{
    /// <summary>
    /// 进度条组件，支持数值显示和渐变动画效果
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        #region 私有方法与参数

        /// <summary>
        /// Unity UI Slider 组件，用于显示进度条的视觉表现
        /// </summary>
        [SerializeField]
        private Slider _progress;

        /// <summary>
        /// Unity UI TextMeshProUGUI 组件，用于显示当前进度数值文本
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI _progressText;

        [SerializeField]
        private float _amount = 1;

        private float _currentPercent;
        private float _target;
        private bool _isRunnig = false;
       

        /// <summary>
        /// 更新 UI 显示，包括文本和进度条动画
        /// 如果渐变动画未运行，则启动协程执行平滑过渡效果
        /// </summary>
        private void UpdateUI()
        {
            // 更新进度文本显示格式："当前值/最大值"
            _progressText.text = $"{_currentValue:0}/{_maxValue:0}";
            // 计算目标进度百分比
            this._target = this._currentValue / this._maxValue;
            // 如果渐变动画未在运行，则启动协程
            if (!_isRunnig)
            {
                this.StartCoroutine(ValueChanger());
            }
        }

        /// <summary>
        /// 渐变动画协程，平滑过渡进度条到目标值
        /// 使用 Lerp 插值实现渐进式动画效果
        /// </summary>
        private IEnumerator ValueChanger()
        {
            this._isRunnig = true;
            // 循环执行直到当前进度百分比与目标值近似相等
            while (!Mathf.Approximately(_currentPercent, _target)) //Approximately 比较两个浮点数是否近似相等
            {
                // 使用 Lerp 进行平滑插值
                this._currentPercent = Mathf.Lerp(this._currentPercent, this._target, Time.deltaTime * _amount);
                this._progress.value = this._currentPercent;
                yield return null;
            }

            // 确保最终值精确等于目标值
            this._currentPercent = _target;
            _progress.value = this._currentPercent;
            this._isRunnig = false;
        }

        #endregion

        #region 外部调用

        private float _maxValue;

        /// <summary>
        /// 进度条最大值
        /// 设置时会自动限制最小值为 1，并刷新 UI 显示
        /// </summary>
        public float MaxValue
        {
            get { return _maxValue; }
            set
            {
                _maxValue = Mathf.Max(value, 1f);
                UpdateUI();
            }
        }

        private float _currentValue;

        /// <summary>
        /// 当前进度值
        /// 设置时会自动限制在 0 到 MaxValue 范围内，并刷新 UI 显示
        /// </summary>
        public float CurrentValue
        {
            get { return _currentValue; }
            set
            {
                value = Mathf.Clamp(value, 0, _maxValue);
                _currentValue = value;
                UpdateUI();
            }
        }

        /// <summary>
        /// 初始化进度条数据
        /// </summary>
        /// <param name="maxValue">进度条最大值，应大于 0</param>
        /// <param name="currentValue">当前进度值，将在 0 到 maxValue 范围内</param>
        /// <param name="amount">渐变动画速度系数</param>
        public void SetData(float maxValue, float currentValue, float amount)
        {
            this._maxValue = maxValue;
            this._currentValue = currentValue;
            this._currentPercent = currentValue / maxValue;
            this._amount = amount;
            UpdateUI();
        }

        #endregion
    }
}