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
/// PlayerDataTable
/// </summary>
public partial class PlayerDataTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 玩家等级
        /// </summary>
        public int Level
        {
            get;
            private set;
        }

        /// <summary>
        /// 所需经验
        /// </summary>
        public int RequiredExp
        {
            get;
            private set;
        }

        /// <summary>
        /// 统御力上限
        /// </summary>
        public int MaxDomination
        {
            get;
            private set;
        }

        /// <summary>
        /// 背包容量
        /// </summary>
        public int InventorySize
        {
            get;
            private set;
        }

        /// <summary>
        /// 圣水携带上限
        /// </summary>
        public int MaxHolyWater
        {
            get;
            private set;
        }

        /// <summary>
        /// 解锁功能
        /// </summary>
        public string UnlockFeature
        {
            get;
            private set;
        }

        /// <summary>
        /// 奖励道具ID
        /// </summary>
        public int RewardItemId
        {
            get;
            private set;
        }

        /// <summary>
        /// 奖励数量
        /// </summary>
        public int RewardCount
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
            Level = int.Parse(columnStrings[index++]);
            RequiredExp = int.Parse(columnStrings[index++]);
            MaxDomination = int.Parse(columnStrings[index++]);
            InventorySize = int.Parse(columnStrings[index++]);
            MaxHolyWater = int.Parse(columnStrings[index++]);
            UnlockFeature = columnStrings[index++];
            RewardItemId = int.Parse(columnStrings[index++]);
            RewardCount = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Level = binaryReader.Read7BitEncodedInt32();
                    RequiredExp = binaryReader.Read7BitEncodedInt32();
                    MaxDomination = binaryReader.Read7BitEncodedInt32();
                    InventorySize = binaryReader.Read7BitEncodedInt32();
                    MaxHolyWater = binaryReader.Read7BitEncodedInt32();
                    UnlockFeature = binaryReader.ReadString();
                    RewardItemId = binaryReader.Read7BitEncodedInt32();
                    RewardCount = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
