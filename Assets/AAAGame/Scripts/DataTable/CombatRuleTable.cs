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
/// CombatRuleTable
/// </summary>
public partial class CombatRuleTable : DataRowBase
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
        /// 准备阶段倒计时秒数
        /// </summary>
        public float PreparationDurationSeconds
        {
            get;
            private set;
        }

        /// <summary>
        /// 进入战斗提示展示时长秒数
        /// </summary>
        public float EnterCombatTipDisplayDurationSeconds
        {
            get;
            private set;
        }

        /// <summary>
        /// 等待进入战斗提示关闭超时秒数
        /// </summary>
        public float EnterCombatTipCloseTimeoutSeconds
        {
            get;
            private set;
        }

        /// <summary>
        /// 失败污染值增加
        /// </summary>
        public float DefeatCorruptionAdd
        {
            get;
            private set;
        }

        /// <summary>
        /// 胜利宝箱延迟显示秒数
        /// </summary>
        public float VictoryTreasureDelaySeconds
        {
            get;
            private set;
        }

        /// <summary>
        /// 失败自动返回探索延迟秒数
        /// </summary>
        public float DefeatAutoReturnDelaySeconds
        {
            get;
            private set;
        }

        /// <summary>
        /// 胜利物品奖励最小数量
        /// </summary>
        public int MinItemAwardCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 胜利物品奖励最大数量
        /// </summary>
        public int MaxItemAwardCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 战斗时相机视野
        /// </summary>
        public int CameraView
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
            PreparationDurationSeconds = float.Parse(columnStrings[index++]);
            EnterCombatTipDisplayDurationSeconds = float.Parse(columnStrings[index++]);
            EnterCombatTipCloseTimeoutSeconds = float.Parse(columnStrings[index++]);
            DefeatCorruptionAdd = float.Parse(columnStrings[index++]);
            VictoryTreasureDelaySeconds = float.Parse(columnStrings[index++]);
            DefeatAutoReturnDelaySeconds = float.Parse(columnStrings[index++]);
            MinItemAwardCount = int.Parse(columnStrings[index++]);
            MaxItemAwardCount = int.Parse(columnStrings[index++]);
            CameraView = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PreparationDurationSeconds = binaryReader.ReadSingle();
                    EnterCombatTipDisplayDurationSeconds = binaryReader.ReadSingle();
                    EnterCombatTipCloseTimeoutSeconds = binaryReader.ReadSingle();
                    DefeatCorruptionAdd = binaryReader.ReadSingle();
                    VictoryTreasureDelaySeconds = binaryReader.ReadSingle();
                    DefeatAutoReturnDelaySeconds = binaryReader.ReadSingle();
                    MinItemAwardCount = binaryReader.Read7BitEncodedInt32();
                    MaxItemAwardCount = binaryReader.Read7BitEncodedInt32();
                    CameraView = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
