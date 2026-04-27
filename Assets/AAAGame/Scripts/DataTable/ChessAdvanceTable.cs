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
/// ChessAdvanceTable
/// </summary>
public partial class ChessAdvanceTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 棋子ID（等于进阶前棋子ID，直接按棋子ID查询）
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 当前阶级（1=一阶）
        /// </summary>
        public int Rank
        {
            get;
            private set;
        }

        /// <summary>
        /// 进阶后棋子ID（0=已是最高阶）
        /// </summary>
        public int AdvancedChessId
        {
            get;
            private set;
        }

        /// <summary>
        /// 进阶所需经验值（0=无法进阶）
        /// </summary>
        public int RequiredEXP
        {
            get;
            private set;
        }

        /// <summary>
        /// 进阶条件事件ID（0=无附加条件，后续接事件系统）
        /// </summary>
        public int ConditionEventId
        {
            get;
            private set;
        }

        /// <summary>
        /// 进阶特效资源ID（0=无特效）
        /// </summary>
        public int AdvanceEffectId
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
            Rank = int.Parse(columnStrings[index++]);
            AdvancedChessId = int.Parse(columnStrings[index++]);
            RequiredEXP = int.Parse(columnStrings[index++]);
            ConditionEventId = int.Parse(columnStrings[index++]);
            AdvanceEffectId = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Rank = binaryReader.Read7BitEncodedInt32();
                    AdvancedChessId = binaryReader.Read7BitEncodedInt32();
                    RequiredEXP = binaryReader.Read7BitEncodedInt32();
                    ConditionEventId = binaryReader.Read7BitEncodedInt32();
                    AdvanceEffectId = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
