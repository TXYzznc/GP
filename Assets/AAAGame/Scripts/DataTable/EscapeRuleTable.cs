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
/// EscapeRuleTable
/// </summary>
public partial class EscapeRuleTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 规则ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 敌人类型：0=普通, 1=精英, 2=Boss
        /// </summary>
        public int EnemyType
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础成功率(0-1)
        /// </summary>
        public double BaseSuccessRate
        {
            get;
            private set;
        }

        /// <summary>
        /// 每回合成功率增长(0-1)
        /// </summary>
        public double TimeBonus
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大成功率上限(0-1)
        /// </summary>
        public double MaxSuccessRate
        {
            get;
            private set;
        }

        /// <summary>
        /// 脱战成功消耗的污染值
        /// </summary>
        public int CorruptionCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 脱战失败时的生命值损失比例(0-1)
        /// </summary>
        public double HealthLossPenalty
        {
            get;
            private set;
        }

        /// <summary>
        /// 脱战失败后的冷却回合数
        /// </summary>
        public int CooldownTurns
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
            EnemyType = int.Parse(columnStrings[index++]);
            BaseSuccessRate = double.Parse(columnStrings[index++]);
            TimeBonus = double.Parse(columnStrings[index++]);
            MaxSuccessRate = double.Parse(columnStrings[index++]);
            CorruptionCost = int.Parse(columnStrings[index++]);
            HealthLossPenalty = double.Parse(columnStrings[index++]);
            CooldownTurns = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    EnemyType = binaryReader.Read7BitEncodedInt32();
                    BaseSuccessRate = binaryReader.ReadDouble();
                    TimeBonus = binaryReader.ReadDouble();
                    MaxSuccessRate = binaryReader.ReadDouble();
                    CorruptionCost = binaryReader.Read7BitEncodedInt32();
                    HealthLossPenalty = binaryReader.ReadDouble();
                    CooldownTurns = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
