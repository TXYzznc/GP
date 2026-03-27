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
/// SynergyTable
/// </summary>
public partial class SynergyTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 羁绊ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 羁绊名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 羁绊类型
        /// </summary>
        public int Type
        {
            get;
            private set;
        }

        /// <summary>
        /// 羁绊描述
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 激活所需数量
        /// </summary>
        public int RequireCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 需要的物品ID列表
        /// </summary>
        public int[] RequireIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 羁绊效果ID
        /// </summary>
        public int EffectId
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
            Type = int.Parse(columnStrings[index++]);
            Description = columnStrings[index++];
            RequireCount = int.Parse(columnStrings[index++]);
            RequireIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            EffectId = int.Parse(columnStrings[index++]);

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
                    Type = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                    RequireCount = binaryReader.Read7BitEncodedInt32();
                    RequireIds = binaryReader.ReadArray<int>();
                    EffectId = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
