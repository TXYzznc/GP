using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 伤害飘字动画库
/// 包含所有飘字动画效果的实现
/// </summary>
public class DamageFloatingTextAnimationLibrary
{
    #region 动画类型枚举
    
    /// <summary>
    /// 动画类型枚举
    /// 对应配置表中的AnimationType字段
    /// </summary>
    public enum AnimationType
    {
        基础上升 = 1,
        弹跳缩放 = 2,
        震动效果 = 3,
        螺旋上升 = 4,
        淡入淡出 = 5,
        火焰摇摆 = 6,
        冰霜闪烁 = 7,
        雷电跳跃 = 8,
        治疗光环 = 9,
        护盾展开 = 10,
        经验飞散 = 11,
        金币闪耀 = 12,
        技能爆发 = 13,
        连击叠加 = 14,
        反弹回旋 = 15,
        吸血脉冲 = 16,
        法力流动 = 17,
        恢复波纹 = 18,
        免疫消散 = 19
    }
    
    #endregion

    #region 公共方法

    /// <summary>
    /// 播放指定类型的动画
    /// </summary>
    /// <param name="popupItem">飘字对象</param>
    /// <param name="animationType">动画类型ID</param>
    /// <param name="onComplete">完成回调</param>
    public void PlayAnimation(DamagePopupItem popupItem, int animationType, Action onComplete = null)
    {
        DebugEx.Log("DamageFloatingTextAnimationLibrary", $"开始播放动画类型: {animationType}");
        
        switch ((AnimationType)animationType)
        {
            case AnimationType.基础上升:
                PlayBasicRiseAnimation(popupItem, onComplete);
                break;
            case AnimationType.弹跳缩放:
                PlayBounceScaleAnimation(popupItem, onComplete);
                break;
            case AnimationType.震动效果:
                PlayShakeAnimation(popupItem, onComplete);
                break;
            case AnimationType.螺旋上升:
                PlaySpiralRiseAnimation(popupItem, onComplete);
                break;
            case AnimationType.淡入淡出:
                PlayFadeAnimation(popupItem, onComplete);
                break;
            case AnimationType.火焰摇摆:
                PlayFlameSwayAnimation(popupItem, onComplete);
                break;
            case AnimationType.冰霜闪烁:
                PlayIceFlickerAnimation(popupItem, onComplete);
                break;
            case AnimationType.雷电跳跃:
                PlayLightningJumpAnimation(popupItem, onComplete);
                break;
            case AnimationType.治疗光环:
                PlayHealingAuraAnimation(popupItem, onComplete);
                break;
            case AnimationType.护盾展开:
                PlayShieldExpandAnimation(popupItem, onComplete);
                break;
            case AnimationType.经验飞散:
                PlayExpScatterAnimation(popupItem, onComplete);
                break;
            case AnimationType.金币闪耀:
                PlayCoinShineAnimation(popupItem, onComplete);
                break;
            case AnimationType.技能爆发:
                PlaySkillBurstAnimation(popupItem, onComplete);
                break;
            case AnimationType.连击叠加:
                PlayComboStackAnimation(popupItem, onComplete);
                break;
            case AnimationType.反弹回旋:
                PlayBounceSpinAnimation(popupItem, onComplete);
                break;
            case AnimationType.吸血脉冲:
                PlayLifestealPulseAnimation(popupItem, onComplete);
                break;
            case AnimationType.法力流动:
                PlayManaFlowAnimation(popupItem, onComplete);
                break;
            case AnimationType.恢复波纹:
                PlayRestoreRippleAnimation(popupItem, onComplete);
                break;
            case AnimationType.免疫消散:
                PlayImmunityFadeAnimation(popupItem, onComplete);
                break;
            default:
                DebugEx.Warning("DamageFloatingTextAnimationLibrary", $"未知动画类型: {animationType}，使用默认动画");
                PlayBasicRiseAnimation(popupItem, onComplete);
                break;
        }
    }

    #endregion

    #region 动画实现

    /// <summary>
    /// 基础上升动画
    /// </summary>
    private void PlayBasicRiseAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlayBasicRiseAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlayBasicRiseAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        // 使用原有的Play方法
        popupItem.Play(onComplete);
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 弹跳缩放动画（暴击伤害）
    /// </summary>
    private void PlayBounceScaleAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlayBounceScaleAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlayBounceScaleAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        Vector3 startScale = popupItem.transform.localScale;
        float duration = 2.0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 弹跳效果
            float bounceScale = 1f + 0.5f * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
            popupItem.transform.localScale = startScale * bounceScale;

            // 上升运动
            float riseHeight = 100f * Mathf.Sin(t * Mathf.PI * 0.5f);
            popupItem.transform.position = startPos + Vector3.up * riseHeight;

            // 透明度变化
            if (t > 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                popupItem.SetInitialAlpha(1f - fadeT);
            }

