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
/// ProfessionAttributeModel
/// </summary>
public partial class ProfessionAttributeModel : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 配置ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 职业名称
        /// </summary>
        public string Profession
        {
            get;
            private set;
        }

        /// <summary>
        /// 功能类型
        /// </summary>
        public string Function
        {
            get;
            private set;
        }

        /// <summary>
        /// 品质等级
        /// </summary>
        public string Quality
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大生命值权重
        /// </summary>
        public double MaxHp
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大法力值权重
        /// </summary>
        public double MaxMp
        {
            get;
            private set;
        }

        /// <summary>
        /// 护甲权重
        /// </summary>
        public double Armor
        {
            get;
            private set;
        }

        /// <summary>
        /// 魔抗权重
        /// </summary>
        public double MagicResist
        {
            get;
            private set;
        }

        /// <summary>
        /// 法术强度权重
        /// </summary>
        public double SpellPower
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击力权重
        /// </summary>
        public double AtkDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 说明
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
            Profession = columnStrings[index++];
            Function = columnStrings[index++];
            Quality = columnStrings[index++];
            MaxHp = double.Parse(columnStrings[index++]);
            MaxMp = double.Parse(columnStrings[index++]);
            Armor = double.Parse(columnStrings[index++]);
            MagicResist = double.Parse(columnStrings[index++]);
            SpellPower = double.Parse(columnStrings[index++]);
            AtkDamage = double.Parse(columnStrings[index++]);
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
                    Profession = binaryReader.ReadString();
                    Function = binaryReader.ReadString();
                    Quality = binaryReader.ReadString();
                    MaxHp = binaryReader.ReadDouble();
                    MaxMp = binaryReader.ReadDouble();
                    Armor = binaryReader.ReadDouble();
                    MagicResist = binaryReader.ReadDouble();
                    SpellPower = binaryReader.ReadDouble();
                    AtkDamage = binaryReader.ReadDouble();
                    Description = binaryReader.ReadString();
                }
            }

            return true;
        }
}
