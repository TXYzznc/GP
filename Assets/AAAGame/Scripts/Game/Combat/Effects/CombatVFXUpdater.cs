using UnityEngine;

/// <summary>
/// 战斗特效更新器
/// 用于驱动 CombatVFXManager 的每帧更新
/// </summary>
public class CombatVFXUpdater : SingletonBase<CombatVFXUpdater>
{
    #region 公共方法

    public static void EnsureExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("[CombatVFXUpdater]");
            go.AddComponent<CombatVFXUpdater>();
            DontDestroyOnLoad(go);
        }
    }

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        base.Awake();
    }

    private void LateUpdate()
    {
        CombatVFXManager.LateUpdate();
    }

    private void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion
}
