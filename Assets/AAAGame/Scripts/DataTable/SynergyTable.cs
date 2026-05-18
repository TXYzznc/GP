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
        /// 羁绊描述
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否为宝物羁绊
        /// </summary>
        public int IsTreasureSynergy
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
        /// 羁绊效果ID
        /// </summary>
        public int EffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 应用范围（0表示只对携带者生效）
        /// </summary>
        public int ApplyScope
        {
            get;
            private set;
        }

        /// <summary>
        /// 羁绊图标资源ID（-1表示无图标）
        /// </summary>
        public int IconId
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
            IsTreasureSynergy = int.Parse(columnStrings[index++]);
            RequireCount = int.Parse(columnStrings[index++]);
            EffectId = int.Parse(columnStrings[index++]);
            ApplyScope = int.Parse(columnStrings[index++]);
            IconId = int.Parse(columnStrings[index++]);

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
                    IsTreasureSynergy = binaryReader.Read7BitEncodedInt32();
                    RequireCount = binaryReader.Read7BitEncodedInt32();
                    EffectId = binaryReader.Read7BitEncodedInt32();
                    ApplyScope = binaryReader.Read7BitEncodedInt32();
                    IconId = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
