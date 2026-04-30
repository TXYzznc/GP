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
    private Material m_ShieldMatInst;
    private Camera m_Cam;

    private ChessEXPComponent m_EXPComp;

    private static readonly string[] RankNames = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };

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
        if (varChessEXP != null) { varChessEXP.type = Image.Type.Filled; varChessEXP.fillMethod = Image.FillMethod.Horizontal; }

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
            DebugEx.Warning("SummonChessStateUI", "未找到 Custom/HealthBarGrid Shader，HP将不绘制格子");
            return;
        }

        m_HpMatInst = new Material(s);
        m_HpImg.material = m_HpMatInst;
        m_HpMatInst.SetColor("_GridLineColor", m_GridLineColor);
        m_HpMatInst.SetFloat("_LineWidth", m_LineWidthUV);

        // 护盾条也使用相同的 Shader 和参数
        if (m_ShieldImg != null)
        {
            m_ShieldMatInst = new Material(s);
            m_ShieldImg.material = m_ShieldMatInst;
            m_ShieldMatInst.SetColor("_GridLineColor", m_GridLineColor);
            m_ShieldMatInst.SetFloat("_LineWidth", m_LineWidthUV);
        }
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
        if (m_ShieldMatInst != null)
        {
            Destroy(m_ShieldMatInst);
            m_ShieldMatInst = null;
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
        RefreshAll();
    }

    private void OnMpChanged(double oldVal, double newVal)
    {
        UpdateMpFill();
    }

    private void OnShieldChanged(double oldVal, double newVal)
    {
        RefreshAll();
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
        // 格子数基于 (Shield + MaxHp) 总容量
        double totalCapacity = m_Attr.Shield + m_Attr.MaxHp;
        int cells = Mathf.Max(1, Mathf.CeilToInt((float)totalCapacity / Mathf.Max(1f, m_HpPerCell)));
        float gridWidth = 1f / cells;
        m_HpMatInst.SetFloat("_GridWidth", gridWidth);

        // 护盾条使用相同的格子宽度
        if (m_ShieldMatInst != null)
        {
            m_ShieldMatInst.SetFloat("_GridWidth", gridWidth);
        }
    }

    private void UpdateHpFill()
    {
        if (m_HpImg == null || m_Attr == null) return;
        // 血条 = CurrentHp / (Shield + MaxHp)
        double totalCapacity = m_Attr.Shield + m_Attr.MaxHp;
        m_HpImg.fillAmount = (float)(m_Attr.CurrentHp / Math.Max(1f, totalCapacity));
    }

    private void UpdateMpFill()
    {
        if (m_MpImg == null || m_Attr == null) return;
        m_MpImg.fillAmount = (float)(m_Attr.CurrentMp / Mathf.Max(1f, (float)m_Attr.MaxMp));
    }

    private void UpdateShieldFill()
    {
        if (m_ShieldImg == null || m_Attr == null) return;
        // 护盾条 = Shield / (Shield + MaxHp)
        // 例如：MaxHp=500, CurrentHp=400, Shield=100 → 总容量=600
        // HPBar=400/600=0.667, ShieldBar=100/600=0.167
        double totalCapacity = m_Attr.Shield + m_Attr.MaxHp;
        m_ShieldImg.fillAmount = (float)(m_Attr.Shield / Math.Max(1f, totalCapacity));
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

        // 订阅EXP事件
        m_EXPComp = owner.GetComponent<ChessEXPComponent>();
        if (m_EXPComp != null)
        {
            m_EXPComp.OnEXPChanged += OnEXPChanged;
            DebugEx.Log("SummonChessStateUI", $"[Bind] ✅ 棋子 {owner.Config.Name} 的经验事件已订阅");
        }
        else
        {
            DebugEx.Error("SummonChessStateUI", $"[Bind] ❌ 棋子 {owner.Config.Name} 没有 ChessEXPComponent");
        }

        UpdateHpGridParams();
        RefreshAll();
        UpdateEXPDisplay();
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

        // 取消订阅EXP事件
        if (m_EXPComp != null)
        {
            m_EXPComp.OnEXPChanged -= OnEXPChanged;
            m_EXPComp = null;
        }

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

    #region EXP显示

    private void OnEXPChanged(int oldExp, int newExp)
    {
        DebugEx.Log("SummonChessStateUI", $"[OnEXPChanged] 棋子={m_Owner?.Config?.Name ?? "null"}, 经验: {oldExp} → {newExp}");
        UpdateEXPDisplay();
    }

    private void UpdateEXPDisplay()
    {
        if (m_Owner == null) return;

        if (varChessName != null)
        {
            string chessName = m_Owner.Config?.Name ?? string.Empty;
            if (m_Owner.Rank > 0 && m_Owner.Rank < RankNames.Length)
                varChessName.text = $"{chessName}·{RankNames[m_Owner.Rank]}阶";
            else
                varChessName.text = chessName;
        }

        int currentEXP = m_EXPComp?.CurrentEXP ?? 0;

        // 获取升到下一级所需的经验值
        int requiredEXP = 0;
        var dt = GF.DataTable.GetDataTable<ChessAdvanceTable>();
        var dr = dt?.GetDataRow(m_Owner.ChessId);
        if (dr != null && m_Owner.Rank > 0 && m_Owner.Rank < 3)
        {
            int rankIndex = m_Owner.Rank - 1; // Rank 1->索引0, Rank 2->索引1
            if (rankIndex >= 0 && rankIndex < dr.RequiredEXP.Length)
                requiredEXP = dr.RequiredEXP[rankIndex];
        }

        if (varChessEXP != null)
            varChessEXP.fillAmount = requiredEXP > 0 ? Mathf.Clamp01((float)currentEXP / requiredEXP) : 1f;

        if (varChessEXPText != null)
            varChessEXPText.text = requiredEXP > 0 ? $"{currentEXP}/{requiredEXP}" : $"{currentEXP}/--";
    }

    #endregion

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