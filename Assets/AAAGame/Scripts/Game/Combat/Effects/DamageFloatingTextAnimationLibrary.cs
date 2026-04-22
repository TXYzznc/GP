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
    /// <param name="moveDistance">上升高度（来自配置表）</param>
    /// <param name="duration">动画时长（来自配置表）</param>
    /// <param name="onComplete">完成回调</param>
    public void PlayAnimation(DamagePopupItem popupItem, int animationType, float moveDistance, float duration, Action onComplete = null)
    {
        DebugEx.Log("DamageFloatingTextAnimationLibrary", $"开始播放动画类型: {animationType}, 高度: {moveDistance}, 时长: {duration}");

        switch ((AnimationType)animationType)
        {
            case AnimationType.基础上升:
                PlayBasicRiseAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.弹跳缩放:
                PlayBounceScaleAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.震动效果:
                PlayShakeAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.螺旋上升:
                PlaySpiralRiseAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.淡入淡出:
                PlayFadeAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.火焰摇摆:
                PlayFlameSwayAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.冰霜闪烁:
                PlayIceFlickerAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.雷电跳跃:
                PlayLightningJumpAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.治疗光环:
                PlayHealingAuraAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.护盾展开:
                PlayShieldExpandAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.经验飞散:
                PlayExpScatterAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.金币闪耀:
                PlayCoinShineAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.技能爆发:
                PlaySkillBurstAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.连击叠加:
                PlayComboStackAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.反弹回旋:
                PlayBounceSpinAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.吸血脉冲:
                PlayLifestealPulseAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.法力流动:
                PlayManaFlowAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.恢复波纹:
                PlayRestoreRippleAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            case AnimationType.免疫消散:
                PlayImmunityFadeAnimation(popupItem, moveDistance, duration, onComplete);
                break;
            default:
                DebugEx.Warning("DamageFloatingTextAnimationLibrary", $"未知动画类型: {animationType}，使用默认动画");
                PlayBasicRiseAnimation(popupItem, moveDistance, duration, onComplete);
                break;
        }
    }

    #endregion

    #region 动画实现

    /// <summary>
    /// 基础上升动画
    /// </summary>
    private void PlayBasicRiseAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        popupItem.SetAnimationParams(duration, moveDistance);
        popupItem.Play(onComplete);
    }

    /// <summary>
    /// 弹跳缩放动画（暴击伤害）
    /// </summary>
    private void PlayBounceScaleAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        PlayBounceScaleAnimationAsync(popupItem, moveDistance, duration, onComplete).Forget();
    }

    private async UniTaskVoid PlayBounceScaleAnimationAsync(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        Vector3 startScale = popupItem.transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 弹跳效果
            float bounceScale = 1f + 0.5f * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
            popupItem.transform.localScale = startScale * bounceScale;

            // 上升运动
            float riseHeight = moveDistance * Mathf.Sin(t * Mathf.PI * 0.5f);
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
    private void PlayShakeAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        PlayShakeAnimationAsync(popupItem, moveDistance, duration, onComplete).Forget();
    }

    private async UniTaskVoid PlayShakeAnimationAsync(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
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
            float riseHeight = moveDistance * t;
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
    private void PlaySpiralRiseAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        PlaySpiralRiseAnimationAsync(popupItem, moveDistance, duration, onComplete).Forget();
    }

    private async UniTaskVoid PlaySpiralRiseAnimationAsync(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
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
            float riseHeight = moveDistance * t;
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
    private void PlayFadeAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        PlayFadeAnimationAsync(popupItem, moveDistance, duration, onComplete).Forget();
    }

    private async UniTaskVoid PlayFadeAnimationAsync(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 缓慢上升
            float riseHeight = moveDistance * t;
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
    private void PlayFlameSwayAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        PlayFlameSwayAnimationAsync(popupItem, moveDistance, duration, onComplete).Forget();
    }

    private async UniTaskVoid PlayFlameSwayAnimationAsync(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 摇摆效果
            float swayX = Mathf.Sin(t * Mathf.PI * 4f) * 0.3f * (1f - t);

            // 上升运动
            float riseHeight = moveDistance * t;
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
    private void PlayIceFlickerAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        PlayIceFlickerAnimationAsync(popupItem, moveDistance, duration, onComplete).Forget();
    }

    private async UniTaskVoid PlayIceFlickerAnimationAsync(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        Vector3 startScale = popupItem.transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 闪烁效果
            float flicker = UnityEngine.Random.Range(0.8f, 1.2f);
            popupItem.transform.localScale = startScale * flicker;

            // 上升运动
            float riseHeight = moveDistance * t;
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
    private void PlayLightningJumpAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        PlayLightningJumpAnimationAsync(popupItem, moveDistance, duration, onComplete).Forget();
    }

    private async UniTaskVoid PlayLightningJumpAnimationAsync(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete)
    {
        Vector3 startPos = popupItem.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 跳跃效果（抛物线）
            float jumpHeight = 4f * t * (1f - t) * moveDistance; // 抛物线公式

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
    private void PlayHealingAuraAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayBasicRiseAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayShieldExpandAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlaySpiralRiseAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayExpScatterAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayBasicRiseAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayCoinShineAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayBounceScaleAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlaySkillBurstAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayBounceScaleAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayComboStackAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayBounceScaleAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayBounceSpinAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlaySpiralRiseAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayLifestealPulseAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayFadeAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayManaFlowAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayFlameSwayAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayRestoreRippleAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayBasicRiseAnimation(popupItem, moveDistance, duration, onComplete);
    private void PlayImmunityFadeAnimation(DamagePopupItem popupItem, float moveDistance, float duration, Action onComplete) => PlayFadeAnimation(popupItem, moveDistance, duration, onComplete);

    #endregion
}