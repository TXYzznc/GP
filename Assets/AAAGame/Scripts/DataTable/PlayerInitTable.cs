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
/// PlayerInitTable
/// </summary>
public partial class PlayerInitTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 配置ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 初始等级
        /// </summary>
        public int InitLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始经验
        /// </summary>
        public int InitExp
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始金币
        /// </summary>
        public int InitGold
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始钻石
        /// </summary>
        public int InitDiamond
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始圣水
        /// </summary>
        public int InitHolyWater
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始召唤师
        /// </summary>
        public int InitSummonerId
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始棋子卡
        /// </summary>
        public int[] InitUnitCards
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始策略卡
        /// </summary>
        public int[] InitStrategyCards
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始科技
        /// </summary>
        public int[] InitTechs
        {
            get;
            private set;
        }

        /// <summary>
        /// 背包容量
        /// </summary>
        public int InitInventorySize
        {
            get;
            private set;
        }

        /// <summary>
        /// 经验倍率
        /// </summary>
        public float ExpMultiplier
        {
            get;
            private set;
        }

        /// <summary>
        /// 精英怪概率
        /// </summary>
        public float EliteSpawnRate
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
            InitLevel = int.Parse(columnStrings[index++]);
            InitExp = int.Parse(columnStrings[index++]);
            InitGold = int.Parse(columnStrings[index++]);
            InitDiamond = int.Parse(columnStrings[index++]);
            InitHolyWater = int.Parse(columnStrings[index++]);
            InitSummonerId = int.Parse(columnStrings[index++]);
            InitUnitCards = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            InitStrategyCards = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            InitTechs = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            InitInventorySize = int.Parse(columnStrings[index++]);
            ExpMultiplier = float.Parse(columnStrings[index++]);
            EliteSpawnRate = float.Parse(columnStrings[index++]);
            Desc = columnStrings[index++];
            index++;
            index++;

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    InitLevel = binaryReader.Read7BitEncodedInt32();
                    InitExp = binaryReader.Read7BitEncodedInt32();
                    InitGold = binaryReader.Read7BitEncodedInt32();
                    InitDiamond = binaryReader.Read7BitEncodedInt32();
                    InitHolyWater = binaryReader.Read7BitEncodedInt32();
                    InitSummonerId = binaryReader.Read7BitEncodedInt32();
                    InitUnitCards = binaryReader.ReadArray<int>();
                    InitStrategyCards = binaryReader.ReadArray<int>();
                    InitTechs = binaryReader.ReadArray<int>();
                    InitInventorySize = binaryReader.Read7BitEncodedInt32();
                    ExpMultiplier = binaryReader.ReadSingle();
                    EliteSpawnRate = binaryReader.ReadSingle();
                    Desc = binaryReader.ReadString();
                }
            }

            return true;
        }
}
