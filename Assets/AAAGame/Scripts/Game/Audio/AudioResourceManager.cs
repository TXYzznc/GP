using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace AAAGame.Audio
{
    public class AudioResourceManager : IDisposable
    {
        private Dictionary<int, AudioClipTable> _audioConfigs = new();
        private Dictionary<int, AudioClip> _clipCache = new();
        private Dictionary<string, List<int>> _tagIndexes = new();

        public void InitializeConfigs(AudioClipTable[] configs)
        {
            foreach (var config in configs)
            {
                _audioConfigs[config.Id] = config;

                // 构建 Tag 索引
                if (!string.IsNullOrEmpty(config.Tag))
                {
                    if (!_tagIndexes.TryGetValue(config.Tag, out var ids))
                    {
                        ids = new List<int>();
                        _tagIndexes[config.Tag] = ids;
                    }
                    ids.Add(config.Id);
                }
            }
            DebugEx.Log("AudioResourceManager", $"✓ 加载 {configs.Length} 个音效配置");
        }

        public AudioClip GetAudioClip(int audioId)
        {
            if (_clipCache.TryGetValue(audioId, out var clip))
            {
                return clip;
            }

            if (!_audioConfigs.TryGetValue(audioId, out var config))
            {
                DebugEx.Error("AudioResourceManager", $"❌ 音效配置不存在: ID={audioId}");
                return null;
            }

            var resourceConfig = GF.DataTable.GetDataTable<ResourceConfigTable>()?.GetDataRow(config.ResourcePath);
            if (resourceConfig == null)
            {
                DebugEx.Error("AudioResourceManager", $"❌ 音效资源配置不存在: ResourceID={config.ResourcePath}");
                return null;
            }

            clip = Resources.Load<AudioClip>(resourceConfig.Path);
            if (clip == null)
            {
                DebugEx.Error("AudioResourceManager", $"❌ 音效文件加载失败: {resourceConfig.Path}");
                return null;
            }

            // BGM 不缓存（流式加载），SFX 缓存
            if (config.AudioType != 1) // 1 = BGM
            {
                _clipCache[audioId] = clip;
            }

            return clip;
        }

        public void PreloadAudioClip(int audioId)
        {
            if (!_clipCache.ContainsKey(audioId))
            {
                var clip = GetAudioClip(audioId);
                if (clip != null && _audioConfigs[audioId].AudioType != 1)
                {
                    _clipCache[audioId] = clip;
                }
            }
        }

        public void UnloadAudioClip(int audioId)
        {
            if (_clipCache.TryGetValue(audioId, out var clip))
            {
                Resources.UnloadAsset(clip);
                _clipCache.Remove(audioId);
            }
        }

        public void UnloadAllAudioClips()
        {
            var keys = new List<int>(_clipCache.Keys);
            foreach (var key in keys)
            {
                UnloadAudioClip(key);
            }
        }

        public AudioClipTable GetConfig(int audioId)
        {
            return _audioConfigs.TryGetValue(audioId, out var config) ? config : null;
        }

        public List<AudioClipTable> GetConfigsByTag(string tag)
        {
            var result = new List<AudioClipTable>();
            if (_tagIndexes.TryGetValue(tag, out var ids))
            {
                foreach (var id in ids)
                {
                    if (_audioConfigs.TryGetValue(id, out var config))
                    {
                        result.Add(config);
                    }
                }
            }
            return result;
        }

        public void Dispose()
        {
            UnloadAllAudioClips();
            _audioConfigs.Clear();
            _tagIndexes.Clear();
        }
    }

    public enum AudioClipType
    {
        BGM = 1,
        SFX = 2,
        Ambient = 3,
        Voice = 4
    }
}
