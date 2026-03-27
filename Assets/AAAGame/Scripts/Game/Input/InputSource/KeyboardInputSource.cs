using System.Collections.Generic;
using UnityEngine;

public class KeyboardInputSource : IPlayerInputSource  // 键盘输入映射
{
    public Vector2 GetMove()
        => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

    public bool GetSkillDown(int slot)
    {
        return slot switch
        {
            1 => Input.GetKeyDown(KeyCode.Alpha1),  // 槽位1 按 1键
            2 => Input.GetKeyDown(KeyCode.Alpha2),  // 槽位2 按 2键
            3 => Input.GetKeyDown(KeyCode.Alpha3),  // 槽位3 按 3键
            _ => false
        };
        // 需要同步更新PlayerSkillSlot.GetKeyNameBySlot中的映射关系
    }

    public bool GetSkillHeld(int slot) => false;
}
