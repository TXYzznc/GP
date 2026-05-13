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
        /// 品质（1-4，对应蓝、紫、金、炫彩）
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
        /// 职业ID数组（用于触发羁绊，如：101）
        /// </summary>
        public int[] Classes
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子预制体资源ID（阶级数组，暂时三阶相同）
        /// </summary>
        public int[] PrefabId
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子图标资源ID（阶级数组，暂时三阶相同）
        /// </summary>
        public int[] IconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子头像资源ID（阶级数组，暂时三阶相同）
        /// </summary>
        public int[] HeadImgId
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大生命值（三个阶级）**基础属性**
        /// </summary>
        public double[] MaxHp
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大法力值（三个阶级）**基础属性**
        /// </summary>
        public double[] MaxMp
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始法力值（三个阶级）
        /// </summary>
        public double[] InitialMp
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击力（三个阶级）**基础属性**
        /// </summary>
        public double[] AtkDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击速度（三个阶级）
        /// </summary>
        public double[] AtkSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击距离（三个阶级）
        /// </summary>
        public double[] AtkRange
        {
            get;
            private set;
        }

        /// <summary>
        /// 护甲（三个阶级）**基础属性**
        /// </summary>
        public double[] Armor
        {
            get;
            private set;
        }

        /// <summary>
        /// 魔抗（三个阶级）**基础属性**
        /// </summary>
        public double[] MagicResist
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
        /// 暴击率（三个阶级）
        /// </summary>
        public double[] CritRate
        {
            get;
            private set;
        }

        /// <summary>
        /// 暴击伤害倍率（三个阶级）
        /// </summary>
        public double[] CritDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 法术强度（三个阶级）**基础属性**
        /// </summary>
        public double[] SpellPower
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
        /// 冷却缩减
        /// </summary>
        public double CooldownReduce
        {
            get;
            private set;
        }

        /// <summary>
        /// 被动技能ID数组（三个值，暂时相同）
        /// </summary>
        public int[] PassiveIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 普攻效果技能ID（三个值，暂时相同）
        /// </summary>
        public int[] NormalAtkId
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能一ID（三个值，暂时相同）
        /// </summary>
        public int[] Skill1Id
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能二ID（三个值，暂时相同）（没有则不填）
        /// </summary>
        public int[] Skill2Id
        {
            get;
            private set;
        }

        /// <summary>
        /// 大招ID（三个值，暂时相同）
        /// </summary>
        public int[] UltimateId
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
        /// 自定义数据（JSON格式）
        /// </summary>
        public string CustomData
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子背景故事（多段，100-200字每段）
        /// </summary>
        public string[] StoryText
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
            PrefabId = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            IconId = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            HeadImgId = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            MaxHp = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            MaxMp = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            InitialMp = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            AtkDamage = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            AtkSpeed = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            AtkRange = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            Armor = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            MagicResist = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            MoveSpeed = double.Parse(columnStrings[index++]);
            CritRate = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            CritDamage = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            SpellPower = DataTableExtension.ParseArray<double>(columnStrings[index++]);
            Shield = double.Parse(columnStrings[index++]);
            CooldownReduce = double.Parse(columnStrings[index++]);
            PassiveIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            NormalAtkId = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            Skill1Id = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            Skill2Id = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            UltimateId = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            AIType = int.Parse(columnStrings[index++]);
            CustomData = columnStrings[index++];
            StoryText = DataTableExtension.ParseArray<string>(columnStrings[index++]);

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
                    PrefabId = binaryReader.ReadArray<int>();
                    IconId = binaryReader.ReadArray<int>();
                    HeadImgId = binaryReader.ReadArray<int>();
                    MaxHp = binaryReader.ReadArray<double>();
                    MaxMp = binaryReader.ReadArray<double>();
                    InitialMp = binaryReader.ReadArray<double>();
                    AtkDamage = binaryReader.ReadArray<double>();
                    AtkSpeed = binaryReader.ReadArray<double>();
                    AtkRange = binaryReader.ReadArray<double>();
                    Armor = binaryReader.ReadArray<double>();
                    MagicResist = binaryReader.ReadArray<double>();
                    MoveSpeed = binaryReader.ReadDouble();
                    CritRate = binaryReader.ReadArray<double>();
                    CritDamage = binaryReader.ReadArray<double>();
                    SpellPower = binaryReader.ReadArray<double>();
                    Shield = binaryReader.ReadDouble();
                    CooldownReduce = binaryReader.ReadDouble();
                    PassiveIds = binaryReader.ReadArray<int>();
                    NormalAtkId = binaryReader.ReadArray<int>();
                    Skill1Id = binaryReader.ReadArray<int>();
                    Skill2Id = binaryReader.ReadArray<int>();
                    UltimateId = binaryReader.ReadArray<int>();
                    AIType = binaryReader.Read7BitEncodedInt32();
                    CustomData = binaryReader.ReadString();
                    StoryText = binaryReader.ReadArray<string>();
                }
            }

            return true;
        }
}
