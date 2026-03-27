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
/// SummonChessTable
/// </summary>
public partial class SummonChessTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 棋子的唯一配置ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 棋子名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 品质/等级（1-4，对应蓝、紫、金、炫彩）
        /// </summary>
        public int Quality
        {
            get;
            private set;
        }

        /// <summary>
        /// 占用人口数
        /// </summary>
        public int PopCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 种族ID数组（用于触发羁绊，如：1,2）
        /// </summary>
        public int[] Races
        {
            get;
            private set;
        }

        /// <summary>
        /// 职业ID数组（用于触发羁绊，如：3）
        /// </summary>
        public int[] Classes
        {
            get;
            private set;
        }

        /// <summary>
        /// 星级（1, 2, 3）
        /// </summary>
        public int StarLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// 进化到下一星级的配置ID（3星填0）
        /// </summary>
        public int NextStarId
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子预制体资源ID
        /// </summary>
        public int PrefabId
        {
            get;
            private set;
        }

        /// <summary>
        /// UI图标资源ID
        /// </summary>
        public int IconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public double MaxHp
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大法力值
        /// </summary>
        public double MaxMp
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始法力值
        /// </summary>
        public double InitialMp
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击力（物理伤害）
        /// </summary>
        public double AtkDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击速度（每秒攻击次数）
        /// </summary>
        public double AtkSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击距离（格子数或米）
        /// </summary>
        public double AtkRange
        {
            get;
            private set;
        }

        /// <summary>
        /// 护甲（物理减伤）
        /// </summary>
        public double Armor
        {
            get;
            private set;
        }

        /// <summary>
        /// 魔抗（魔法减伤）
        /// </summary>
        public double MagicResist
        {
            get;
            private set;
        }

        /// <summary>
        /// 移动速度
        /// </summary>
        public double MoveSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 暴击率
        /// </summary>
        public double CritRate
        {
            get;
            private set;
        }

        /// <summary>
        /// 暴击伤害倍率
        /// </summary>
        public double CritDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 法术强度
        /// </summary>
        public double SpellPower
        {
            get;
            private set;
        }

        /// <summary>
        /// 护盾值
        /// </summary>
        public double Shield
        {
            get;
            private set;
        }

        /// <summary>
        /// 冷却缩减（百分比）
        /// </summary>
        public double CooldownReduce
        {
            get;
            private set;
        }

        /// <summary>
        /// 被动技能ID数组
        /// </summary>
        public int[] PassiveIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 普攻效果技能ID
        /// </summary>
        public int NormalAtkId
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能一ID
        /// </summary>
        public int Skill1Id
        {
            get;
            private set;
        }

        /// <summary>
        /// 大招ID
        /// </summary>
        public int Skill2Id
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子AI模式:1=近战寻敌2=远程站桩3=稻草人
        /// </summary>
        public int AIType
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子的背景描述
        /// </summary>
        public string Description
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
            Quality = int.Parse(columnStrings[index++]);
            PopCost = int.Parse(columnStrings[index++]);
            Races = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            Classes = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            StarLevel = int.Parse(columnStrings[index++]);
            NextStarId = int.Parse(columnStrings[index++]);
            PrefabId = int.Parse(columnStrings[index++]);
            IconId = int.Parse(columnStrings[index++]);
            MaxHp = double.Parse(columnStrings[index++]);
            MaxMp = double.Parse(columnStrings[index++]);
            InitialMp = double.Parse(columnStrings[index++]);
            AtkDamage = double.Parse(columnStrings[index++]);
            AtkSpeed = double.Parse(columnStrings[index++]);
            AtkRange = double.Parse(columnStrings[index++]);
            Armor = double.Parse(columnStrings[index++]);
            MagicResist = double.Parse(columnStrings[index++]);
            MoveSpeed = double.Parse(columnStrings[index++]);
            CritRate = double.Parse(columnStrings[index++]);
            CritDamage = double.Parse(columnStrings[index++]);
            SpellPower = double.Parse(columnStrings[index++]);
            Shield = double.Parse(columnStrings[index++]);
            CooldownReduce = double.Parse(columnStrings[index++]);
            PassiveIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            NormalAtkId = int.Parse(columnStrings[index++]);
            Skill1Id = int.Parse(columnStrings[index++]);
            Skill2Id = int.Parse(columnStrings[index++]);
            AIType = int.Parse(columnStrings[index++]);
            Description = columnStrings[index++];

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
                    Quality = binaryReader.Read7BitEncodedInt32();
                    PopCost = binaryReader.Read7BitEncodedInt32();
                    Races = binaryReader.ReadArray<int>();
                    Classes = binaryReader.ReadArray<int>();
                    StarLevel = binaryReader.Read7BitEncodedInt32();
                    NextStarId = binaryReader.Read7BitEncodedInt32();
                    PrefabId = binaryReader.Read7BitEncodedInt32();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    MaxHp = binaryReader.ReadDouble();
                    MaxMp = binaryReader.ReadDouble();
                    InitialMp = binaryReader.ReadDouble();
                    AtkDamage = binaryReader.ReadDouble();
                    AtkSpeed = binaryReader.ReadDouble();
                    AtkRange = binaryReader.ReadDouble();
                    Armor = binaryReader.ReadDouble();
                    MagicResist = binaryReader.ReadDouble();
                    MoveSpeed = binaryReader.ReadDouble();
                    CritRate = binaryReader.ReadDouble();
                    CritDamage = binaryReader.ReadDouble();
                    SpellPower = binaryReader.ReadDouble();
                    Shield = binaryReader.ReadDouble();
                    CooldownReduce = binaryReader.ReadDouble();
                    PassiveIds = binaryReader.ReadArray<int>();
                    NormalAtkId = binaryReader.Read7BitEncodedInt32();
                    Skill1Id = binaryReader.Read7BitEncodedInt32();
                    Skill2Id = binaryReader.Read7BitEncodedInt32();
                    AIType = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                }
            }

            return true;
        }
}
