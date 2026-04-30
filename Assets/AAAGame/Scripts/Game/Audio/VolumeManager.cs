using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace AAAGame.Audio
{
    public class VolumeManager : IDisposable
    {
        private const string VOLUME_PREFS_PREFIX = "Audio_";
        private float _masterVolume = 1f;
        private bool _isMuted = false;

        private Dictionary<AudioTrackType, float> _trackVolumes = new()
        {
            { AudioTrackType.BGM, 0.8f },
            { AudioTrackType.SFX, 0.9f },
            { AudioTrackType.Ambient, 0.6f },
            { AudioTrackType.Voice, 1f }
        };

        public bool IsMuted => _isMuted;

        public VolumeManager()
        {
            LoadVolumeSettings();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(VOLUME_PREFS_PREFIX + "Master", _masterVolume);
            DebugEx.Log("VolumeManager", $"✓ 设置主音量: {_masterVolume:F2}");
        }

        public void SetTrackVolume(AudioTrackType trackType, float volume)
        {
            _trackVolumes[trackType] = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(VOLUME_PREFS_PREFIX + trackType, _trackVolumes[trackType]);
            DebugEx.Log("VolumeManager", $"✓ 设置 {trackType} 音量: {_trackVolumes[trackType]:F2}");
        }

        public float GetTrackVolume(AudioTrackType trackType)
        {
            return _trackVolumes.TryGetValue(trackType, out var volume) ? volume : 1f;
        }

        public float GetEffectiveVolume(AudioTrackType trackType)
        {
            if (_isMuted) return 0;
            return _masterVolume * GetTrackVolume(trackType);
        }

        public void SetMute(bool isMuted)
        {
            _isMuted = isMuted;
            PlayerPrefs.SetInt(VOLUME_PREFS_PREFIX + "Muted", isMuted ? 1 : 0);
            DebugEx.Log("VolumeManager", $"✓ {(_isMuted ? "启用" : "取消")}全局静音");
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(VOLUME_PREFS_PREFIX + "Master", 1f);
            foreach (var trackType in new[] { AudioTrackType.BGM, AudioTrackType.SFX, AudioTrackType.Ambient, AudioTrackType.Voice })
            {
                _trackVolumes[trackType] = PlayerPrefs.GetFloat(VOLUME_PREFS_PREFIX + trackType, _trackVolumes[trackType]);
            }
            _isMuted = PlayerPrefs.GetInt(VOLUME_PREFS_PREFIX + "Muted", 0) == 1;
            DebugEx.Log("VolumeManager", "✓ 加载音量设置完成");
        }

        public void Dispose()
        {
            PlayerPrefs.Save();
        }
    }
}
