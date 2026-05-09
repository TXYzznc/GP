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
/// EquipmentTable
/// </summary>
public partial class EquipmentTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 装备表ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 物品名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 对应ItemTable中的ID
        /// </summary>
        public int ItemTableId
        {
            get;
            private set;
        }

        /// <summary>
        /// 特殊效果ID
        /// </summary>
        public int SpecialEffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础属性(JSON格式)
        /// </summary>
        public string BaseAttributes
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
            Name = columnStrings[index++];
            ItemTableId = int.Parse(columnStrings[index++]);
            SpecialEffectId = int.Parse(columnStrings[index++]);
            BaseAttributes = columnStrings[index++];

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
                    ItemTableId = binaryReader.Read7BitEncodedInt32();
                    SpecialEffectId = binaryReader.Read7BitEncodedInt32();
                    BaseAttributes = binaryReader.ReadString();
                }
            }

            return true;
        }
}
