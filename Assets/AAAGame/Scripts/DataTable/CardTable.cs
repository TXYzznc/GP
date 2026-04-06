//------------------------------------------------------------
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
// 生成时间：__DATA_TABLE_CREATE_TIME__
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
#endif
/// <summary>
/// CardTable
/// </summary>
public partial class CardTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 卡牌ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 卡牌名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 卡牌描述
        /// </summary>
        public string Desc
        {
            get;
            private set;
        }

        /// <summary>
        /// 卡牌图标资源ID
        /// </summary>
        public int IconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 预制体配置JSON
        /// </summary>
        public string PrefabConfig
        {
            get;
            private set;
        }

        /// <summary>
        /// 灵力消耗
        /// </summary>
        public float SpiritCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 目标类型(1=自身 2=友方单体 3=友方全体 4=敌方单体 5=敌方全体 6=全场)
        /// </summary>
        public int TargetType
        {
            get;
            private set;
        }

        /// <summary>
        /// 施法范围(0=无限制)
        /// </summary>
        public float CastRange
        {
            get;
            private set;
        }

        /// <summary>
        /// AOE半径(0=单体)
        /// </summary>
        public float AreaRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害类型(0=无 1=物理 2=魔法 3=真实)
        /// </summary>
        public int DamageType
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害系数
        /// </summary>
        public float DamageCoeff
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础固定伤害
        /// </summary>
        public float BaseDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 释放时施加的Buff列表
        /// </summary>
        public string InstantBuffs
        {
            get;
            private set;
        }

        /// <summary>
        /// 命中目标时施加的Buff列表
        /// </summary>
        public string HitBuffs
        {
            get;
            private set;
        }

        /// <summary>
        /// 策略卡参数配置JSON
        /// </summary>
        public string ParamsConfig
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能特效资源ID
        /// </summary>
        public int EffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 受击特效资源ID
        /// </summary>
        public int HitEffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 特效生成高度偏移
        /// </summary>
        public float EffectSpawnHeight
        {
            get;
            private set;
        }

        /// <summary>
        /// 稀有度(1=普通 2=稀有 3=史诗 4=传说)
        /// </summary>
        public int Rarity
        {
            get;
            private set;
        }

        /// <summary>
        /// 解锁条件
        /// </summary>
        public string UnlockCondition
        {
            get;
            private set;
        }

        /// <summary>
        /// 故事文本
        /// </summary>
        public string StoryText
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            Name = columnStrings[index++];
            Desc = columnStrings[index++];
            IconId = int.Parse(columnStrings[index++]);
            PrefabConfig = columnStrings[index++];
            SpiritCost = float.Parse(columnStrings[index++]);
            TargetType = int.Parse(columnStrings[index++]);
            CastRange = float.Parse(columnStrings[index++]);
            AreaRadius = float.Parse(columnStrings[index++]);
            DamageType = int.Parse(columnStrings[index++]);
            DamageCoeff = float.Parse(columnStrings[index++]);
            BaseDamage = float.Parse(columnStrings[index++]);
            InstantBuffs = columnStrings[index++];
            HitBuffs = columnStrings[index++];
            ParamsConfig = columnStrings[index++];
            EffectId = int.Parse(columnStrings[index++]);
            HitEffectId = int.Parse(columnStrings[index++]);
            EffectSpawnHeight = float.Parse(columnStrings[index++]);
            Rarity = int.Parse(columnStrings[index++]);
            UnlockCondition = columnStrings[index++];
            StoryText = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Name = binaryReader.ReadString();
                    Desc = binaryReader.ReadString();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    PrefabConfig = binaryReader.ReadString();
                    SpiritCost = binaryReader.ReadSingle();
                    TargetType = binaryReader.Read7BitEncodedInt32();
                    CastRange = binaryReader.ReadSingle();
                    AreaRadius = binaryReader.ReadSingle();
                    DamageType = binaryReader.Read7BitEncodedInt32();
                    DamageCoeff = binaryReader.ReadSingle();
                    BaseDamage = binaryReader.ReadSingle();
                    InstantBuffs = binaryReader.ReadString();
                    HitBuffs = binaryReader.ReadString();
                    ParamsConfig = binaryReader.ReadString();
                    EffectId = binaryReader.Read7BitEncodedInt32();
                    HitEffectId = binaryReader.Read7BitEncodedInt32();
                    EffectSpawnHeight = binaryReader.ReadSingle();
                    Rarity = binaryReader.Read7BitEncodedInt32();
                    UnlockCondition = binaryReader.ReadString();
                    StoryText = binaryReader.ReadString();
                }
            }

            return true;
        }
}
