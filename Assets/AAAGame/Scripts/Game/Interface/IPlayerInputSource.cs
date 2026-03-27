using System.Collections.Generic;
using UnityEngine;
public interface IPlayerInputSource
{
    Vector2 GetMove();          // x=×óÓÒ, y=Ç°ºó
    bool GetSkillDown(int slot); // slot=1/2/3...
    bool GetSkillHeld(int slot);
}