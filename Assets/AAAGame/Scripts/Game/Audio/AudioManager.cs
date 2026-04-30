using GameFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAAGame.Audio
{
    /// <summary>
    /// 音频系统总管理器，负责 BGM、SFX、音量、资源的统一管理
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        private BGMManager _bgmManager;
        private SFXManager _sfxManager;
        private VolumeManager _volumeManager;
        private AudioResourceManager _resourceManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            DebugEx.Log("AudioManager", "初始化音频系统...");
            try
            {
                _resourceManager = new AudioResourceManager();
                _volumeManager = new VolumeManager();
                _bgmManager = new BGMManager(_resourceManager, _volumeManager, this);
                _sfxManager = new SFXManager(_resourceManager, _volumeManager, this);

                LoadAudioConfigs();
                DebugEx.Success("AudioManager", "音频系统初始化完成");
            }
            catch (Exception ex)
            {
                DebugEx.Error("AudioManager", $"音频系统初始化失败: {ex.Message}");
            }
        }

        private void LoadAudioConfigs()
        {
            try
            {
                var audioTable = GF.DataTable.GetDataTable<AudioClipTable>();
                if (audioTable == null)
                {
                    DebugEx.Error("AudioManager", "❌ 无法加载 AudioClipTable 配置表");
                    return;
                }

                _resourceManager.InitializeConfigs(audioTable.GetAllDataRows());
                DebugEx.Log("AudioManager", $"✓ 加载配置表完成，共 {audioTable.Count} 个音效");
            }
            catch (Exception ex)
            {
                DebugEx.Error("AudioManager", $"加载配置表失败: {ex.Message}");
            }
        }

        // ==================== BGM API ====================

        public void PlayBGM(int audioId, float fadeInTime = 0.5f)
        {
            _bgmManager?.Play(new BGMPlayRequest
            {
                AudioId = audioId,
                FadeInTime = fadeInTime
            });
        }

        public void StopBGM(float fadeOutTime = 0.5f)
        {
            _bgmManager?.Stop(fadeOutTime);
        }

        public void PauseBGM()
        {
            _bgmManager?.Pause();
        }

        public void ResumeBGM()
        {
            _bgmManager?.Resume();
        }

        // ==================== SFX API ====================

        public void PlaySFX(int audioId, float volumeScale = 1f)
        {
            _sfxManager?.Play(new SFXPlayRequest
            {
                AudioId = audioId,
                VolumeScale = volumeScale
            });
        }

        public void PlaySFX3D(int audioId, Vector3 worldPos, float volumeScale = 1f)
        {
            _sfxManager?.Play(new SFXPlayRequest
            {
                AudioId = audioId,
                WorldPosition = worldPos,
                VolumeScale = volumeScale,
                Is3D = true
            });
        }

        public void PlaySFXDelayed(int audioId, float delay, float volumeScale = 1f)
        {
            _sfxManager?.PlayDelayed(audioId, delay, volumeScale);
        }

        public void StopSFX(int audioId)
        {
            _sfxManager?.Stop(audioId);
        }

        public void StopAllSFX()
        {
            _sfxManager?.StopAll();
        }

        // ==================== 音量控制 API ====================

        public void SetMasterVolume(float volume)
        {
            _volumeManager?.SetMasterVolume(volume);
        }

        public void SetTrackVolume(AudioTrackType trackType, float volume)
        {
            _volumeManager?.SetTrackVolume(trackType, volume);
        }

        public float GetTrackVolume(AudioTrackType trackType)
        {
            return _volumeManager?.GetTrackVolume(trackType) ?? 1f;
        }

        public void SetMute(bool isMuted)
        {
            _volumeManager?.SetMute(isMuted);
        }

        public bool IsMuted => _volumeManager?.IsMuted ?? false;

        // ==================== 资源管理 API ====================

        public void PreloadAudioClip(int audioId)
        {
            _resourceManager?.PreloadAudioClip(audioId);
        }

        public void UnloadAudioClip(int audioId)
        {
            _resourceManager?.UnloadAudioClip(audioId);
        }

        public void UnloadAllAudioClips()
        {
            _resourceManager?.UnloadAllAudioClips();
        }

        private void OnDestroy()
        {
            _bgmManager?.Dispose();
            _sfxManager?.Dispose();
            _volumeManager?.Dispose();
            _resourceManager?.Dispose();
        }
    }

    // ==================== 数据结构 ====================

    public enum AudioTrackType
    {
        BGM = 0,      // 背景音乐
        SFX = 1,      // 音效
        Ambient = 2,  // 环境音
        Voice = 3     // 语音
    }

    public class BGMPlayRequest
    {
        public int AudioId;
        public float FadeInTime = 0.5f;
        public float FadeOutTime = 0.5f;
        public bool IsImmediate = false;
        public Action OnComplete;
    }

    public class SFXPlayRequest
    {
        public int AudioId;
        public Vector3 WorldPosition = Vector3.zero;
        public float VolumeScale = 1f;
        public float Delay = 0f;
        public bool Is3D = false;
        public Action OnComplete;
    }
}
