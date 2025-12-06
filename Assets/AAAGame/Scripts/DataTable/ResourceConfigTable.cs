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
/// ResourceConfigTable
/// </summary>
public class ResourceConfigTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 资源ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 资源类型：1 = Sprite（图片）2 = Prefab（预制体）3 = Effect（特效）4 = Material（材质）5 = Texture（纹理）
        /// </summary>
        public int Type
        {
            get;
            private set;
        }

        /// <summary>
        /// 资源路径
        /// </summary>
        public string Path
        {
            get;
            private set;
        }

        /// <summary>
        /// 资源名称
        /// </summary>
        public string Name
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
            Type = int.Parse(columnStrings[index++]);
            Path = columnStrings[index++];
            Name = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Type = binaryReader.Read7BitEncodedInt32();
                    Path = binaryReader.ReadString();
                    Name = binaryReader.ReadString();
                }
            }

            return true;
        }
}
