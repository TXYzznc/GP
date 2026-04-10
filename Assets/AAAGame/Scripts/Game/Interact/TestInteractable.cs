using UnityEngine;

/// <summary>
/// 测试用可交互对象
/// 验证交互系统是否正常工作，正式上线后可删除
/// </summary>
public class TestInteractable : InteractableBase
{
    [Header("测试配置")]
    [SerializeField] private string testMessage = "测试交互成功！";
    [SerializeField] private bool singleUse = false;

    private bool m_Used = false;

    public override bool CanInteract(GameObject player)
    {
        return !singleUse || !m_Used;
    }

    public override void OnInteract(GameObject player)
    {
        m_Used = true;
        DebugEx.LogModule("TestInteractable", testMessage);
    }
}
