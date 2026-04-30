using UnityEngine.UI;
using GameFramework.UI;

namespace AAAGame.Audio
{
    /// <summary>
    /// UI 元素的音效扩展方法
    /// </summary>
    public static class AudioUIExtensions
    {
        /// <summary>
        /// 为按钮添加点击音效
        /// </summary>
        public static void AddAudioClick(this Button button, int sfxId = AudioClipIds.SFX_ButtonClick)
        {
            if (button == null) return;
            button.onClick.AddListener(() => AudioManager.Instance?.PlaySFX(sfxId));
        }

        /// <summary>
        /// 为 Slider 添加拖拽音效
        /// </summary>
        public static void AddAudioSlider(this Slider slider, int sfxId = AudioClipIds.SFX_ButtonClick)
        {
            if (slider == null) return;
            slider.onValueChanged.AddListener(_ => AudioManager.Instance?.PlaySFX(sfxId));
        }

        /// <summary>
        /// 为 Toggle 添加切换音效
        /// </summary>
        public static void AddAudioToggle(this Toggle toggle, int sfxId = AudioClipIds.SFX_ButtonClick)
        {
            if (toggle == null) return;
            toggle.onValueChanged.AddListener(_ => AudioManager.Instance?.PlaySFX(sfxId));
        }
    }

    /// <summary>
    /// UIForm 音效配置
    /// </summary>
    public class UIAudioConfig
    {
        public int OpenSFXId = AudioClipIds.SFX_UIOpen;
        public int CloseSFXId = AudioClipIds.SFX_UIClose;
    }
}
