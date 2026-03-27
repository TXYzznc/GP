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
/// EnemyTable
/// </summary>
public partial class EnemyTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 敌人关联的战斗配置ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 敌人名称
        /// </summary>
        public string EnemyName
        {
            get;
            private set;
        }

        /// <summary>
        /// （对战时）最小人口数
        /// </summary>
        public int MinPopulation
        {
            get;
            private set;
        }

        /// <summary>
        /// （对战时）最大人口数
        /// </summary>
        public int MaxPopulation
        {
            get;
            private set;
        }

        /// <summary>
        /// 波次数量
        /// </summary>
        public int WaveCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子ID列表
        /// </summary>
        public int[] ChessIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 阵型类型（1=横排，2=竖排，3=矩形）
        /// </summary>
        public int FormationType
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋子间距（米）
        /// </summary>
        public float Spacing
        {
            get;
            private set;
        }

        /// <summary>
        /// 难度倍率（影响棋子属性）
        /// </summary>
        public float DifficultyMultiplier
        {
            get;
            private set;
        }

        /// <summary>
        /// 奖励倍率
        /// </summary>
        public float RewardMultiplier
        {
            get;
            private set;
        }

        /// <summary>
        /// 战斗时间限制（秒，0=无限制）
        /// </summary>
        public int TimeLimit
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
            EnemyName = columnStrings[index++];
            MinPopulation = int.Parse(columnStrings[index++]);
            MaxPopulation = int.Parse(columnStrings[index++]);
            WaveCount = int.Parse(columnStrings[index++]);
            ChessIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            FormationType = int.Parse(columnStrings[index++]);
            Spacing = float.Parse(columnStrings[index++]);
            DifficultyMultiplier = float.Parse(columnStrings[index++]);
            RewardMultiplier = float.Parse(columnStrings[index++]);
            TimeLimit = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    EnemyName = binaryReader.ReadString();
                    MinPopulation = binaryReader.Read7BitEncodedInt32();
                    MaxPopulation = binaryReader.Read7BitEncodedInt32();
                    WaveCount = binaryReader.Read7BitEncodedInt32();
                    ChessIds = binaryReader.ReadArray<int>();
                    FormationType = binaryReader.Read7BitEncodedInt32();
                    Spacing = binaryReader.ReadSingle();
                    DifficultyMultiplier = binaryReader.ReadSingle();
                    RewardMultiplier = binaryReader.ReadSingle();
                    TimeLimit = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
