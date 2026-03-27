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
/// BuffTable
/// </summary>
public partial class BuffTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 注册到BuffFactory中的
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// Buff名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Buff图标资源ID
        /// </summary>
        public int SpriteId
        {
            get;
            private set;
        }

        /// <summary>
        /// Buff描述
        /// </summary>
        public string Desc
        {
            get;
            private set;
        }

        /// <summary>
        /// Buff类型：- 1 : 增益 (Buff)- 2 : 减益 (Debuff)
        /// </summary>
        public int BuffType
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果类型：1 : 属性修改 (按比例/数值修改攻击、防御、速度等)2 : 周期性 (DOT/HOT，如流血、回春)3 : 护盾 (吸收伤害)4 : 状态改变 (控制类：眩晕、冰冻、沉默)5 : 特殊逻辑 (通过代码触发特定事件)
        /// </summary>
        public int EffectType
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果数值
        /// </summary>
        public double EffectValue
        {
            get;
            private set;
        }

        /// <summary>
        /// 属性修改参数(JSON)：例如 {"AtkDamage":"25%","AtkRange":"30%"}；值为数值表示固定增量，带%表示按当前属性百分比计算
        /// </summary>
        public string StatMods
        {
            get;
            private set;
        }

        /// <summary>
        /// 自定义参数(JSON)：用于特殊效果Buff，例如 {"SpecialState":"Stun"}
        /// </summary>
        public string CustomData
        {
            get;
            private set;
        }

        /// <summary>
        /// 持续时间：- > 0 : 持续秒数- 0 : 永久生效（直到被手动移除或事件结束）
        /// </summary>
        public double Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 触发间隔：仅对周期性 Buff 有效，例如每 1 秒触发一次
        /// </summary>
        public double Interval
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大堆叠：相同 ID 的 Buff 最多可以叠加几层
        /// </summary>
        public int MaxStack
        {
            get;
            private set;
        }

        /// <summary>
        /// 互斥组：同一组 ID 的 Buff 可能会相互冲突
        /// </summary>
        public int MutexGroup
        {
            get;
            private set;
        }

        /// <summary>
        /// 互斥类型：- 0 : 不冲突。- 1 : 等级替换 (高等级 Buff 覆盖低等级)。- 2 : 无法共存 (新的直接替换旧的)。- 3 : 无法生效 (已有同组 Buff 时，新的无法附加)
        /// </summary>
        public int MutexType
        {
            get;
            private set;
        }

        /// <summary>
        /// Buff特效资源ID
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
            SpriteId = int.Parse(columnStrings[index++]);
            Desc = columnStrings[index++];
            BuffType = int.Parse(columnStrings[index++]);
            EffectType = int.Parse(columnStrings[index++]);
            EffectValue = double.Parse(columnStrings[index++]);
            StatMods = columnStrings[index++];
            CustomData = columnStrings[index++];
            Duration = double.Parse(columnStrings[index++]);
            Interval = double.Parse(columnStrings[index++]);
            MaxStack = int.Parse(columnStrings[index++]);
            MutexGroup = int.Parse(columnStrings[index++]);
            MutexType = int.Parse(columnStrings[index++]);
            EffectId = int.Parse(columnStrings[index++]);
            index++;
            index++;
            index++;
            index++;
            index++;
            index++;

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
                    SpriteId = binaryReader.Read7BitEncodedInt32();
                    Desc = binaryReader.ReadString();
                    BuffType = binaryReader.Read7BitEncodedInt32();
                    EffectType = binaryReader.Read7BitEncodedInt32();
                    EffectValue = binaryReader.ReadDouble();
                    StatMods = binaryReader.ReadString();
                    CustomData = binaryReader.ReadString();
                    Duration = binaryReader.ReadDouble();
                    Interval = binaryReader.ReadDouble();
                    MaxStack = binaryReader.Read7BitEncodedInt32();
                    MutexGroup = binaryReader.Read7BitEncodedInt32();
                    MutexType = binaryReader.Read7BitEncodedInt32();
                    EffectId = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
