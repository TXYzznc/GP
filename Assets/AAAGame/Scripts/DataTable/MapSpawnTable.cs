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
    /// 地图ID（主键）
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 敌人配置(格式:101:30,102:50)
        /// </summary>
        public string SpawnEnemys
        {
            get;
            private set;
        }

        /// <summary>
        /// 敌人数量范围(min,max)
        /// </summary>
        public int[] EnemyNums
        {
            get;
            private set;
        }

        /// <summary>
        /// 宝箱配置(格式:201:40,202:60)
        /// </summary>
        public string SpawnTreasures
        {
            get;
            private set;
        }

        /// <summary>
        /// 宝箱数量范围(min,max)
        /// </summary>
        public int[] TreasureNums
        {
            get;
            private set;
        }

        /// <summary>
        /// 等级系数范围(min,max)
        /// </summary>
        public int[] LevelCoefficient
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
            SpawnEnemys = columnStrings[index++];
            EnemyNums = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            SpawnTreasures = columnStrings[index++];
            TreasureNums = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            LevelCoefficient = DataTableExtension.ParseArray<int>(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    SpawnEnemys = binaryReader.ReadString();
                    EnemyNums = binaryReader.ReadArray<int>();
                    SpawnTreasures = binaryReader.ReadString();
                    TreasureNums = binaryReader.ReadArray<int>();
                    LevelCoefficient = binaryReader.ReadArray<int>();
                }
            }

            return true;
        }
}
