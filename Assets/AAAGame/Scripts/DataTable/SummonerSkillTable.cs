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
/// SummonerSkillTable
/// </summary>
public partial class SummonerSkillTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 技能唯一ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 技能名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 所属召唤师职业 1=狂战 2=术士 3=混沌 4=德鲁伊
        /// </summary>
        public int SummonerClass
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能类型 1=被动 2=主动
        /// </summary>
        public int SkillType
        {
            get;
            private set;
        }

        /// <summary>
        /// 解锁阶段 1=初始 3=第三阶 4=第四阶 5=第五阶
        /// </summary>
        public int UnlockTier
        {
            get;
            private set;
        }

        /// <summary>
        /// 分支ID 0=固定技能 1=路线一 2=路线二
        /// </summary>
        public int BranchId
        {
            get;
            private set;
        }

        /// <summary>
        /// 冷却时间（秒）
        /// </summary>
        public float Cooldown
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
        /// 持续时间（秒）
        /// </summary>
        public float Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 施法/生效范围
        /// </summary>
        public float CastRange
        {
            get;
            private set;
        }

        /// <summary>
        /// AOE半径（0=单体）
        /// </summary>
        public float AreaRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害类型 0=无 1=物理 2=魔法 3=真实
        /// </summary>
        public int DamageType
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害系数（基于攻击力/法强 如1.5=150%）
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
        /// 命中类型 0=瞬发 1=近战 2=投射物 3=AoE 4=射线
        /// </summary>
        public int EffectHitType
        {
            get;
            private set;
        }

        /// <summary>
        /// 投射物预制体ID（0=无）
        /// </summary>
        public int ProjectilePrefabId
        {
            get;
            private set;
        }

        /// <summary>
        /// 投射物速度（0=即时命中）
        /// </summary>
        public float ProjectileSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 命中/触发次数
        /// </summary>
        public int HitCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 释放/条件触发时施加的Buff列表，格式"buffId:targetType,..."。targetType:1=召唤师自身 2=全体友方(不含召唤师) 3=全体友方(含召唤师) 4=全体敌方 5=命中目标(单体)
        /// </summary>
        public string InstantBuffs
        {
            get;
            private set;
        }

        /// <summary>
        /// 命中目标时施加的Buff列表，格式同InstantBuffs
        /// </summary>
        public string HitBuffs
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能专属参数数组（含义由各技能代码定义 见Desc备注）
        /// </summary>
        public float[] Params
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
        /// 技能图标资源ID
        /// </summary>
        public int IconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能描述
        /// </summary>
        public string Desc
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
            SummonerClass = int.Parse(columnStrings[index++]);
            SkillType = int.Parse(columnStrings[index++]);
            UnlockTier = int.Parse(columnStrings[index++]);
            BranchId = int.Parse(columnStrings[index++]);
            Cooldown = float.Parse(columnStrings[index++]);
            SpiritCost = float.Parse(columnStrings[index++]);
            Duration = float.Parse(columnStrings[index++]);
            CastRange = float.Parse(columnStrings[index++]);
            AreaRadius = float.Parse(columnStrings[index++]);
            DamageType = int.Parse(columnStrings[index++]);
            DamageCoeff = float.Parse(columnStrings[index++]);
            BaseDamage = float.Parse(columnStrings[index++]);
            EffectHitType = int.Parse(columnStrings[index++]);
            ProjectilePrefabId = int.Parse(columnStrings[index++]);
            ProjectileSpeed = float.Parse(columnStrings[index++]);
            HitCount = int.Parse(columnStrings[index++]);
            InstantBuffs = columnStrings[index++];
            HitBuffs = columnStrings[index++];
            Params = DataTableExtension.ParseArray<float>(columnStrings[index++]);
            EffectId = int.Parse(columnStrings[index++]);
            HitEffectId = int.Parse(columnStrings[index++]);
            EffectSpawnHeight = float.Parse(columnStrings[index++]);
            IconId = int.Parse(columnStrings[index++]);
            Desc = columnStrings[index++];

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
                    SummonerClass = binaryReader.Read7BitEncodedInt32();
                    SkillType = binaryReader.Read7BitEncodedInt32();
                    UnlockTier = binaryReader.Read7BitEncodedInt32();
                    BranchId = binaryReader.Read7BitEncodedInt32();
                    Cooldown = binaryReader.ReadSingle();
                    SpiritCost = binaryReader.ReadSingle();
                    Duration = binaryReader.ReadSingle();
                    CastRange = binaryReader.ReadSingle();
                    AreaRadius = binaryReader.ReadSingle();
                    DamageType = binaryReader.Read7BitEncodedInt32();
                    DamageCoeff = binaryReader.ReadSingle();
                    BaseDamage = binaryReader.ReadSingle();
                    EffectHitType = binaryReader.Read7BitEncodedInt32();
                    ProjectilePrefabId = binaryReader.Read7BitEncodedInt32();
                    ProjectileSpeed = binaryReader.ReadSingle();
                    HitCount = binaryReader.Read7BitEncodedInt32();
                    InstantBuffs = binaryReader.ReadString();
                    HitBuffs = binaryReader.ReadString();
                    Params = binaryReader.ReadArray<float>();
                    EffectId = binaryReader.Read7BitEncodedInt32();
                    HitEffectId = binaryReader.Read7BitEncodedInt32();
                    EffectSpawnHeight = binaryReader.ReadSingle();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    Desc = binaryReader.ReadString();
                }
            }

            return true;
        }
}
