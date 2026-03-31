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
/// ColorTable
/// </summary>
public partial class ColorTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// ID编号
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 颜色名称
        /// </summary>
        public string ColorName
        {
            get;
            private set;
        }

        /// <summary>
        /// 十六进制色值（支持RGB和RGBA格式：#RGB、#RRGGBB、#RGBA、#RRGGBBAA）
        /// </summary>
        public string ColorHex
        {
            get;
            private set;
        }

        /// <summary>
        /// 颜色描述/用途
        /// </summary>
        public string Description
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
            ColorName = columnStrings[index++];
            ColorHex = columnStrings[index++];
            Description = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    ColorName = binaryReader.ReadString();
                    ColorHex = binaryReader.ReadString();
                    Description = binaryReader.ReadString();
                }
            }

            return true;
        }
}
