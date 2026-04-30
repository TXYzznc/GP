/// <summary>
/// 物品效果基类（可选）
/// </summary>
public abstract class ItemEffectBase : IItemEffect
{
    public abstract bool Execute(ItemEffectContext context);

    protected void LogSuccess(string scriptName, string message)
    {
        DebugEx.Success(scriptName, message);
    }

    protected void LogWarning(string scriptName, string message)
    {
        DebugEx.Warning(scriptName, message);
    }

    protected void LogError(string scriptName, string message)
    {
        DebugEx.Error(scriptName, message);
    }
}
