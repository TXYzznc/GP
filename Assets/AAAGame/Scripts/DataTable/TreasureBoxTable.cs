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
        /// 物品组ID列表
        /// </summary>
        public int[] ItemGroupIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 有金币的概率
        /// </summary>
        public double CoinsProbability
        {
            get;
            private set;
        }

        /// <summary>
        /// 最小金币数量
        /// </summary>
        public int MinCoins
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大金币数量
        /// </summary>
        public int MaxCoins
        {
            get;
            private set;
        }

        /// <summary>
        /// 有灵石的概率
        /// </summary>
        public double MagicaStoneProbability
        {
            get;
            private set;
        }

        /// <summary>
        /// 最小灵石数量
        /// </summary>
        public int MinMagicaStone
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大灵石数量
        /// </summary>
        public int MaxMagicaStone
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
            ItemGroupIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            CoinsProbability = double.Parse(columnStrings[index++]);
            MinCoins = int.Parse(columnStrings[index++]);
            MaxCoins = int.Parse(columnStrings[index++]);
            MagicaStoneProbability = double.Parse(columnStrings[index++]);
            MinMagicaStone = int.Parse(columnStrings[index++]);
            MaxMagicaStone = int.Parse(columnStrings[index++]);
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
                    ItemGroupIds = binaryReader.ReadArray<int>();
                    CoinsProbability = binaryReader.ReadDouble();
                    MinCoins = binaryReader.Read7BitEncodedInt32();
                    MaxCoins = binaryReader.Read7BitEncodedInt32();
                    MagicaStoneProbability = binaryReader.ReadDouble();
                    MinMagicaStone = binaryReader.Read7BitEncodedInt32();
                    MaxMagicaStone = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                }
            }

            return true;
        }
}