            await UniTask.Yield();
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 震动效果动画（真实伤害）
    /// </summary>
    private void PlayShakeAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlayShakeAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlayShakeAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float duration = 2.2f;
        float elapsedTime = 0f;
        float shakeIntensity = 0.1f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 震动效果
            Vector3 shakeOffset = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                0f
            ) * (1f - t); // 震动强度随时间减弱

            // 基础上升
            float riseHeight = 70f * t;
            popupItem.transform.position = startPos + Vector3.up * riseHeight + shakeOffset;

            // 透明度变化
            if (t > 0.7f)
            {
                float fadeT = (t - 0.7f) / 0.3f;
                popupItem.SetInitialAlpha(1f - fadeT);
            }

            await UniTask.Yield();
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 螺旋上升动画（护盾吸收）
    /// </summary>
    private void PlaySpiralRiseAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlaySpiralRiseAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlaySpiralRiseAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float duration = 1.2f;
        float elapsedTime = 0f;
        float spiralRadius = 0.5f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 螺旋运动
            float angle = t * Mathf.PI * 4f; // 2圈
            Vector3 spiralOffset = new Vector3(
                Mathf.Cos(angle) * spiralRadius * (1f - t),
                0f,
                Mathf.Sin(angle) * spiralRadius * (1f - t)
            );

            // 上升运动
            float riseHeight = 40f * t;
            popupItem.transform.position = startPos + Vector3.up * riseHeight + spiralOffset;

            // 透明度变化
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                popupItem.SetInitialAlpha(1f - fadeT);
            }

            await UniTask.Yield();
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 淡入淡出动画（毒伤害）
    /// </summary>
    private void PlayFadeAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlayFadeAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlayFadeAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float duration = 2.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 缓慢上升
            float riseHeight = 55f * t;
            popupItem.transform.position = startPos + Vector3.up * riseHeight;

            // 脉冲透明度效果
            float pulseAlpha = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 6f);
            if (t > 0.8f)
            {
                float fadeT = (t - 0.8f) / 0.2f;
                pulseAlpha *= (1f - fadeT);
            }
            popupItem.SetInitialAlpha(pulseAlpha);

            await UniTask.Yield();
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 火焰摇摆动画（火焰伤害）
    /// </summary>
    private void PlayFlameSwayAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlayFlameSwayAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlayFlameSwayAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float duration = 2.0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 摇摆效果
            float swayX = Mathf.Sin(t * Mathf.PI * 4f) * 0.3f * (1f - t);
            
            // 上升运动
            float riseHeight = 65f * t;
            popupItem.transform.position = startPos + new Vector3(swayX, riseHeight, 0f);

            // 透明度变化
            if (t > 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                popupItem.SetInitialAlpha(1f - fadeT);
            }

            await UniTask.Yield();
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 冰霜闪烁动画（冰霜伤害）
    /// </summary>
    private void PlayIceFlickerAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlayIceFlickerAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlayIceFlickerAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        Vector3 startScale = popupItem.transform.localScale;
        float duration = 2.2f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 闪烁效果
            float flicker = UnityEngine.Random.Range(0.8f, 1.2f);
            popupItem.transform.localScale = startScale * flicker;

            // 上升运动
            float riseHeight = 60f * t;
            popupItem.transform.position = startPos + Vector3.up * riseHeight;

            // 透明度变化
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                popupItem.SetInitialAlpha(1f - fadeT);
            }

            await UniTask.Yield();
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 雷电跳跃动画（雷电伤害）
    /// </summary>
    private void PlayLightningJumpAnimation(DamagePopupItem popupItem, Action onComplete)
    {
        PlayLightningJumpAnimationAsync(popupItem, onComplete).Forget();
    }

    private async UniTaskVoid PlayLightningJumpAnimationAsync(DamagePopupItem popupItem, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float duration = 1.8f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 跳跃效果（抛物线）
            float jumpHeight = 4f * t * (1f - t) * 75f; // 抛物线公式
            
            // 随机偏移模拟雷电效果
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-0.2f, 0.2f),
                0f,
                UnityEngine.Random.Range(-0.2f, 0.2f)
            );

            popupItem.transform.position = startPos + Vector3.up * jumpHeight + randomOffset;

            // 透明度变化
            if (t > 0.4f)
            {
                float fadeT = (t - 0.4f) / 0.6f;
                popupItem.SetInitialAlpha(1f - fadeT);
            }

            await UniTask.Yield();
        }

        onComplete?.Invoke();
    }

    // 其他动画方法的简化实现，使用基础动画作为占位符
    private void PlayHealingAuraAnimation(DamagePopupItem popupItem, Action onComplete) => PlayBasicRiseAnimation(popupItem, onComplete);
    private void PlayShieldExpandAnimation(DamagePopupItem popupItem, Action onComplete) => PlaySpiralRiseAnimation(popupItem, onComplete);
    private void PlayExpScatterAnimation(DamagePopupItem popupItem, Action onComplete) => PlayBasicRiseAnimation(popupItem, onComplete);
    private void PlayCoinShineAnimation(DamagePopupItem popupItem, Action onComplete) => PlayBounceScaleAnimation(popupItem, onComplete);
    private void PlaySkillBurstAnimation(DamagePopupItem popupItem, Action onComplete) => PlayBounceScaleAnimation(popupItem, onComplete);
    private void PlayComboStackAnimation(DamagePopupItem popupItem, Action onComplete) => PlayBounceScaleAnimation(popupItem, onComplete);
    private void PlayBounceSpinAnimation(DamagePopupItem popupItem, Action onComplete) => PlaySpiralRiseAnimation(popupItem, onComplete);
    private void PlayLifestealPulseAnimation(DamagePopupItem popupItem, Action onComplete) => PlayFadeAnimation(popupItem, onComplete);
    private void PlayManaFlowAnimation(DamagePopupItem popupItem, Action onComplete) => PlayFlameSwayAnimation(popupItem, onComplete);
    private void PlayRestoreRippleAnimation(DamagePopupItem popupItem, Action onComplete) => PlayBasicRiseAnimation(popupItem, onComplete);
    private void PlayImmunityFadeAnimation(DamagePopupItem popupItem, Action onComplete) => PlayFadeAnimation(popupItem, onComplete);

    #endregion
}