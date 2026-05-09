using System;
using UnityEngine;

/// <summary>
/// 物品基础配置数据（对应 ItemTable）
/// </summary>
[Serializable]
public class ItemData
{
    public int Id;
    public string Name;
    public ItemType Type;
    public ItemRarity Rarity;
    public bool IsOnlyInGame;
    public string Description;
    public int IconId;
    public bool CanStack;
    public int MaxStackCount;
    public int SellPrice;
    public int Value;
    public int Weight;

    public Color GetRarityColor()
    {
        return RarityColorHelper.GetColor((int)Rarity);
    }
}
