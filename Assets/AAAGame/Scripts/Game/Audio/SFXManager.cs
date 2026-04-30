using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AAAGame.Audio
{
    public class SFXManager : IDisposable
    {
        private AudioResourceManager _resourceManager;
        private VolumeManager _volumeManager;
        private AudioManager _audioManager;
        private Dictionary<int, AudioSourcePool> _sfxPools = new();
        private List<CancellationTokenSource> _delayedPlayTokens = new();

        public SFXManager(AudioResourceManager resourceManager, VolumeManager volumeManager, AudioManager audioManager)
        {
            _resourceManager = resourceManager;
            _volumeManager = volumeManager;
            _audioManager = audioManager;
        }

        public void Play(SFXPlayRequest request)
        {
            if (request.Delay > 0)
            {
                PlayDelayedAsync(request).Forget();
                return;
            }

            var pool = GetOrCreatePool(request.AudioId);
            if (pool == null) return;

            var source = pool.Get();
            if (source == null) return;

            if (request.Is3D)
            {
                source.transform.position = request.WorldPosition;
                source.spatialBlend = 1f;
            }
            else
            {
                source.spatialBlend = 0f;
            }

            source.volume = _volumeManager.GetEffectiveVolume(AudioTrackType.SFX) * request.VolumeScale;
            source.PlayOneShot(source.clip);

            var duration = source.clip.length;
            ReturnToPoolAsync(pool, source, duration).Forget();

            request.OnComplete?.Invoke();
        }

        public void PlayDelayed(int audioId, float delay, float volumeScale = 1f)
        {
            PlayDelayedAsync(new SFXPlayRequest { AudioId = audioId, VolumeScale = volumeScale, Delay = delay }).Forget();
        }

        private async UniTask PlayDelayedAsync(SFXPlayRequest request)
        {
            await UniTask.Delay((int)(request.Delay * 1000));
            request.Delay = 0;
            Play(request);
        }

        private async UniTask ReturnToPoolAsync(AudioSourcePool pool, AudioSource source, float duration)
        {
            await UniTask.Delay((int)(duration * 1000));
            pool.Return(source);
        }

        public void Stop(int audioId)
        {
            if (_sfxPools.TryGetValue(audioId, out var pool))
            {
                pool.StopAll();
            }
        }

        public void StopAll()
        {
            foreach (var pool in _sfxPools.Values)
            {
                pool.StopAll();
            }
        }

        private AudioSourcePool GetOrCreatePool(int audioId)
        {
            if (!_sfxPools.TryGetValue(audioId, out var pool))
            {
                var clip = _resourceManager.GetAudioClip(audioId);
                if (clip == null)
                {
                    DebugEx.Error("SFXManager", $"❌ SFX 音效加载失败: ID={audioId}");
                    return null;
                }

                pool = new AudioSourcePool($"SFXPool_{audioId}", clip, _audioManager, initialSize: 5);
                _sfxPools[audioId] = pool;
                DebugEx.Log("SFXManager", $"✓ 创建 SFX 对象池: ID={audioId}");
            }
            return pool;
        }

        public void Dispose()
        {
            foreach (var cts in _delayedPlayTokens)
            {
                cts?.Cancel();
                cts?.Dispose();
            }
            _delayedPlayTokens.Clear();

            foreach (var pool in _sfxPools.Values)
            {
                pool.Dispose();
            }
            _sfxPools.Clear();
        }
    }
}
