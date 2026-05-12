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
/// CombatEffectTable
/// </summary>
public partial class CombatEffectTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 唯一标识
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 关联SpecialEffectTable的ID
        /// </summary>
        public int SpecialEffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 类别：1=玩家先手,2=敌方先手,3=偷袭
        /// </summary>
        public int Category
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
        /// 随机权重
        /// </summary>
        public int Weight
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
            SpecialEffectId = int.Parse(columnStrings[index++]);
            Category = int.Parse(columnStrings[index++]);
            IconId = int.Parse(columnStrings[index++]);
            Weight = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    SpecialEffectId = binaryReader.Read7BitEncodedInt32();
                    Category = binaryReader.Read7BitEncodedInt32();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    Weight = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
