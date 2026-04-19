using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝箱特效循环器
/// 在指定时间间隔内循环播放多个特效预制体
/// </summary>
public class ChestEffectCycler : MonoBehaviour
{
    [SerializeField]
    [Tooltip("要循环播放的特效预制体列表")]
    private List<GameObject> listOfEffects = new List<GameObject>();

    [SerializeField]
    [Tooltip("循环时长（秒）")]
    private float loopTimeLength = 2f;

    private float m_ElapsedTime = 0f;
    private int m_CurrentEffectIndex = 0;
    private GameObject m_CurrentEffectInstance;

    private void OnEnable()
    {
        m_ElapsedTime = 0f;
        m_CurrentEffectIndex = 0;
        PlayNextEffect();
    }

    private void OnDisable()
    {
        // 销毁当前特效
        if (m_CurrentEffectInstance != null)
        {
            Destroy(m_CurrentEffectInstance);
            m_CurrentEffectInstance = null;
        }
    }

    private void Update()
    {
        if (listOfEffects == null || listOfEffects.Count == 0)
            return;

        m_ElapsedTime += Time.deltaTime;

        // 检查是否需要切换到下一个特效
        if (m_ElapsedTime >= loopTimeLength)
        {
            m_ElapsedTime = 0f;
            PlayNextEffect();
        }
    }

    /// <summary>播放下一个特效</summary>
    private void PlayNextEffect()
    {
        // 销毁上一个特效
        if (m_CurrentEffectInstance != null)
        {
            Destroy(m_CurrentEffectInstance);
            m_CurrentEffectInstance = null;
        }

        if (listOfEffects.Count == 0)
            return;

        // 获取下一个特效预制体
        var effectPrefab = listOfEffects[m_CurrentEffectIndex];
        if (effectPrefab != null)
        {
            m_CurrentEffectInstance = Instantiate(
                effectPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );
            DebugEx.Log("ChestEffectCycler", $"播放特效 [{m_CurrentEffectIndex}]");
        }

        // 循环到下一个特效
        m_CurrentEffectIndex = (m_CurrentEffectIndex + 1) % listOfEffects.Count;
    }
}
