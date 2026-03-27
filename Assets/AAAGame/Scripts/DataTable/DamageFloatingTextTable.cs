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
/// DamageFloatingTextTable
/// </summary>
public partial class DamageFloatingTextTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 伤害类型ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 伤害类型名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 字体大小
        /// </summary>
        public float FontSize
        {
            get;
            private set;
        }

        /// <summary>
        /// 文字颜色(RGBA)
        /// </summary>
        public string TextColor
        {
            get;
            private set;
        }

        /// <summary>
        /// 描边颜色(RGBA)
        /// </summary>
        public string OutlineColor
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否渐变
        /// </summary>
        public bool IsGradient
        {
            get;
            private set;
        }

        /// <summary>
        /// 渐变起始色
        /// </summary>
        public string GradientStartColor
        {
            get;
            private set;
        }

        /// <summary>
        /// 渐变结束色
        /// </summary>
        public string GradientEndColor
        {
            get;
            private set;
        }

        /// <summary>
        /// 动画类型
        /// </summary>
        public int AnimationType
        {
            get;
            private set;
        }

        /// <summary>
        /// 缩放倍数
        /// </summary>
        public float ScaleMultiplier
        {
            get;
            private set;
        }

        /// <summary>
        /// 持续时间
        /// </summary>
        public float Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 移动距离
        /// </summary>
        public float MoveDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 淡入时间
        /// </summary>
        public float FadeInTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 淡出时间
        /// </summary>
        public float FadeOutTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 特效资源ID
        /// </summary>
        public int EffectId
        {
            get;
            private set;
        }

        /// <summary>
        /// 音效资源ID
        /// </summary>
        public int SoundId
        {
            get;
            private set;
        }

        /// <summary>
        /// 显示优先级
        /// </summary>
        public int Priority
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
            Name = columnStrings[index++];
            FontSize = float.Parse(columnStrings[index++]);
            TextColor = columnStrings[index++];
            OutlineColor = columnStrings[index++];
            IsGradient = bool.Parse(columnStrings[index++]);
            GradientStartColor = columnStrings[index++];
            GradientEndColor = columnStrings[index++];
            AnimationType = int.Parse(columnStrings[index++]);
            ScaleMultiplier = float.Parse(columnStrings[index++]);
            Duration = float.Parse(columnStrings[index++]);
            MoveDistance = float.Parse(columnStrings[index++]);
            FadeInTime = float.Parse(columnStrings[index++]);
            FadeOutTime = float.Parse(columnStrings[index++]);
            EffectId = int.Parse(columnStrings[index++]);
            SoundId = int.Parse(columnStrings[index++]);
            Priority = int.Parse(columnStrings[index++]);

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
                    FontSize = binaryReader.ReadSingle();
                    TextColor = binaryReader.ReadString();
                    OutlineColor = binaryReader.ReadString();
                    IsGradient = binaryReader.ReadBoolean();
                    GradientStartColor = binaryReader.ReadString();
                    GradientEndColor = binaryReader.ReadString();
                    AnimationType = binaryReader.Read7BitEncodedInt32();
                    ScaleMultiplier = binaryReader.ReadSingle();
                    Duration = binaryReader.ReadSingle();
                    MoveDistance = binaryReader.ReadSingle();
                    FadeInTime = binaryReader.ReadSingle();
                    FadeOutTime = binaryReader.ReadSingle();
                    EffectId = binaryReader.Read7BitEncodedInt32();
                    SoundId = binaryReader.Read7BitEncodedInt32();
                    Priority = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
