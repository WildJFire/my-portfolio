using UnityEngine;


namespace UIFramework
{
    /// <summary>
    /// 这个Layer层是控制面板的
    /// 面板是界面的一种，没有历史记录，没有队列,
    /// 就是简单的显示在界面中
    /// 比如说体力槽，小地图这种常驻的
    /// </summary>
    public class PanelUILayer : UILayer<IPanelController>
    {
        [SerializeField]
        [Tooltip("优先级并行层的设置。注册到此层的面板将根据其优先级重新归属到不同的并行层对象.")]
        private PanelPriorityLayerList priorityLayers = null;

        /// <summary>
        /// 重新设置父对象，如果是面板的话，直接放在对应的层级上
        /// </summary>
        /// <param name="controller">面板实例</param>
        /// <param name="screenTransform">父节点</param>
        public override void ReparentScreen(IScreenController controller, Transform screenTransform)
        {
            var ctl = controller as IPanelController;
            if (ctl != null)
            {// 如果是面板的话，直接放在对应的层级上

                ReparentToParaLayer(ctl.Priority, screenTransform);
            }
            else
            {
                base.ReparentScreen(controller, screenTransform);
            }
        }

        /// <summary>
        /// 根据优先级重新设置父对象
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="screenTransform"></param>
        private void ReparentToParaLayer(PanelPriority priority, Transform screenTransform)
        {
            if (!priorityLayers.ParaLayerLookup.TryGetValue(priority, out Transform trans))
            {
                trans = transform;
            }

            screenTransform.SetParent(trans, false);
        }

        public override void HideScreen(IPanelController screen)
        {
            screen.Hide();
        }

        public override void ShowScreen(IPanelController screen)
        {
            screen.Show();
        }

        public override void ShowScreen<TProps>(IPanelController screen, TProps properties)
        {
            screen.Show(properties);
        }


        public bool IsPanelVisible(string panelId)
        {
            if (registeredScreens.TryGetValue(panelId, out IPanelController panel))
            {
                return panel.IsVisible;
            }

            return false;
        }
    }
}