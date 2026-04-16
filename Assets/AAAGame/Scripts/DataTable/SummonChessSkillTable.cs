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
/// SummonChessSkillTable
/// </summary>
public partial class SummonChessSkillTable : DataRowBase
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
        /// 技能类型：1=被动 2=普攻效果 3=主动技能 4=大招
        /// </summary>
        public int SkillType
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害类型：0=无伤害 1=物理 2=魔法 3=真实
        /// </summary>
        public int DamageType
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害系数:（基于攻击力/法强的百分比）如1.5=150%）
        /// </summary>
        public double DamageCoeff
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果命中类型：0=瞬发1=近2=投射物 3=AO4=射线,5=特殊
        /// </summary>
        public int EffectHitType
        {
            get;
            private set;
        }

        /// <summary>
        /// 投射物预制体ID（AttackHitType=2时使用）
        /// </summary>
        public int ProjectilePrefabId
        {
            get;
            private set;
        }

        /// <summary>
        /// Buff触发类型：0=执行时（动画执行帧）应用1=命中时应用2=完成某种条件进行应用
        /// </summary>
        public int BuffTriggerType
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础固定伤害值
        /// </summary>
        public double BaseDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 法力消耗
        /// </summary>
        public double MpCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 法力回复
        /// </summary>
        public double MpRestore
        {
            get;
            private set;
        }

        /// <summary>
        /// 冷却时间（秒）
        /// </summary>
        public double Cooldown
        {
            get;
            private set;
        }

        /// <summary>
        /// 施法/生效范围
        /// </summary>
        public double CastRange
        {
            get;
            private set;
        }

        /// <summary>
        /// AOE半径（0=单体）
        /// </summary>
        public double AreaRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 持续时间（引导类技能）
        /// </summary>
        public double Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 触发次数（多段伤害）表示一次技能触发效果的次数
        /// </summary>
        public int HitCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 穿透数量（投射物穿透）表示投射物可以穿透并命中的敌人数量
        /// </summary>
        public int PenetrationCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 释放时附加的BuffID数组
        /// </summary>
        public int[] BuffIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 释放时给自身附加的BuffID数组
        /// </summary>
        public int[] SelfBuffIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 投射物速度（0=即时命中）
        /// </summary>
        public double ProjectileSpeed
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
        /// 技能特效资源ID
        /// </summary>
        public int EffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 特效播放的相对位置
        /// </summary>
        public float EffectSpawnHeight
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
        /// 自定义数据（JSON格式）
        /// </summary>
        public string CustomData
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
            SkillType = int.Parse(columnStrings[index++]);
            DamageType = int.Parse(columnStrings[index++]);
            DamageCoeff = double.Parse(columnStrings[index++]);
            EffectHitType = int.Parse(columnStrings[index++]);
            ProjectilePrefabId = int.Parse(columnStrings[index++]);
            BuffTriggerType = int.Parse(columnStrings[index++]);
            BaseDamage = double.Parse(columnStrings[index++]);
            MpCost = double.Parse(columnStrings[index++]);
            MpRestore = double.Parse(columnStrings[index++]);
            Cooldown = double.Parse(columnStrings[index++]);
            CastRange = double.Parse(columnStrings[index++]);
            AreaRadius = double.Parse(columnStrings[index++]);
            Duration = double.Parse(columnStrings[index++]);
            HitCount = int.Parse(columnStrings[index++]);
            PenetrationCount = int.Parse(columnStrings[index++]);
            BuffIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            SelfBuffIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            ProjectileSpeed = double.Parse(columnStrings[index++]);
            IconId = int.Parse(columnStrings[index++]);
            EffectId = int.Parse(columnStrings[index++]);
            EffectSpawnHeight = float.Parse(columnStrings[index++]);
            HitEffectId = int.Parse(columnStrings[index++]);
            CustomData = columnStrings[index++];
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
                    SkillType = binaryReader.Read7BitEncodedInt32();
                    DamageType = binaryReader.Read7BitEncodedInt32();
                    DamageCoeff = binaryReader.ReadDouble();
                    EffectHitType = binaryReader.Read7BitEncodedInt32();
                    ProjectilePrefabId = binaryReader.Read7BitEncodedInt32();
                    BuffTriggerType = binaryReader.Read7BitEncodedInt32();
                    BaseDamage = binaryReader.ReadDouble();
                    MpCost = binaryReader.ReadDouble();
                    MpRestore = binaryReader.ReadDouble();
                    Cooldown = binaryReader.ReadDouble();
                    CastRange = binaryReader.ReadDouble();
                    AreaRadius = binaryReader.ReadDouble();
                    Duration = binaryReader.ReadDouble();
                    HitCount = binaryReader.Read7BitEncodedInt32();
                    PenetrationCount = binaryReader.Read7BitEncodedInt32();
                    BuffIds = binaryReader.ReadArray<int>();
                    SelfBuffIds = binaryReader.ReadArray<int>();
                    ProjectileSpeed = binaryReader.ReadDouble();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    EffectId = binaryReader.Read7BitEncodedInt32();
                    EffectSpawnHeight = binaryReader.ReadSingle();
                    HitEffectId = binaryReader.Read7BitEncodedInt32();
                    CustomData = binaryReader.ReadString();
                    Desc = binaryReader.ReadString();
                }
            }

            return true;
        }
}
