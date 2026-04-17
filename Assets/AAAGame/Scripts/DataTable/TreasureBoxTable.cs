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
/// TreasureBoxTable
/// </summary>
public partial class TreasureBoxTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 宝箱ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 宝箱名字
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 稀有度(0普通 1稀有 2史诗 3传说)
        /// </summary>
        public int Rarity
        {
            get;
            private set;
        }

        /// <summary>
        /// 开箱最小物品数
        /// </summary>
        public int ItemCountMin
        {
            get;
            private set;
        }

        /// <summary>
        /// 开箱最大物品数
        /// </summary>
        public int ItemCountMax
        {
            get;
            private set;
        }

        /// <summary>
        /// 物品组ID
        /// </summary>
        public int ItemGroupId
        {
            get;
            private set;
        }

        /// <summary>
        /// 宝箱描述
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
            Name = columnStrings[index++];
            Rarity = int.Parse(columnStrings[index++]);
            ItemCountMin = int.Parse(columnStrings[index++]);
            ItemCountMax = int.Parse(columnStrings[index++]);
            ItemGroupId = int.Parse(columnStrings[index++]);
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
                    Name = binaryReader.ReadString();
                    Rarity = binaryReader.Read7BitEncodedInt32();
                    ItemCountMin = binaryReader.Read7BitEncodedInt32();
                    ItemCountMax = binaryReader.Read7BitEncodedInt32();
                    ItemGroupId = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                }
            }

            return true;
        }
}
