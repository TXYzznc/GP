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
/// LanguageTextTable
/// </summary>
public partial class LanguageTextTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 文本Key
        /// </summary>
        public string TextKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 打字机速度
        /// </summary>
        public float TypeSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 文本类型
        /// </summary>
        public int TextType
        {
            get;
            private set;
        }

        /// <summary>
        /// 中文简体
        /// </summary>
        public string ChineseSimplified
        {
            get;
            private set;
        }

        /// <summary>
        /// 英文
        /// </summary>
        public string English
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
            TextKey = columnStrings[index++];
            TypeSpeed = float.Parse(columnStrings[index++]);
            TextType = int.Parse(columnStrings[index++]);
            ChineseSimplified = columnStrings[index++];
            English = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    TextKey = binaryReader.ReadString();
                    TypeSpeed = binaryReader.ReadSingle();
                    TextType = binaryReader.Read7BitEncodedInt32();
                    ChineseSimplified = binaryReader.ReadString();
                    English = binaryReader.ReadString();
                }
            }

            return true;
        }
}
