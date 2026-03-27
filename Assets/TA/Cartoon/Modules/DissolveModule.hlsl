#ifndef DISSOLVE_MODULE_INCLUDED
#define DISSOLVE_MODULE_INCLUDED

// ========================================
// 溶解效果模块 - 支持多颜色渐变边缘发光
// ========================================

TEXTURE2D(_DissolveTex);
SAMPLER(sampler_DissolveTex);

// 溶解效果数据结构
struct DissolveData
{
    float alpha;
    float3 emissionColor;
    float emissionStrength;
};

// ========================================
// 颜色混合辅助函数
// ========================================

// 三色渐变：外边缘 -> 中间 -> 内边缘
float3 ThreeColorGradient(float t, float3 outerColor, float3 midColor, float3 innerColor)
{
    // t: 0 = 外边缘, 1 = 内边缘
    if (t < 0.5)
    {
        return lerp(outerColor, midColor, t * 2.0);
    }
    else
    {
        return lerp(midColor, innerColor, (t - 0.5) * 2.0);
    }
}

// 四色渐变
float3 FourColorGradient(float t, float3 color1, float3 color2, float3 color3, float3 color4)
{
    if (t < 0.333)
    {
        return lerp(color1, color2, t * 3.0);
    }
    else if (t < 0.666)
    {
        return lerp(color2, color3, (t - 0.333) * 3.0);
    }
    else
    {
        return lerp(color3, color4, (t - 0.666) * 3.0);
    }
}

// ========================================
// 主要函数：计算溶解效果（单色版本 - 保持兼容）
// ========================================
DissolveData CalculateDissolve(
    float2 uv,
    float threshold,
    float4 edgeColor,
    float edgeWidth,
    float4 dissolveTex_ST)
{
    DissolveData result;
    
    float2 dissolveUV = uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
    float dissolveValue = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
    
    float dissolveDiff = dissolveValue - threshold;
    result.alpha = dissolveDiff;
    
    float edgeFactor = 1.0 - saturate(dissolveDiff / edgeWidth);
    
    if (dissolveDiff > 0 && dissolveDiff < edgeWidth)
    {
        result.emissionColor = edgeColor.rgb;
        result.emissionStrength = edgeFactor * edgeColor.a;
    }
    else
    {
        result.emissionColor = float3(0, 0, 0);
        result.emissionStrength = 0;
    }
    
    return result;
}

// ========================================
// 双色渐变溶解
// ========================================
DissolveData CalculateDissolve_TwoColor(
    float2 uv,
    float threshold,
    float4 outerColor,    // 外边缘颜色（靠近溶解边界）
    float4 innerColor,    // 内边缘颜色（靠近物体内部）
    float edgeWidth,
    float4 dissolveTex_ST)
{
    DissolveData result;
    
    float2 dissolveUV = uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
    float dissolveValue = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
    
    float dissolveDiff = dissolveValue - threshold;
    result.alpha = dissolveDiff;
    
    if (dissolveDiff > 0 && dissolveDiff < edgeWidth)
    {
        // t: 0 = 外边缘, 1 = 内边缘
        float t = saturate(dissolveDiff / edgeWidth);
        
        // 颜色渐变
        result.emissionColor = lerp(outerColor.rgb, innerColor.rgb, t);
        
        // 强度渐变（外边缘最亮）
        float intensityOuter = outerColor.a;
        float intensityInner = innerColor.a;
        result.emissionStrength = lerp(intensityOuter, intensityInner, t);
    }
    else
    {
        result.emissionColor = float3(0, 0, 0);
        result.emissionStrength = 0;
    }
    
    return result;
}

