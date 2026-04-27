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
/// ChessEXPRuleTable
/// </summary>
public partial class ChessEXPRuleTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 配置ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 规则类型（1=使用道具 2=击败敌方棋子）
        /// </summary>
        public int RuleType
        {
            get;
            private set;
        }

        /// <summary>
        /// 目标阶级（RuleType=2时有效，0=无效）
        /// </summary>
        public int EnemyRank
        {
            get;
            private set;
        }

        /// <summary>
        /// 经验奖励（RuleType=1时为0由道具自身决定）
        /// </summary>
        public int EXPReward
        {
            get;
            private set;
        }

        /// <summary>
        /// 规则描述
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
            RuleType = int.Parse(columnStrings[index++]);
            EnemyRank = int.Parse(columnStrings[index++]);
            EXPReward = int.Parse(columnStrings[index++]);
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
                    RuleType = binaryReader.Read7BitEncodedInt32();
                    EnemyRank = binaryReader.Read7BitEncodedInt32();
                    EXPReward = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                }
            }

            return true;
        }
}
