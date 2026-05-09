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
/// ItemTable
/// </summary>
public partial class ItemTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 物品ID
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
        /// 物品类型
        /// </summary>
        public int Type
        {
            get;
            private set;
        }

        /// <summary>
        /// 品质等级
        /// </summary>
        public int Rarity
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否只在局内使用
        /// </summary>
        public bool IsOnlyInGame
        {
            get;
            private set;
        }

        /// <summary>
        /// 物品描述
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 图标资源ID
        /// </summary>
        public int IconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否可堆叠
        /// </summary>
        public bool CanStack
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大堆叠数
        /// </summary>
        public int MaxStackCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 售价
        /// </summary>
        public int SellPrice
        {
            get;
            private set;
        }

        /// <summary>
        /// 价值
        /// </summary>
        public int Value
        {
            get;
            private set;
        }

        /// <summary>
        /// 重量(克)
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
            Type = int.Parse(columnStrings[index++]);
            Rarity = int.Parse(columnStrings[index++]);
            IsOnlyInGame = bool.Parse(columnStrings[index++]);
            Description = columnStrings[index++];
            IconId = int.Parse(columnStrings[index++]);
            CanStack = bool.Parse(columnStrings[index++]);
            MaxStackCount = int.Parse(columnStrings[index++]);
            SellPrice = int.Parse(columnStrings[index++]);
            Value = int.Parse(columnStrings[index++]);
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
                    Type = binaryReader.Read7BitEncodedInt32();
                    Rarity = binaryReader.Read7BitEncodedInt32();
                    IsOnlyInGame = binaryReader.ReadBoolean();
                    Description = binaryReader.ReadString();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    CanStack = binaryReader.ReadBoolean();
                    MaxStackCount = binaryReader.Read7BitEncodedInt32();
                    SellPrice = binaryReader.Read7BitEncodedInt32();
                    Value = binaryReader.Read7BitEncodedInt32();
                    Weight = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
