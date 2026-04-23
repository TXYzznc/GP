/// <summary>
/// SpecialEffectTable 扩展类 - 提供效果参数解析
/// </summary>
public partial class SpecialEffectTable
{
    /// <summary>
    /// 效果参数（JSON格式）
    /// 由 DataTableGenerator 自动填充，主要用于消耗品效果配置
    /// 格式示例：{"type":"UnlockCard","cardId":1001}
    /// </summary>
    public string EffectParams
    {
        get;
        set;
    }
}
