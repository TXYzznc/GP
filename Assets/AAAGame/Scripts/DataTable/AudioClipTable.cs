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
/// AudioClipTable
/// </summary>
public partial class AudioClipTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 音效ID
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 音效名称
        /// </summary>
        public string AudioName
        {
            get;
            private set;
        }

        /// <summary>
        /// 音效类型
        /// </summary>
        public int AudioType
        {
            get;
            private set;
        }

        /// <summary>
        /// 资源ID
        /// </summary>
        public int ResourcePath
        {
            get;
            private set;
        }

        /// <summary>
        /// 时长
        /// </summary>
        public float Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 音量
        /// </summary>
        public float Volume
        {
            get;
            private set;
        }

        /// <summary>
        /// 音调
        /// </summary>
        public float Pitch
        {
            get;
            private set;
        }

        /// <summary>
        /// 循环
        /// </summary>
        public bool IsLoop
        {
            get;
            private set;
        }

        /// <summary>
        /// 3D定位
        /// </summary>
        public bool Is3D
        {
            get;
            private set;
        }

        /// <summary>
        /// 3D最大距离
        /// </summary>
        public float MaxDistance
        {
            get;
            private set;
        }

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority
        {
            get;
            private set;
        }

        /// <summary>
        /// 淡入时长
        /// </summary>
        public float FadeInTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 淡出时长
        /// </summary>
        public float FadeOutTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 分类标签
        /// </summary>
        public string Tag
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
            AudioName = columnStrings[index++];
            AudioType = int.Parse(columnStrings[index++]);
            ResourcePath = int.Parse(columnStrings[index++]);
            Duration = float.Parse(columnStrings[index++]);
            Volume = float.Parse(columnStrings[index++]);
            Pitch = float.Parse(columnStrings[index++]);
            IsLoop = bool.Parse(columnStrings[index++]);
            Is3D = bool.Parse(columnStrings[index++]);
            MaxDistance = float.Parse(columnStrings[index++]);
            Priority = int.Parse(columnStrings[index++]);
            FadeInTime = float.Parse(columnStrings[index++]);
            FadeOutTime = float.Parse(columnStrings[index++]);
            Tag = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    AudioName = binaryReader.ReadString();
                    AudioType = binaryReader.Read7BitEncodedInt32();
                    ResourcePath = binaryReader.Read7BitEncodedInt32();
                    Duration = binaryReader.ReadSingle();
                    Volume = binaryReader.ReadSingle();
                    Pitch = binaryReader.ReadSingle();
                    IsLoop = binaryReader.ReadBoolean();
                    Is3D = binaryReader.ReadBoolean();
                    MaxDistance = binaryReader.ReadSingle();
                    Priority = binaryReader.Read7BitEncodedInt32();
                    FadeInTime = binaryReader.ReadSingle();
                    FadeOutTime = binaryReader.ReadSingle();
                    Tag = binaryReader.ReadString();
                }
            }

            return true;
        }
}
