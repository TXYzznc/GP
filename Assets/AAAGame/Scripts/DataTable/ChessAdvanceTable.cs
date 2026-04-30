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
    /// 棋子ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 升阶所需经验值数组（1升2, 2升3）
        /// </summary>
        public int[] RequiredEXP
        {
            get;
            private set;
        }

        /// <summary>
        /// 升阶条件事件ID数组（1升2, 2升3）
        /// </summary>
        public int[] ConditionEventId
        {
            get;
            private set;
        }

        /// <summary>
        /// 升阶特效资源ID
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
            RequiredEXP = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            ConditionEventId = DataTableExtension.ParseArray<int>(columnStrings[index++]);
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
                    RequiredEXP = binaryReader.ReadArray<int>();
                    ConditionEventId = binaryReader.ReadArray<int>();
                    AdvanceEffectId = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
