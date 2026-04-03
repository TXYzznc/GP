using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using System;
using System.Collections.Generic;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class SummonChessStateUI : UIItemBase
{
    [Header("HP格子配置")]
    [SerializeField]
    [Tooltip("单格代表的血量（固定血量/格）。格子数 = ceil(MaxHp / 该值)，总宽不变；该值越小格子越多、越密集。")]
    private float m_HpPerCell = 50f;

    [SerializeField]
    [Tooltip("分隔线宽度（UV）。越大线越粗。")]
    private float m_LineWidthUV = 0.006f;

    [SerializeField]
    [Tooltip("分隔线颜色（含透明度）。")]
    private Color m_GridLineColor = new Color(0, 0, 0, 0.85f);

    [Header("行为配置")]
    [SerializeField]
    [Tooltip("是否始终面向主相机（世界空间UI建议开启）。")]
    private bool m_Billboard = true;

    private bool m_FollowEnabled;
    private Transform m_FollowTarget;
    private Vector3 m_FollowOffset;

    private ChessEntity m_Owner;
    private ChessAttribute m_Attr;

    private Image m_HpImg;
    private Image m_MpImg;
    private Image m_ShieldImg;

    private Material m_HpMatInst;
    private Camera m_Cam;

    // Buff管理
    private Dictionary<int, BuffItem> m_BuffItems = new Dictionary<int, BuffItem>();

    protected override void OnInit()
    {
        base.OnInit();
        m_Cam = CameraRegistry.PlayerCamera;

        if (varHPBarImage != null) m_HpImg = varHPBarImage.GetComponent<Image>();
        if (varMPBarImage != null) m_MpImg = varMPBarImage.GetComponent<Image>();
        if (varShieldBarImage != null) m_ShieldImg = varShieldBarImage.GetComponent<Image>();

        if (m_HpImg != null) { m_HpImg.type = Image.Type.Filled; m_HpImg.fillMethod = Image.FillMethod.Horizontal; }
        if (m_MpImg != null) { m_MpImg.type = Image.Type.Filled; m_MpImg.fillMethod = Image.FillMethod.Horizontal; }
        if (m_ShieldImg != null) { m_ShieldImg.type = Image.Type.Filled; m_ShieldImg.fillMethod = Image.FillMethod.Horizontal; }

        SetupHpMaterial();
        TryBindOwner();
        RefreshAll();
    }

    private void SetupHpMaterial()
    {
        if (m_HpImg == null) return;

        Shader s = m_HpImg.material != null ? m_HpImg.material.shader : Shader.Find("Custom/HealthBarGrid");
        if (s == null)
        {
            DebugEx.WarningModule("SummonChessStateUI", "未找到 Custom/HealthBarGrid Shader，HP将不绘制格子");
            return;
        }

        m_HpMatInst = new Material(s);
        m_HpImg.material = m_HpMatInst;
        m_HpMatInst.SetColor("_GridLineColor", m_GridLineColor);
        m_HpMatInst.SetFloat("_LineWidth", m_LineWidthUV);
    }

    private void TryBindOwner()
    {
        if (m_Owner == null) m_Owner = GetComponentInParent<ChessEntity>();
        if (m_Owner == null) return;
        Bind(m_Owner);
    }

    private void OnDestroy()
    {
        Unbind();
        if (m_HpMatInst != null)
        {
            Destroy(m_HpMatInst);
            m_HpMatInst = null;
        }
    }

    private void LateUpdate()
    {
        if (m_FollowEnabled && m_FollowTarget != null)
        {
            transform.position = m_FollowTarget.position + m_FollowOffset;
        }

        if (m_Billboard)
        {
            if (m_Cam == null) m_Cam = CameraRegistry.PlayerCamera;
            if (m_Cam != null) transform.forward = m_Cam.transform.forward;
        }
    }

    private void OnHpChanged(double oldVal, double newVal)
    {
        UpdateHpFill();
    }

    private void OnMpChanged(double oldVal, double newVal)
    {
        UpdateMpFill();
    }

    private void OnShieldChanged(double oldVal, double newVal)
    {
        UpdateShieldFill();
    }

    private void RefreshAll()
    {
        UpdateHpGridParams();
        UpdateHpFill();
        UpdateMpFill();
        UpdateShieldFill();
    }

    private void UpdateHpGridParams()
    {
        if (m_HpMatInst == null || m_Attr == null) return;
        float maxHp = Mathf.Max(1f, (float)m_Attr.MaxHp);
        int cells = Mathf.Max(1, Mathf.CeilToInt(maxHp / Mathf.Max(1f, m_HpPerCell)));
        float gridWidth = 1f / cells;
        m_HpMatInst.SetFloat("_GridWidth", gridWidth);
    }

    private void UpdateHpFill()
    {
        if (m_HpImg == null || m_Attr == null) return;
        m_HpImg.fillAmount = (float)(m_Attr.CurrentHp / Mathf.Max(1f, (float)m_Attr.MaxHp));
    }

    private void UpdateMpFill()
    {
        if (m_MpImg == null || m_Attr == null) return;
        m_MpImg.fillAmount = (float)(m_Attr.CurrentMp / Mathf.Max(1f, (float)m_Attr.MaxMp));
    }

    private void UpdateShieldFill()
    {
        if (m_ShieldImg == null || m_Attr == null) return;
        // 护盾条显示 Shield / MaxHp（独立于血条）
        // 例如：MaxHp=500, CurrentHp=400, Shield=100 → HPBar=0.8, ShieldBar=0.2
        m_ShieldImg.fillAmount = (float)(m_Attr.Shield / Mathf.Max(1f, (float)m_Attr.MaxHp));
    }

    public void Bind(ChessEntity owner)
    {
        if (owner == null) return;

        if (m_Attr != null)
        {
            m_Attr.OnHpChanged -= OnHpChanged;
            m_Attr.OnMpChanged -= OnMpChanged;
            m_Attr.OnShieldChanged -= OnShieldChanged;
        }

        m_Owner = owner;
        m_Attr = owner.Attribute;
        if (m_Attr == null) return;

        m_Attr.OnHpChanged += OnHpChanged;
        m_Attr.OnMpChanged += OnMpChanged;
        m_Attr.OnShieldChanged += OnShieldChanged;

        // 订阅Buff事件
        BindBuffManager();

        UpdateHpGridParams();
        RefreshAll();
    }

    public void Unbind()
    {
        if (m_Attr != null)
        {
            m_Attr.OnHpChanged -= OnHpChanged;
            m_Attr.OnMpChanged -= OnMpChanged;
            m_Attr.OnShieldChanged -= OnShieldChanged;
        }

        // 取消订阅Buff事件
        UnbindBuffManager();
        ClearAllBuffItems();

        m_Owner = null;
        m_Attr = null;
        SetFollowTarget(null, Vector3.zero);
    }

    public void SetBarsVisible(bool showHp, bool showMp, bool showShield)
    {
        if (varHPBarParent != null) varHPBarParent.gameObject.SetActive(showHp);
        if (varMPBarParent != null) varMPBarParent.gameObject.SetActive(showMp);
        if (varOtherBarParent != null) varOtherBarParent.gameObject.SetActive(showShield);
    }

    public void SetFollowTarget(Transform target, Vector3 offset)
    {
        m_FollowTarget = target;
        m_FollowOffset = offset;
        m_FollowEnabled = target != null;
    }

    public void SetBillboard(bool billboard)
    {
        m_Billboard = billboard;
    }

    #region Buff管理

    /// <summary>
    /// 订阅BuffManager事件并初始化已有的Buff
    /// </summary>
    private void BindBuffManager()
    {
        if (m_Owner == null || m_Owner.BuffManager == null) return;

        var buffManager = m_Owner.BuffManager;

        // 订阅事件
        buffManager.OnBuffAdded += OnBuffAdded;
        buffManager.OnBuffRemoved += OnBuffRemoved;
        buffManager.OnBuffStackChanged += OnBuffStackChanged;

        // 初始化已有的Buff
        RefreshAllBuffs();
    }

    /// <summary>
    /// 取消订阅BuffManager事件
    /// </summary>
    private void UnbindBuffManager()
    {
        if (m_Owner == null || m_Owner.BuffManager == null) return;

        var buffManager = m_Owner.BuffManager;

        // 取消订阅事件
        buffManager.OnBuffAdded -= OnBuffAdded;
        buffManager.OnBuffRemoved -= OnBuffRemoved;
        buffManager.OnBuffStackChanged -= OnBuffStackChanged;
    }

    /// <summary>
    /// 刷新所有Buff显示
    /// </summary>
    private void RefreshAllBuffs()
    {
        ClearAllBuffItems();

        if (m_Owner == null || m_Owner.BuffManager == null) return;

        var allBuffs = m_Owner.BuffManager.GetAllBuffs();
        foreach (var buff in allBuffs)
        {
            AddBuffItem(buff.BuffId, buff.StackCount);
        }
    }

    /// <summary>
    /// Buff被添加时的回调
    /// </summary>
    private void OnBuffAdded(int buffId)
    {
        if (!m_BuffItems.ContainsKey(buffId))
        {
            AddBuffItem(buffId, 1);
        }
    }

    /// <summary>
    /// Buff被移除时的回调
    /// </summary>
    private void OnBuffRemoved(int buffId)
    {
        RemoveBuffItem(buffId);
    }

    /// <summary>
    /// Buff堆叠层数变化时的回调
    /// </summary>
    private void OnBuffStackChanged(int buffId, int newStackCount)
    {
        if (m_BuffItems.TryGetValue(buffId, out var buffItem))
        {
            buffItem.SetStackCount(newStackCount);
        }
    }

    /// <summary>
    /// 添加单个BuffItem
    /// </summary>
    private void AddBuffItem(int buffId, int stackCount)
    {
        if (varBuffPanel == null || varBuffItem == null) return;

        // 检查是否已存在
        if (m_BuffItems.ContainsKey(buffId))
        {
            return;
        }

        // 实例化BuffItem
        GameObject buffItemGo = Instantiate(varBuffItem, varBuffPanel.transform, false);
        BuffItem buffItem = buffItemGo.GetComponent<BuffItem>();

        if (buffItem != null)
        {
            buffItem.SetData(buffId);
            buffItem.SetStackCount(stackCount);
            m_BuffItems[buffId] = buffItem;
            buffItemGo.SetActive(true);
        }
    }

    /// <summary>
    /// 移除单个BuffItem
    /// </summary>
    private void RemoveBuffItem(int buffId)
    {
        if (m_BuffItems.TryGetValue(buffId, out var buffItem))
        {
            if (buffItem != null && buffItem.gameObject != null)
            {
                Destroy(buffItem.gameObject);
            }
            m_BuffItems.Remove(buffId);
        }
    }

    /// <summary>
    /// 清除所有BuffItem
    /// </summary>
    private void ClearAllBuffItems()
    {
        foreach (var buffItem in m_BuffItems.Values)
        {
            if (buffItem != null && buffItem.gameObject != null)
            {
                Destroy(buffItem.gameObject);
            }
        }
        m_BuffItems.Clear();
    }

    #endregion
}