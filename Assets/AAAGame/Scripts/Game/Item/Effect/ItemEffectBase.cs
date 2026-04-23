/// <summary>
/// 物品效果基类（可选）
/// </summary>
public abstract class ItemEffectBase : IItemEffect
{
    public abstract bool Execute(ItemEffectContext context);

    protected void LogSuccess(string moduleName, string message)
    {
        DebugEx.Success(moduleName, message);
    }

    protected void LogWarning(string moduleName, string message)
    {
        DebugEx.Warning(moduleName, message);
    }

    protected void LogError(string moduleName, string message)
    {
        DebugEx.Error(moduleName, message);
    }
}
