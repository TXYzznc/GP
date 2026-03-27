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
/// PlayerSkillTable
/// </summary>
public partial class PlayerSkillTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 技能ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 冷却时间
        /// </summary>
        public float Cooldown
        {
            get;
            private set;
        }

        /// <summary>
        /// 灵力消耗
        /// </summary>
        public float Cost
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能描述
        /// </summary>
        public string Desc
        {
            get;
            private set;
        }

        /// <summary>
        /// 图标资源ID
        /// </summary>
        public int IconId
        {
            get;
            private set;
        }

        /// <summary>
        /// 槽位索引
        /// </summary>
        public int SlotIndex
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
            Cooldown = float.Parse(columnStrings[index++]);
            Cost = float.Parse(columnStrings[index++]);
            Name = columnStrings[index++];
            Desc = columnStrings[index++];
            IconId = int.Parse(columnStrings[index++]);
            SlotIndex = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Cooldown = binaryReader.ReadSingle();
                    Cost = binaryReader.ReadSingle();
                    Name = binaryReader.ReadString();
                    Desc = binaryReader.ReadString();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    SlotIndex = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
