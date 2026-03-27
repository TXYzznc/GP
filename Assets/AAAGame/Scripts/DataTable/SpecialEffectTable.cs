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
/// SpecialEffectTable
/// </summary>
public partial class SpecialEffectTable : DataRowBase
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
        /// 效果名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果分类：1=玩家先手,2=敌人先手,3=玩家偷袭
        /// </summary>
        public int EffectCategory
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果类型：1=增益,2=减益,3=控制
        /// </summary>
        public int EffectType
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果描述（实时战斗用持续时间描述）
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 给目标附加的BuffID数组
        /// </summary>
        public int[] BuffIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 给自身附加的BuffID数组
        /// </summary>
        public int[] SelfBuffIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 冷却时间（秒，-1=无冷却）
        /// </summary>
        public double Cooldown
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
        /// 稀有度：1=普通,2=罕见,3=稀有,4=传奇
        /// </summary>
        public int Rarity
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
            Name = columnStrings[index++];
            EffectCategory = int.Parse(columnStrings[index++]);
            EffectType = int.Parse(columnStrings[index++]);
            Description = columnStrings[index++];
            BuffIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            SelfBuffIds = DataTableExtension.ParseArray<int>(columnStrings[index++]);
            Cooldown = double.Parse(columnStrings[index++]);
            IconId = int.Parse(columnStrings[index++]);
            Rarity = int.Parse(columnStrings[index++]);
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
                    Name = binaryReader.ReadString();
                    EffectCategory = binaryReader.Read7BitEncodedInt32();
                    EffectType = binaryReader.Read7BitEncodedInt32();
                    Description = binaryReader.ReadString();
                    BuffIds = binaryReader.ReadArray<int>();
                    SelfBuffIds = binaryReader.ReadArray<int>();
                    Cooldown = binaryReader.ReadDouble();
                    IconId = binaryReader.Read7BitEncodedInt32();
                    Rarity = binaryReader.Read7BitEncodedInt32();
                    Weight = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
