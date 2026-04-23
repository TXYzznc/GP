/// <summary>
/// 物品效果接口
/// </summary>
public interface IItemEffect
{
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="context">效果上下文，包含所需的所有数据</param>
    /// <returns>执行是否成功</returns>
    bool Execute(ItemEffectContext context);
}
