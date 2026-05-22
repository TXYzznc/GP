using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 黑暗杨戬武器管理组件
/// 控制两个武器的显示/隐藏，支持溶解动画效果
/// </summary>
public class DarkYangyuanWeaponComponent : MonoBehaviour
{
    #region 序列化字段

    /// <summary>两个武器GameObject（索引0=武器1戟，索引1=武器二）</summary>
    [SerializeField]
    private GameObject[] m_Weapons = new GameObject[2];

    /// <summary>溶解动画时长（秒）</summary>
    [SerializeField]
    private float m_DissolveDuration = 0.3f;

    #endregion

    #region 私有字段

    /// <summary>动画事件接收器</summary>
    private DarkYangyuanAnimationEventReceiver m_EventReceiver;

    /// <summary>各武器当前的溶解进度（0=完全显示，1=完全隐藏）</summary>
    private float[] m_DissolveProgress = new float[2];

    #endregion

    #region Unity 生命周期

    private void Start()
    {
        // 获取动画事件接收器
        var animator = GetComponent<ChessAnimator>();
        m_EventReceiver = animator?.EventReceiver as DarkYangyuanAnimationEventReceiver;

        if (m_EventReceiver == null)
        {
            DebugEx.Error("DarkYangyuanWeaponComponent", "未找到 DarkYangyuanAnimationEventReceiver");
            return;
        }

        // 初始化：只显示武器1
        SetWeaponVisibility(0, true, immediate: true);
        SetWeaponVisibility(1, false, immediate: true);

        // 订阅武器显示/隐藏事件
        m_EventReceiver.OnWeaponShow += OnWeaponShow;
        m_EventReceiver.OnWeaponHide += OnWeaponHide;

        DebugEx.Log("DarkYangyuanWeaponComponent", "武器管理组件初始化完成");
    }

    private void OnDestroy()
    {
        if (m_EventReceiver != null)
        {
            m_EventReceiver.OnWeaponShow -= OnWeaponShow;
            m_EventReceiver.OnWeaponHide -= OnWeaponHide;
        }
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 武器显示事件回调
    /// </summary>
    private void OnWeaponShow(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= m_Weapons.Length)
        {
            DebugEx.Warning("DarkYangyuanWeaponComponent", $"武器索引 {weaponIndex} 超出范围");
            return;
        }

        SetWeaponVisibility(weaponIndex, show: true, immediate: false);
    }

    /// <summary>
    /// 武器隐藏事件回调
    /// </summary>
    private void OnWeaponHide(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= m_Weapons.Length)
        {
            DebugEx.Warning("DarkYangyuanWeaponComponent", $"武器索引 {weaponIndex} 超出范围");
            return;
        }

        SetWeaponVisibility(weaponIndex, show: false, immediate: false);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 设置武器可见性（支持溶解动画）
    /// </summary>
    /// <param name="weaponIndex">武器索引</param>
    /// <param name="show">true=显示，false=隐藏</param>
    /// <param name="immediate">true=立即切换（无动画），false=溶解动画</param>
    private void SetWeaponVisibility(int weaponIndex, bool show, bool immediate)
    {
        var weapon = m_Weapons[weaponIndex];
        if (weapon == null)
            return;

        if (immediate)
        {
            weapon.SetActive(show);
            m_DissolveProgress[weaponIndex] = show ? 0f : 1f;
            DebugEx.Log("DarkYangyuanWeaponComponent",
                $"武器 {weaponIndex} 立即{(show ? "显示" : "隐藏")}");
        }
        else
        {
            DissolveTo(weaponIndex, show ? 0f : 1f);
        }
    }

    /// <summary>
    /// 溶解动画：平滑过渡武器的显示/隐藏
    /// </summary>
    private async void DissolveTo(int weaponIndex, float targetProgress)
    {
        var weapon = m_Weapons[weaponIndex];
        if (weapon == null)
            return;

        // 如果目标是显示（targetProgress=0），先激活GameObject
        if (targetProgress < 0.5f && !weapon.activeInHierarchy)
        {
            weapon.SetActive(true);
        }

        float startProgress = m_DissolveProgress[weaponIndex];
        float elapsed = 0f;

        while (elapsed < m_DissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_DissolveDuration);
            m_DissolveProgress[weaponIndex] = Mathf.Lerp(startProgress, targetProgress, t);

            ApplyDissolveEffect(weaponIndex, m_DissolveProgress[weaponIndex]);

            await UniTask.Yield();
        }

        m_DissolveProgress[weaponIndex] = targetProgress;
        ApplyDissolveEffect(weaponIndex, targetProgress);

        // 如果目标是隐藏（targetProgress=1），停用GameObject
        if (targetProgress > 0.5f)
        {
            weapon.SetActive(false);
        }
    }

    /// <summary>
    /// 应用溶解效果到武器材质
    /// 注：需要配合支持 _DissolveProgress 参数的 Shader 使用
    /// </summary>
    private void ApplyDissolveEffect(int weaponIndex, float dissolveProgress)
    {
        var weapon = m_Weapons[weaponIndex];
        if (weapon == null)
            return;

        // 获取武器上的所有 Renderer
        var renderers = weapon.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                // 如果材质支持 _DissolveProgress 参数，设置它
                if (material.HasProperty("_DissolveProgress"))
                {
                    material.SetFloat("_DissolveProgress", dissolveProgress);
                }

                // 备选方案：如果没有专门的溶解shader，可用透明度渐变
                // material.color = new Color(material.color.r, material.color.g, material.color.b,
                //     Mathf.Lerp(1f, 0f, dissolveProgress));
            }
        }
    }

    #endregion
}
