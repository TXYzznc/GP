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
/// MapSpawnTable
/// </summary>
public partial class MapSpawnTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 主键
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 地图ID：（对应 SceneTable.Id）
        /// </summary>
        public int MapId
        {
            get;
            private set;
        }

        /// <summary>
        /// 生成类型：0=敌人 / 1=宝箱
        /// </summary>
        public int SpawnType
        {
            get;
            private set;
        }

        /// <summary>
        /// 生成目标的配置表ID
        /// </summary>
        public int SpawnTargetId
        {
            get;
            private set;
        }

        /// <summary>
        /// 权重
        /// </summary>
        public int Weight
        {
            get;
            private set;
        }

        /// <summary>
        /// 宝箱等级
        /// </summary>
        public int ChestLevel
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
            MapId = int.Parse(columnStrings[index++]);
            SpawnType = int.Parse(columnStrings[index++]);
            SpawnTargetId = int.Parse(columnStrings[index++]);
            Weight = int.Parse(columnStrings[index++]);
            ChestLevel = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    MapId = binaryReader.Read7BitEncodedInt32();
                    SpawnType = binaryReader.Read7BitEncodedInt32();
                    SpawnTargetId = binaryReader.Read7BitEncodedInt32();
                    Weight = binaryReader.Read7BitEncodedInt32();
                    ChestLevel = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