// ========================================
// 三色渐变溶解（火焰效果常用）
// ========================================
DissolveData CalculateDissolve_ThreeColor(
    float2 uv,
    float threshold,
    float4 outerColor,    // 外边缘（如：白色/黄色）
    float4 midColor,      // 中间（如：橙色）
    float4 innerColor,    // 内边缘（如：红色/暗红）
    float edgeWidth,
    float4 dissolveTex_ST)
{
    DissolveData result;
    
    float2 dissolveUV = uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
    float dissolveValue = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
    
    float dissolveDiff = dissolveValue - threshold;
    result.alpha = dissolveDiff;
    
    if (dissolveDiff > 0 && dissolveDiff < edgeWidth)
    {
        float t = saturate(dissolveDiff / edgeWidth);
        
        // 三色渐变
        result.emissionColor = ThreeColorGradient(t, outerColor.rgb, midColor.rgb, innerColor.rgb);
        
        // 强度渐变
        float intensity;
        if (t < 0.5)
        {
            intensity = lerp(outerColor.a, midColor.a, t * 2.0);
        }
        else
        {
            intensity = lerp(midColor.a, innerColor.a, (t - 0.5) * 2.0);
        }
        result.emissionStrength = intensity;
    }
    else
    {
        result.emissionColor = float3(0, 0, 0);
        result.emissionStrength = 0;
    }
    
    return result;
}

// ========================================
// 彩虹渐变溶解
// ========================================
DissolveData CalculateDissolve_Rainbow(
    float2 uv,
    float threshold,
    float edgeWidth,
    float intensity,
    float4 dissolveTex_ST)
{
    DissolveData result;
    
    float2 dissolveUV = uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
    float dissolveValue = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
    
    float dissolveDiff = dissolveValue - threshold;
    result.alpha = dissolveDiff;
    
    if (dissolveDiff > 0 && dissolveDiff < edgeWidth)
    {
        float t = saturate(dissolveDiff / edgeWidth);
        
        // HSV to RGB（色相从0到1遍历整个彩虹）
        float hue = t;
        float3 rgb;
        float h = hue * 6.0;
        float c = 1.0;
        float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
        
        if (h < 1.0)      rgb = float3(c, x, 0);
        else if (h < 2.0) rgb = float3(x, c, 0);
        else if (h < 3.0) rgb = float3(0, c, x);
        else if (h < 4.0) rgb = float3(0, x, c);
        else if (h < 5.0) rgb = float3(x, 0, c);
        else              rgb = float3(c, 0, x);
        
        result.emissionColor = rgb;
        result.emissionStrength = intensity * (1.0 - t * 0.5); // 外边缘更亮
    }
    else
    {
        result.emissionColor = float3(0, 0, 0);
        result.emissionStrength = 0;
    }
    
    return result;
}

// ========================================
// 脉冲闪烁效果
// ========================================
DissolveData CalculateDissolve_Pulse(
    float2 uv,
    float threshold,
    float4 color1,
    float4 color2,
    float edgeWidth,
    float pulseSpeed,
    float time,
    float4 dissolveTex_ST)
{
    DissolveData result;
    
    float2 dissolveUV = uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
    float dissolveValue = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
    
    float dissolveDiff = dissolveValue - threshold;
    result.alpha = dissolveDiff;
    
    if (dissolveDiff > 0 && dissolveDiff < edgeWidth)
    {
        float t = saturate(dissolveDiff / edgeWidth);
        
        // 脉冲混合因子
        float pulse = sin(time * pulseSpeed) * 0.5 + 0.5;
        
        // 颜色在两种之间脉冲
        result.emissionColor = lerp(color1.rgb, color2.rgb, pulse);
        result.emissionStrength = lerp(color1.a, color2.a, pulse) * (1.0 - t);
    }
    else
    {
        result.emissionColor = float3(0, 0, 0);
        result.emissionStrength = 0;
    }
    
    return result;
}

// ========================================
// 简化版本：仅返回是否应该被裁剪
// ========================================
float GetDissolveAlpha(float2 uv, float threshold, float4 dissolveTex_ST)
{
    float2 dissolveUV = uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
    float dissolveValue = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
    return dissolveValue - threshold;
}

// ========================================
// 应用溶解到最终颜色
// ========================================
float3 ApplyDissolveEmission(float3 originalColor, DissolveData dissolveData)
{
    return originalColor + dissolveData.emissionColor * dissolveData.emissionStrength;
}

#endif // DISSOLVE_MODULE_INCLUDED
