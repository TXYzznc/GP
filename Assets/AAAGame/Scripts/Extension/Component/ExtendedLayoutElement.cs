using UnityEngine;
using UnityEngine.UI;

namespace Assets.AAAGame.Scripts.Extension.Component
{
    /// <summary>
    /// 扩展的Layout Element，支持高度最大最小值限制
    /// 通过LateUpdate拦截并调整高度来实现约束
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ExtendedLayoutElement : MonoBehaviour, ILayoutElement
    {
        [SerializeField]
        private bool m_EnableHeightConstraint = false;

        [SerializeField]
        private float m_MinHeight = 0;

        [SerializeField]
        private float m_MaxHeight = float.MaxValue;

        private RectTransform m_RectTransform;
        private LayoutGroup m_ParentLayoutGroup;

        public float minWidth => -1;
        public float preferredWidth => -1;
        public float maxWidth => -1;

        public float minHeight => -1;
        public float preferredHeight => -1;
        public float maxHeight => -1;

        public float flexibleWidth => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => 0;

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }

        private void OnEnable()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_ParentLayoutGroup = GetComponentInParent<LayoutGroup>();

            if (m_ParentLayoutGroup != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(m_ParentLayoutGroup.GetComponent<RectTransform>());
            }
        }

        private void OnDisable()
        {
            if (m_ParentLayoutGroup != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(m_ParentLayoutGroup.GetComponent<RectTransform>());
            }
        }

        private void LateUpdate()
        {
            ApplyHeightConstraint();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 在编辑模式下立刻生效，但不能在OnValidate中修改RectTransform
            if (!Application.isPlaying)
            {
                m_RectTransform = GetComponent<RectTransform>();
                m_ParentLayoutGroup = GetComponentInParent<LayoutGroup>();

                if (m_ParentLayoutGroup != null)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(m_ParentLayoutGroup.GetComponent<RectTransform>());
                }
            }
        }
#endif

        private void ApplyHeightConstraint()
        {
            if (!m_EnableHeightConstraint || m_RectTransform == null)
                return;

            var sizeDelta = m_RectTransform.sizeDelta;
            float currentHeight = sizeDelta.y;

            // 应用高度约束
            float constrainedHeight = currentHeight;
            if (currentHeight < m_MinHeight)
            {
                constrainedHeight = m_MinHeight;
            }
            else if (currentHeight > m_MaxHeight)
            {
                constrainedHeight = m_MaxHeight;
            }

            // 只在高度改变时更新
            if (!Mathf.Approximately(currentHeight, constrainedHeight))
            {
                m_RectTransform.sizeDelta = new Vector2(sizeDelta.x, constrainedHeight);
            }
        }

        public void SetHeightConstraint(float minHeight, float maxHeight)
        {
            m_EnableHeightConstraint = true;
            m_MinHeight = minHeight;
            m_MaxHeight = maxHeight;

            if (m_RectTransform == null)
                m_RectTransform = GetComponent<RectTransform>();

            ApplyHeightConstraint();
        }

        public void DisableHeightConstraint()
        {
            m_EnableHeightConstraint = false;
        }
    }
}
