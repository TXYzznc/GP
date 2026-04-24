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
/// ExpRuleTable
/// </summary>
public partial class ExpRuleTable : DataRowBase
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
        /// 来源类型(1=物品稀有度,2=敌人,3=任务)
        /// </summary>
        public int SourceType
        {
            get;
            private set;
        }

        /// <summary>
        /// 来源参数(稀有度值/敌人难度/任务类型)
        /// </summary>
        public int SourceParam
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础经验
        /// </summary>
        public int BaseExp
        {
            get;
            private set;
        }

        /// <summary>
        /// 每级额外经验系数
        /// </summary>
        public float ExpPerLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// 描述
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
            SourceType = int.Parse(columnStrings[index++]);
            SourceParam = int.Parse(columnStrings[index++]);
            BaseExp = int.Parse(columnStrings[index++]);
            ExpPerLevel = float.Parse(columnStrings[index++]);
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
                    SourceType = binaryReader.Read7BitEncodedInt32();
                    SourceParam = binaryReader.Read7BitEncodedInt32();
                    BaseExp = binaryReader.Read7BitEncodedInt32();
                    ExpPerLevel = binaryReader.ReadSingle();
                    Desc = binaryReader.ReadString();
                }
            }

            return true;
        }
}
