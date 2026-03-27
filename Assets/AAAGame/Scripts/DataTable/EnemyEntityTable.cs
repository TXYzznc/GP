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
/// EnemyEntityTable
/// </summary>
public partial class EnemyEntityTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 敌人ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 敌人名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 敌人类型
        /// </summary>
        public int EnemyType
        {
            get;
            private set;
        }

        /// <summary>
        /// 净化后获得的卡牌ID
        /// </summary>
        public int PurificationCardId
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否会广播
        /// </summary>
        public bool CanBroadcast
        {
            get;
            private set;
        }

        /// <summary>
        /// 广播距离
        /// </summary>
        public float BroadcastDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 掉落奖励等级
        /// </summary>
        public int RewardTier
        {
            get;
            private set;
        }

        /// <summary>
        /// 预制体路径
        /// </summary>
        public int PrefabId
        {
            get;
            private set;
        }

        /// <summary>
        /// 关联的战斗配置ID（EnemyTable）
        /// </summary>
        public int BattleConfigId
        {
            get;
            private set;
        }

        /// <summary>
        /// 巡逻半径（米）
        /// </summary>
        public float PatrolRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 巡逻速度
        /// </summary>
        public float PatrolSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 休息概率（0-1）
        /// </summary>
        public float RestProbability
        {
            get;
            private set;
        }

        /// <summary>
        /// 休息时长（秒）
        /// </summary>
        public float RestDuration
        {
            get;
            private set;
        }

        /// <summary>
        /// 警戒距离（米）
        /// </summary>
        public float AlertDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 周围圈检测半径
        /// </summary>
        public float VisionCircleRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 扇形视野角度
        /// </summary>
        public float VisionConeAngle
        {
            get;
            private set;
        }

        /// <summary>
        /// 扇形检测距离
        /// </summary>
        public float VisionConeDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 警觉度增长速率（/秒）
        /// </summary>
        public float AlertIncreaseRate
        {
            get;
            private set;
        }

        /// <summary>
        /// 警觉度衰减速率（/秒）
        /// </summary>
        public float AlertDecreaseRate
        {
            get;
            private set;
        }

        /// <summary>
        /// 触发警戒的阈值
        /// </summary>
        public float AlertThreshold
        {
            get;
            private set;
        }

        /// <summary>
        /// 警戒时长（秒，发现玩家前）
        /// </summary>
        public float AlertTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 发现距离（米，近距离快速发现）
        /// </summary>
        public float DetectDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 近距离发现时长（秒）
        /// </summary>
        public float DetectTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 追击速度
        /// </summary>
        public float ChaseSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 追击距离（超过此距离放弃追击）
        /// </summary>
        public float ChaseDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 进入战斗距离（米）
        /// </summary>
        public float CombatDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 难度系数（1-5）
        /// </summary>
        public int Difficulty
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
            Description = columnStrings[index++];
            EnemyType = int.Parse(columnStrings[index++]);
            PurificationCardId = int.Parse(columnStrings[index++]);
            CanBroadcast = bool.Parse(columnStrings[index++]);
            BroadcastDistance = float.Parse(columnStrings[index++]);
            RewardTier = int.Parse(columnStrings[index++]);
            PrefabId = int.Parse(columnStrings[index++]);
            BattleConfigId = int.Parse(columnStrings[index++]);
            PatrolRadius = float.Parse(columnStrings[index++]);
            PatrolSpeed = float.Parse(columnStrings[index++]);
            RestProbability = float.Parse(columnStrings[index++]);
            RestDuration = float.Parse(columnStrings[index++]);
            AlertDistance = float.Parse(columnStrings[index++]);
            VisionCircleRadius = float.Parse(columnStrings[index++]);
            VisionConeAngle = float.Parse(columnStrings[index++]);
            VisionConeDistance = float.Parse(columnStrings[index++]);
            AlertIncreaseRate = float.Parse(columnStrings[index++]);
            AlertDecreaseRate = float.Parse(columnStrings[index++]);
            AlertThreshold = float.Parse(columnStrings[index++]);
            AlertTime = float.Parse(columnStrings[index++]);
            DetectDistance = float.Parse(columnStrings[index++]);
            DetectTime = float.Parse(columnStrings[index++]);
            ChaseSpeed = float.Parse(columnStrings[index++]);
            ChaseDistance = float.Parse(columnStrings[index++]);
            CombatDistance = float.Parse(columnStrings[index++]);
            Difficulty = int.Parse(columnStrings[index++]);

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
                    Description = binaryReader.ReadString();
                    EnemyType = binaryReader.Read7BitEncodedInt32();
                    PurificationCardId = binaryReader.Read7BitEncodedInt32();
                    CanBroadcast = binaryReader.ReadBoolean();
                    BroadcastDistance = binaryReader.ReadSingle();
                    RewardTier = binaryReader.Read7BitEncodedInt32();
                    PrefabId = binaryReader.Read7BitEncodedInt32();
                    BattleConfigId = binaryReader.Read7BitEncodedInt32();
                    PatrolRadius = binaryReader.ReadSingle();
                    PatrolSpeed = binaryReader.ReadSingle();
                    RestProbability = binaryReader.ReadSingle();
                    RestDuration = binaryReader.ReadSingle();
                    AlertDistance = binaryReader.ReadSingle();
                    VisionCircleRadius = binaryReader.ReadSingle();
                    VisionConeAngle = binaryReader.ReadSingle();
                    VisionConeDistance = binaryReader.ReadSingle();
                    AlertIncreaseRate = binaryReader.ReadSingle();
                    AlertDecreaseRate = binaryReader.ReadSingle();
                    AlertThreshold = binaryReader.ReadSingle();
                    AlertTime = binaryReader.ReadSingle();
                    DetectDistance = binaryReader.ReadSingle();
                    DetectTime = binaryReader.ReadSingle();
                    ChaseSpeed = binaryReader.ReadSingle();
                    ChaseDistance = binaryReader.ReadSingle();
                    CombatDistance = binaryReader.ReadSingle();
                    Difficulty = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
