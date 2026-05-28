using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品稀有度发光效果工具类
/// 统一处理 GlowEffect 子对象的 Shader 参数设置和脉冲动画
/// </summary>
public static class RarityGlowHelper
{
    private const float GlowPulseFrequency = 0.4f;
    private const float GlowRadius = 2.0f;
    private const float EdgeSoftness = 0.35f;

    /// <summary>
    /// 在指定 Transform 的 "GlowEffect" 子对象上应用稀有度发光效果，
    /// 并返回创建的脉冲 Tween（调用方负责在销毁时 Kill）。
    /// </summary>
    /// <param name="root">物品卡片根节点</param>
    /// <param name="rarity">稀有度（1-5）</param>
    /// <param name="existingGlowImage">已缓存的 Image 引用，首次传 null 会自动查找并写回</param>
    /// <returns>脉冲动画 Tween，未找到 GlowEffect 时返回 null</returns>
    public static Tween Apply(Transform root, int rarity, ref Image existingGlowImage)
    {
        if (existingGlowImage == null)
        {
            var glowTransform = root.Find("GlowEffect");
            if (glowTransform == null)
            {
                DebugEx.Warning(root.name, "未找到 GlowEffect 子对象");
                return null;
            }
            existingGlowImage = glowTransform.GetComponent<Image>();
            if (existingGlowImage == null)
            {
                DebugEx.Warning(root.name, "GlowEffect 上没有 Image 组件");
                return null;
            }
        }

        var shader = Shader.Find("UI/RarityGlow");
        if (shader == null)
        {
            DebugEx.Warning(root.name, "未找到 UI/RarityGlow Shader");
            return null;
        }

        var mat = new Material(shader);
        existingGlowImage.material = mat;

        Color color = rarity switch
        {
            1 => new Color(0.8f, 0.8f, 0.8f, 1f), // 普通：白色
            2 => new Color(0.2f, 0.8f, 0.2f, 1f), // 优良：绿色
            3 => new Color(0.2f, 0.6f, 1f,   1f), // 稀有：蓝色
            4 => new Color(0.8f, 0.2f, 1f,   1f), // 史诗：紫色
            5 => new Color(1f,   0.8f, 0.2f, 1f), // 传说：金色
            _  => Color.white,
        };
        const float baseIntensity = 1.5f;

        mat.SetColor("_GlowColor", color);
        mat.SetFloat("_GlowIntensity", baseIntensity);
        mat.SetFloat("_GlowRadius", GlowRadius);
        mat.SetFloat("_EdgeSoftness", EdgeSoftness);

        float duration = 1f / (GlowPulseFrequency * 2f);
        return DOTween
            .To(
                () => mat.GetFloat("_GlowIntensity"),
                v  => mat.SetFloat("_GlowIntensity", v),
                baseIntensity * 1.4f,
                duration
            )
            .From(baseIntensity * 0.6f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}
