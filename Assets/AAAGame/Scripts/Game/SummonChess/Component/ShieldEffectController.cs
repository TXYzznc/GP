using UnityEngine;
using Cysharp.Threading.Tasks;
using GameExtension;

/// <summary>
/// 护盾特效控制器
/// 监听棋子的护盾值变化，动态显示/隐藏护盾特效
/// 护盾特效资源 ID：
///   - 3201: 护盾获得特效 (Shield.prefab)
///   - 3202: 护盾受击特效 (ShieldHit.prefab)
/// </summary>
public class ShieldEffectController : MonoBehaviour
{
    private const int SHIELD_EFFECT_ID = 3201;      // 护盾获得特效
    private const int SHIELD_HIT_EFFECT_ID = 3202;  // 护盾受击特效

    private ChessEntity m_Chess;
    private GameObject m_ShieldEffectInstance;
    private ParticleSystem m_ShieldParticleSystem;
    private bool m_IsLoading = false;

    /// <summary>初始化护盾特效控制器</summary>
    public void Initialize(ChessEntity chess)
    {
        m_Chess = chess;

        if (m_Chess?.Attribute == null)
        {
            DebugEx.Error(nameof(ShieldEffectController), "Initialize: chess 或 chess.Attribute 为 null");
            return;
        }

        // 监听护盾变化事件
        m_Chess.Attribute.OnShieldChanged += OnShieldChanged;

        // 监听护盾受击事件
        m_Chess.Attribute.OnShieldHit += OnShieldHit;

        DebugEx.Log(nameof(ShieldEffectController), $"[{m_Chess.Config.Name}] 护盾特效控制器初始化");
    }

    /// <summary>护盾值变化回调</summary>
    private void OnShieldChanged(double oldValue, double newValue)
    {
        // 护盾从 0 → >0：显示护盾特效
        if (oldValue == 0 && newValue > 0)
        {
            SpawnShieldEffectAsync().Forget();
        }
        // 护盾从 >0 → 0：隐藏护盾特效
        else if (oldValue > 0 && newValue == 0)
        {
            DestroyShieldEffect();
        }
        // 护盾值变化但不为0：保持显示，无需操作
    }

    /// <summary>护盾受击回调</summary>
    private void OnShieldHit(double damageAbsorbed)
    {
        PlayShieldHitEffect().Forget();
    }

    /// <summary>异步创建护盾特效</summary>
    private async UniTaskVoid SpawnShieldEffectAsync()
    {
        if (m_ShieldEffectInstance != null)
        {
            DebugEx.Log(nameof(ShieldEffectController),
                $"[{m_Chess.Config.Name}] 护盾特效已存在，无需重复创建");
            return;
        }

        if (m_IsLoading)
        {
            DebugEx.Warning(nameof(ShieldEffectController),
                $"[{m_Chess.Config.Name}] 护盾特效正在加载，忽略重复请求");
            return;
        }

        m_IsLoading = true;

        try
        {
            // 异步加载特效预制体
            var prefab = await ResourceExtension.LoadPrefabAsync(SHIELD_EFFECT_ID);

            if (prefab == null)
            {
                DebugEx.Error(nameof(ShieldEffectController),
                    $"[{m_Chess.Config.Name}] 加载护盾特效预制体失败 (ID={SHIELD_EFFECT_ID})");
                return;
            }

            // 检查棋子是否已被销毁
            if (m_Chess == null || gameObject == null)
            {
                DebugEx.Warning(nameof(ShieldEffectController),
                    "棋子已销毁，放弃创建护盾特效");
                return;
            }

            // 实例化特效
            m_ShieldEffectInstance = Instantiate(prefab, transform);
            m_ShieldEffectInstance.name = "ShieldEffect";
            m_ShieldEffectInstance.transform.localPosition = Vector3.zero;
            m_ShieldEffectInstance.transform.localRotation = Quaternion.identity;

            // 获取粒子系统并播放
            m_ShieldParticleSystem = m_ShieldEffectInstance.GetComponent<ParticleSystem>();
            if (m_ShieldParticleSystem != null)
            {
                m_ShieldParticleSystem.Play();
            }

            DebugEx.Log(nameof(ShieldEffectController),
                $"[{m_Chess.Config.Name}] 护盾特效已创建并开始播放");
        }
        catch (System.Exception ex)
        {
            DebugEx.Error(nameof(ShieldEffectController),
                $"[{m_Chess.Config.Name}] 创建护盾特效异常: {ex.Message}");
        }
        finally
        {
            m_IsLoading = false;
        }
    }

    /// <summary>销毁护盾特效</summary>
    private void DestroyShieldEffect()
    {
        if (m_ShieldEffectInstance == null) return;

        // 停止粒子特效（允许现有粒子播放完成）
        if (m_ShieldParticleSystem != null)
        {
            m_ShieldParticleSystem.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
        }

        // 销毁特效对象
        Destroy(m_ShieldEffectInstance);
        m_ShieldEffectInstance = null;
        m_ShieldParticleSystem = null;

        DebugEx.Log(nameof(ShieldEffectController),
            $"[{m_Chess.Config.Name}] 护盾特效已销毁");
    }

    /// <summary>播放护盾受击特效（当护盾吸收伤害时调用）</summary>
    public async UniTaskVoid PlayShieldHitEffect()
    {
        if (m_Chess == null || m_Chess.Attribute?.Shield <= 0) return;

        try
        {
            var prefab = await ResourceExtension.LoadPrefabAsync(SHIELD_HIT_EFFECT_ID);

            if (prefab == null)
            {
                DebugEx.Warning(nameof(ShieldEffectController),
                    $"加载护盾受击特效失败 (ID={SHIELD_HIT_EFFECT_ID})");
                return;
            }

            // 在棋子位置创建受击特效（不作为子物体，自动销毁）
            var hitEffect = Instantiate(prefab, transform.position, Quaternion.identity);
            var ps = hitEffect.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                ps.Play();

                // 获取特效持续时间，自动销毁
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(hitEffect, duration + 0.5f);
            }
            else
            {
                Destroy(hitEffect, 2f);
            }

            DebugEx.Log(nameof(ShieldEffectController),
                $"[{m_Chess.Config.Name}] 护盾受击特效已播放");
        }
        catch (System.Exception ex)
        {
            DebugEx.Warning(nameof(ShieldEffectController),
                $"播放护盾受击特效异常: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        // 清理事件监听
        if (m_Chess?.Attribute != null)
        {
            m_Chess.Attribute.OnShieldChanged -= OnShieldChanged;
            m_Chess.Attribute.OnShieldHit -= OnShieldHit;
        }

        // 清理特效对象
        if (m_ShieldEffectInstance != null)
        {
            Destroy(m_ShieldEffectInstance);
        }
    }
}
