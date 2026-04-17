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
/// SceneTable
/// </summary>
public partial class SceneTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 场景ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 场景名称（Unity场景名）
        /// </summary>
        public string SceneName
        {
            get;
            private set;
        }

        /// <summary>
        /// 场景类型（1=基地, 2=大世界, 3=引导, 4=副本）
        /// </summary>
        public int SceneType
        {
            get;
            private set;
        }

        /// <summary>
        /// 场景显示名称（本地化Key）
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// 场景描述（本地化Key）
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 进入条件类型（0=无条件, 1=完成引导, 2=完成任务, 3=达到等级, 4=拥有物品, 5=解锁科技, 99=自定义）
        /// </summary>
        public int ConditionType
        {
            get;
            private set;
        }

        /// <summary>
        /// 进入条件参数
        /// </summary>
        public int[] ConditionParam
        {
            get;
            private set;
        }

        /// <summary>
        /// 默认出生点ID（对应 PosTable）
        /// </summary>
        public int DefaultSpawnPosId
        {
            get;
            private set;
        }

        /// <summary>
        /// 场景等级推荐
        /// </summary>
        public int RecommendLevel
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
            SceneName = columnStrings[index++];
            SceneType = int.Parse(columnStrings[index++]);
            DisplayName = columnStrings[index++];
            Description = columnStrings[index++];
            ConditionType = int.Parse(columnStrings[index++]);
            ConditionParam = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            DefaultSpawnPosId = int.Parse(columnStrings[index++]);
            RecommendLevel = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    SceneName = binaryReader.ReadString();
                    SceneType = binaryReader.Read7BitEncodedInt32();
                    DisplayName = binaryReader.ReadString();
                    Description = binaryReader.ReadString();
                    ConditionType = binaryReader.Read7BitEncodedInt32();
                    ConditionParam = binaryReader.ReadArray<int>();
                    DefaultSpawnPosId = binaryReader.Read7BitEncodedInt32();
                    RecommendLevel = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
