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
/// CombatDifficultyRule
/// </summary>
public partial class CombatDifficultyRule : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 难度等级(1-10)
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 敌人难度系数（用于调整敌人棋子的五大基础属性）
        /// </summary>
        public float EnemyDifficultyCoef
        {
            get;
            private set;
        }

        /// <summary>
        /// 最小敌人数量
        /// </summary>
        public int MinPopulation
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大敌人数量
        /// </summary>
        public int MaxPopulation
        {
            get;
            private set;
        }

        /// <summary>
        /// 奖励倍率(对战时，每0.3会额外增加一个奖励，保底1个)
        /// </summary>
        public float RewardMultiplier
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
            EnemyDifficultyCoef = float.Parse(columnStrings[index++]);
            MinPopulation = int.Parse(columnStrings[index++]);
            MaxPopulation = int.Parse(columnStrings[index++]);
            RewardMultiplier = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    EnemyDifficultyCoef = binaryReader.ReadSingle();
                    MinPopulation = binaryReader.Read7BitEncodedInt32();
                    MaxPopulation = binaryReader.Read7BitEncodedInt32();
                    RewardMultiplier = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
