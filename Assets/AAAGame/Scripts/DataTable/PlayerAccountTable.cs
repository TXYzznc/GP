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
/// PlayerAccountTable
/// </summary>
public partial class PlayerAccountTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 玩家ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 存档索引
        /// </summary>
        public int Index
        {
            get;
            private set;
        }

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string PlayerName
        {
            get;
            private set;
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public double CreateTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 最后登录
        /// </summary>
        public double LastLoginTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 全局等级
        /// </summary>
        public int GlobalLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前经验
        /// </summary>
        public int CurrentExp
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前召唤师
        /// </summary>
        public int CurrentSummonerId
        {
            get;
            private set;
        }

        /// <summary>
        /// 已解锁召唤师信息
        /// </summary>
        public string UnlockedSummonerInfo
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前召唤师阶段
        /// </summary>
        public int SummonerPhases
        {
            get;
            private set;
        }

        /// <summary>
        /// 拥有棋子卡
        /// </summary>
        public int[] OwnedUnitCardIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 拥有策略卡
        /// </summary>
        public int[] OwnedStrategyCardIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 已解锁科技
        /// </summary>
        public int[] UnlockedTechIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 金币
        /// </summary>
        public int Gold
        {
            get;
            private set;
        }

        /// <summary>
        /// 起源石
        /// </summary>
        public int OriginStone
        {
            get;
            private set;
        }

        /// <summary>
        /// 背包物品
        /// </summary>
        public string InventoryItems
        {
            get;
            private set;
        }

        /// <summary>
        /// 背包容量
        /// </summary>
        public int InventoryCapacity
        {
            get;
            private set;
        }

        /// <summary>
        /// 卡组数据
        /// </summary>
        public string SavedDecks
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前卡组
        /// </summary>
        public int[] CurrentDeckIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// 已完成任务
        /// </summary>
        public int[] CompletedQuestIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否完成新手引导
        /// </summary>
        public bool HasCompletedTutorial
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前所在场景ID
        /// </summary>
        public int CurrentSceneId
        {
            get;
            private set;
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        public string Settings
        {
            get;
            private set;
        }

        /// <summary>
        /// 统计数据
        /// </summary>
        public string Statistics
        {
            get;
            private set;
        }

        /// <summary>
        /// 玩家坐标
        /// </summary>
        public Vector3 PlayerPos
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
            Index = int.Parse(columnStrings[index++]);
            PlayerName = columnStrings[index++];
            CreateTime = double.Parse(columnStrings[index++]);
            LastLoginTime = double.Parse(columnStrings[index++]);
            GlobalLevel = int.Parse(columnStrings[index++]);
            CurrentExp = int.Parse(columnStrings[index++]);
            CurrentSummonerId = int.Parse(columnStrings[index++]);
            UnlockedSummonerInfo = columnStrings[index++];
            SummonerPhases = int.Parse(columnStrings[index++]);
            OwnedUnitCardIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            OwnedStrategyCardIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            UnlockedTechIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            Gold = int.Parse(columnStrings[index++]);
            OriginStone = int.Parse(columnStrings[index++]);
            InventoryItems = columnStrings[index++];
            InventoryCapacity = int.Parse(columnStrings[index++]);
            SavedDecks = columnStrings[index++];
            CurrentDeckIndex = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            CompletedQuestIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            HasCompletedTutorial = bool.Parse(columnStrings[index++]);
            CurrentSceneId = int.Parse(columnStrings[index++]);
            Settings = columnStrings[index++];
            Statistics = columnStrings[index++];
            PlayerPos = DataTableExtension.ParseVector3(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Index = binaryReader.Read7BitEncodedInt32();
                    PlayerName = binaryReader.ReadString();
                    CreateTime = binaryReader.ReadDouble();
                    LastLoginTime = binaryReader.ReadDouble();
                    GlobalLevel = binaryReader.Read7BitEncodedInt32();
                    CurrentExp = binaryReader.Read7BitEncodedInt32();
                    CurrentSummonerId = binaryReader.Read7BitEncodedInt32();
                    UnlockedSummonerInfo = binaryReader.ReadString();
                    SummonerPhases = binaryReader.Read7BitEncodedInt32();
                    OwnedUnitCardIds = binaryReader.ReadArray<int>();
                    OwnedStrategyCardIds = binaryReader.ReadArray<int>();
                    UnlockedTechIds = binaryReader.ReadArray<int>();
                    Gold = binaryReader.Read7BitEncodedInt32();
                    OriginStone = binaryReader.Read7BitEncodedInt32();
                    InventoryItems = binaryReader.ReadString();
                    InventoryCapacity = binaryReader.Read7BitEncodedInt32();
                    SavedDecks = binaryReader.ReadString();
                    CurrentDeckIndex = binaryReader.ReadArray<int>();
                    CompletedQuestIds = binaryReader.ReadArray<int>();
                    HasCompletedTutorial = binaryReader.ReadBoolean();
                    CurrentSceneId = binaryReader.Read7BitEncodedInt32();
                    Settings = binaryReader.ReadString();
                    Statistics = binaryReader.ReadString();
                    PlayerPos = binaryReader.ReadVector3();
                }
            }

            return true;
        }
}
