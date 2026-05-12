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
/// TargetSearchStrategyTable
/// </summary>
public partial class TargetSearchStrategyTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 策略ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 策略名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// AI类型(0=默认,1=近战,2=远程,99=Boss)
        /// </summary>
        public int AIType
        {
            get;
            private set;
        }

        /// <summary>
        /// 索敌距离(0=全场)
        /// </summary>
        public float SearchRange
        {
            get;
            private set;
        }

        /// <summary>
        /// 距离权重(0=不考虑)
        /// </summary>
        public float DistanceWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// 血量权重(0=不考虑)
        /// </summary>
        public float HpWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// 威胁度权重(0=不考虑)
        /// </summary>
        public float ThreatWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// 描述
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
            Name = columnStrings[index++];
            AIType = int.Parse(columnStrings[index++]);
            SearchRange = float.Parse(columnStrings[index++]);
            DistanceWeight = float.Parse(columnStrings[index++]);
            HpWeight = float.Parse(columnStrings[index++]);
            ThreatWeight = float.Parse(columnStrings[index++]);
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
                    Name = binaryReader.ReadString();
                    AIType = binaryReader.Read7BitEncodedInt32();
                    SearchRange = binaryReader.ReadSingle();
                    DistanceWeight = binaryReader.ReadSingle();
                    HpWeight = binaryReader.ReadSingle();
                    ThreatWeight = binaryReader.ReadSingle();
                    Description = binaryReader.ReadString();
                }
            }

            return true;
        }
}
