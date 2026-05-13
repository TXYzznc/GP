using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝物装备管理器 - 在战斗准备时应用装备的宝物效果
/// </summary>
public class TreasureEquipmentManager : MonoBehaviour
{
    private static TreasureEquipmentManager s_Instance;
    public static TreasureEquipmentManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                var obj = new GameObject("TreasureEquipmentManager");
                s_Instance = obj.AddComponent<TreasureEquipmentManager>();
            }
            return s_Instance;
        }
    }

    /// <summary>
    /// 应用玩家所有装备的宝物效果到棋子
    /// </summary>
    public void ApplyPlayerEquippedTreasures(ChessAttribute chessAttribute, int chessId)
    {
        if (chessAttribute == null)
        {
            DebugEx.Error(nameof(TreasureEquipmentManager), "棋子属性为空");
            return;
        }

        var saveData = PlayerAccountDataManager.Instance?.CurrentSaveData;
        if (saveData == null)
        {
            DebugEx.Warning(nameof(TreasureEquipmentManager), "玩家存档为空");
            return;
        }

        var equippedTreasures = PlayerAccountDataManager.Instance.GetChessEquipments(chessId);
        if (equippedTreasures == null || equippedTreasures.Count == 0)
        {
            DebugEx.Log(nameof(TreasureEquipmentManager), $"棋子 {chessId} 无装备宝物");
            return;
        }

        DebugEx.Log(nameof(TreasureEquipmentManager),
            $"为棋子 {chessId} 应用 {equippedTreasures.Count} 件装备宝物");

        foreach (var treasureData in equippedTreasures)
        {
            ApplySingleTreasure(chessAttribute, treasureData);
        }

        DebugEx.Success(nameof(TreasureEquipmentManager),
            $"✅ 棋子 {chessId} 装备宝物效果应用完成");
    }

    /// <summary>
    /// 应用单件宝物的所有效果（基础属性 + 词条）
    /// </summary>
    private void ApplySingleTreasure(ChessAttribute chessAttribute, TreasureInstanceData treasureData)
    {
        if (treasureData?.Affixes == null)
            return;

        // 应用词条效果
        foreach (var affixData in treasureData.Affixes)
        {
            ApplySingleAffix(chessAttribute, affixData);
        }

        DebugEx.Log(nameof(TreasureEquipmentManager),
            $"宝物 {treasureData.TreasureId}(InstanceId={treasureData.InstanceId}) 词条应用完成，词条数={treasureData.Affixes.Count}");
    }

    /// <summary>
    /// 应用单条词条到棋子属性
    /// </summary>
    private void ApplySingleAffix(ChessAttribute chessAttribute, AffixData affixData)
    {
        if (affixData == null)
            return;

        double delta = affixData.ValueMax * 1.0; // 使用最大值（已滚出）

        // 百分比类型转换为小数（如 20% → 0.2）
        if (affixData.ValueType == ValueType.Percent)
            delta /= 100.0;

        switch (affixData.AttributeType)
        {
            case AttributeType.MaxHP:
                chessAttribute.ModifyHp(delta);
                break;
            case AttributeType.Attack:
                chessAttribute.ModifyAtkDamage(delta);
                break;
            case AttributeType.MaxMP:
                chessAttribute.ModifyMp(delta);
                break;
            case AttributeType.AttackSpeed:
                chessAttribute.ModifyAtkSpeed(delta);
                break;
            case AttributeType.CritRate:
                chessAttribute.ModifyCritRate(delta);
                break;
            case AttributeType.CritDamage:
                chessAttribute.ModifyCritDamage(delta);
                break;
            case AttributeType.Defense:
                chessAttribute.ModifyArmor(delta);
                break;
            case AttributeType.MagicResist:
                chessAttribute.ModifyMagicResist(delta);
                break;
            case AttributeType.SpellPower:
                chessAttribute.ModifySpellPower(delta);
                break;
            case AttributeType.MoveSpeed:
                chessAttribute.ModifyMoveSpeed(delta);
                break;
            case AttributeType.CooldownReduce:
                chessAttribute.ModifyCooldownReduce(delta);
                break;
        }
    }
}
