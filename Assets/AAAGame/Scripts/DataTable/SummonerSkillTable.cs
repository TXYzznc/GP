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
    /// 技能ID
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
        /// 技能类型(1=被动，2=主动)
        /// </summary>
        public int SkillType
        {
            get;
            private set;
        }

        /// <summary>
        /// 冷却时间
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
        /// 效果类型（1=Buff,2=Debuff,3=伤害,4=治疗）
        /// </summary>
        public int EffectType
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果值
        /// </summary>
        public float EffectValue
        {
            get;
            private set;
        }

        /// <summary>
        /// 持续时间（0表示条件触发，无持续时间）
        /// </summary>
        public float Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 作用范围
        /// </summary>
        public float Range
        {
            get;
            private set;
        }

        /// <summary>
        /// 范围类型（0=作用于自己，1=单体，2=直线，3=群体）
        /// </summary>
        public int RangeType
        {
            get;
            private set;
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 图标路径
        /// </summary>
        public int SpritePath
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
            Cooldown = float.Parse(columnStrings[index++]);
            SpiritCost = float.Parse(columnStrings[index++]);
            EffectType = int.Parse(columnStrings[index++]);
            EffectValue = float.Parse(columnStrings[index++]);
            Duration = float.Parse(columnStrings[index++]);
            Range = float.Parse(columnStrings[index++]);
            RangeType = int.Parse(columnStrings[index++]);
            Description = columnStrings[index++];
            SpritePath = int.Parse(columnStrings[index++]);

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
                    Cooldown = binaryReader.ReadSingle();
                    SpiritCost = binaryReader.ReadSingle();
                    EffectType = binaryReader.Read7BitEncodedInt32();
                    EffectValue = binaryReader.ReadSingle();
                    Duration = binaryReader.ReadSingle();
                    Range = binaryReader.ReadSingle();
                    RangeType = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                    SpritePath = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
