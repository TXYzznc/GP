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
/// AffixTable
/// </summary>
public partial class AffixTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 词条ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 词条名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 词条描述
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 词条类型
        /// </summary>
        public int AffixType
        {
            get;
            private set;
        }

        /// <summary>
        /// 属性类型
        /// </summary>
        public int AttributeType
        {
            get;
            private set;
        }

        /// <summary>
        /// 数值类型
        /// </summary>
        public int ValueType
        {
            get;
            private set;
        }

        /// <summary>
        /// 最小值
        /// </summary>
        public float ValueMin
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大值
        /// </summary>
        public float ValueMax
        {
            get;
            private set;
        }

        /// <summary>
        /// 权重
        /// </summary>
        public int Weight
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
            Description = columnStrings[index++];
            AffixType = int.Parse(columnStrings[index++]);
            AttributeType = int.Parse(columnStrings[index++]);
            ValueType = int.Parse(columnStrings[index++]);
            ValueMin = float.Parse(columnStrings[index++]);
            ValueMax = float.Parse(columnStrings[index++]);
            Weight = int.Parse(columnStrings[index++]);

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
                    Description = binaryReader.ReadString();
                    AffixType = binaryReader.Read7BitEncodedInt32();
                    AttributeType = binaryReader.Read7BitEncodedInt32();
                    ValueType = binaryReader.Read7BitEncodedInt32();
                    ValueMin = binaryReader.ReadSingle();
                    ValueMax = binaryReader.ReadSingle();
                    Weight = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
