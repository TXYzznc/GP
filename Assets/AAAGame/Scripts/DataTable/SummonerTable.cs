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
/// SummonerTable
/// </summary>
public partial class SummonerTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 召唤师ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 职业名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 对应 SummonChessTable 中召唤师的棋子行 ID
        /// </summary>
        public int SummonChessId
        {
            get;
            private set;
        }

        /// <summary>
        /// 职业类型
        /// </summary>
        public int ClassType
        {
            get;
            private set;
        }

        /// <summary>
        /// 阶段
        /// </summary>
        public int Phase
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础生命
        /// </summary>
        public float BaseHP
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础灵力
        /// </summary>
        public float BaseMP
        {
            get;
            private set;
        }

        /// <summary>
        /// 灵力恢复
        /// </summary>
        public float MPRegen
        {
            get;
            private set;
        }

        /// <summary>
        /// 召唤师移速
        /// </summary>
        public float MoveSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 玩家移速
        /// </summary>
        public float PlayerMoveSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 被动技能ID
        /// </summary>
        public int[] PassiveSkillIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 主动技能ID
        /// </summary>
        public int[] ActiveSkillIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 进阶条件类型
        /// </summary>
        public int AdvanceType
        {
            get;
            private set;
        }

        /// <summary>
        /// 进阶条件值
        /// </summary>
        public int AdvanceValue
        {
            get;
            private set;
        }

        /// <summary>
        /// 下一阶段ID
        /// </summary>
        public int NextPhaseId
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
        /// 预制体资源ID
        /// </summary>
        public int PrefabId
        {
            get;
            private set;
        }

        /// <summary>
        /// 立绘资源ID
        /// </summary>
        public int PortraitId
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
            SummonChessId = int.Parse(columnStrings[index++]);
            ClassType = int.Parse(columnStrings[index++]);
            Phase = int.Parse(columnStrings[index++]);
            BaseHP = float.Parse(columnStrings[index++]);
            BaseMP = float.Parse(columnStrings[index++]);
            MPRegen = float.Parse(columnStrings[index++]);
            MoveSpeed = float.Parse(columnStrings[index++]);
            PlayerMoveSpeed = float.Parse(columnStrings[index++]);
            PassiveSkillIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            ActiveSkillIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            AdvanceType = int.Parse(columnStrings[index++]);
            AdvanceValue = int.Parse(columnStrings[index++]);
            NextPhaseId = int.Parse(columnStrings[index++]);
            Description = columnStrings[index++];
            PrefabId = int.Parse(columnStrings[index++]);
            PortraitId = int.Parse(columnStrings[index++]);

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
                    SummonChessId = binaryReader.Read7BitEncodedInt32();
                    ClassType = binaryReader.Read7BitEncodedInt32();
                    Phase = binaryReader.Read7BitEncodedInt32();
                    BaseHP = binaryReader.ReadSingle();
                    BaseMP = binaryReader.ReadSingle();
                    MPRegen = binaryReader.ReadSingle();
                    MoveSpeed = binaryReader.ReadSingle();
                    PlayerMoveSpeed = binaryReader.ReadSingle();
                    PassiveSkillIds = binaryReader.ReadArray<int>();
                    ActiveSkillIds = binaryReader.ReadArray<int>();
                    AdvanceType = binaryReader.Read7BitEncodedInt32();
                    AdvanceValue = binaryReader.Read7BitEncodedInt32();
                    NextPhaseId = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                    PrefabId = binaryReader.Read7BitEncodedInt32();
                    PortraitId = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
