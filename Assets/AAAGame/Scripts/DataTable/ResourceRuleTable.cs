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
/// ResourceRuleTable
/// </summary>
public partial class ResourceRuleTable : DataRowBase
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
        /// 初始背包格数
        /// </summary>
        public int InitInventorySlots
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大扩展格数
        /// </summary>
        public int MaxExtendSlots
        {
            get;
            private set;
        }

        /// <summary>
        /// 扩展基础费用
        /// </summary>
        public int ExtendBaseCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 每次升级增加格数
        /// </summary>
        public int ExtendSlotsPerUpgrade
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始仓库格数
        /// </summary>
        public int InitWarehouseSlots
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
            InitInventorySlots = int.Parse(columnStrings[index++]);
            MaxExtendSlots = int.Parse(columnStrings[index++]);
            ExtendBaseCost = int.Parse(columnStrings[index++]);
            ExtendSlotsPerUpgrade = int.Parse(columnStrings[index++]);
            InitWarehouseSlots = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    InitInventorySlots = binaryReader.Read7BitEncodedInt32();
                    MaxExtendSlots = binaryReader.Read7BitEncodedInt32();
                    ExtendBaseCost = binaryReader.Read7BitEncodedInt32();
                    ExtendSlotsPerUpgrade = binaryReader.Read7BitEncodedInt32();
                    InitWarehouseSlots = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
