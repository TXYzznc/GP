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
        public int Quality
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
        /// 缩略图资源ID
        /// </summary>
        public int IconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 详细图资源ID
        /// </summary>
        public int DetailIconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否可堆叠
        /// </summary>
        public int CanStack
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
        /// 是否可使用
        /// </summary>
        public int CanUse
        {
            get;
            private set;
        }

        /// <summary>
        /// 使用效果ID
        /// </summary>
        public int UseEffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否可装备
        /// </summary>
        public int CanEquip
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
        /// 词条池ID列表
        /// </summary>
        public int[] AffixPoolIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 词条最小数量
        /// </summary>
        public int AffixMinCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 词条最大数量
        /// </summary>
        public int AffixMaxCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 羁绊ID列表
        /// </summary>
        public int[] SynergyIds
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

        /// <summary>
        /// 售价
        /// </summary>
        public int SellPrice
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

        /// <summary>
        /// 最大耐久度
        /// </summary>
        public int MaxDurability
        {
            get;
            private set;
        }

        /// <summary>
        /// 稀有度
        /// </summary>
        public int Rarity
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
            Quality = int.Parse(columnStrings[index++]);
            Description = columnStrings[index++];
            IconId = int.Parse(columnStrings[index++]);
            DetailIconId = int.Parse(columnStrings[index++]);
            CanStack = int.Parse(columnStrings[index++]);
            MaxStackCount = int.Parse(columnStrings[index++]);
            CanUse = int.Parse(columnStrings[index++]);
            UseEffectId = int.Parse(columnStrings[index++]);
            CanEquip = int.Parse(columnStrings[index++]);
            SpecialEffectId = int.Parse(columnStrings[index++]);
            AffixPoolIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            AffixMinCount = int.Parse(columnStrings[index++]);
            AffixMaxCount = int.Parse(columnStrings[index++]);
            SynergyIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            BaseAttributes = columnStrings[index++];
            SellPrice = int.Parse(columnStrings[index++]);
            Weight = int.Parse(columnStrings[index++]);
            MaxDurability = int.Parse(columnStrings[index++]);
            Rarity = int.Parse(columnStrings[index++]);

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
                    Quality = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    DetailIconId = binaryReader.Read7BitEncodedInt32();
                    CanStack = binaryReader.Read7BitEncodedInt32();
                    MaxStackCount = binaryReader.Read7BitEncodedInt32();
                    CanUse = binaryReader.Read7BitEncodedInt32();
                    UseEffectId = binaryReader.Read7BitEncodedInt32();
                    CanEquip = binaryReader.Read7BitEncodedInt32();
                    SpecialEffectId = binaryReader.Read7BitEncodedInt32();
                    AffixPoolIds = binaryReader.ReadArray<int>();
                    AffixMinCount = binaryReader.Read7BitEncodedInt32();
                    AffixMaxCount = binaryReader.Read7BitEncodedInt32();
                    SynergyIds = binaryReader.ReadArray<int>();
                    BaseAttributes = binaryReader.ReadString();
                    SellPrice = binaryReader.Read7BitEncodedInt32();
                    Weight = binaryReader.Read7BitEncodedInt32();
                    MaxDurability = binaryReader.Read7BitEncodedInt32();
                    Rarity = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
