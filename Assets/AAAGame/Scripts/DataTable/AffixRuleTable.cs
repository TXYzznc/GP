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
/// AffixRuleTable
/// </summary>
public partial class AffixRuleTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 品质ID(对应ItemRarity)
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 品质名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 词条数量下限
        /// </summary>
        public int AffixCountMin
        {
            get;
            private set;
        }

        /// <summary>
        /// 词条数量上限
        /// </summary>
        public int AffixCountMax
        {
            get;
            private set;
        }

        /// <summary>
        /// 数值缩放下限(0~1)
        /// </summary>
        public float ValueScaleMin
        {
            get;
            private set;
        }

        /// <summary>
        /// 数值缩放上限(0~1)
        /// </summary>
        public float ValueScaleMax
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
            AffixCountMin = int.Parse(columnStrings[index++]);
            AffixCountMax = int.Parse(columnStrings[index++]);
            ValueScaleMin = float.Parse(columnStrings[index++]);
            ValueScaleMax = float.Parse(columnStrings[index++]);

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
                    AffixCountMin = binaryReader.Read7BitEncodedInt32();
                    AffixCountMax = binaryReader.Read7BitEncodedInt32();
                    ValueScaleMin = binaryReader.ReadSingle();
                    ValueScaleMax = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
