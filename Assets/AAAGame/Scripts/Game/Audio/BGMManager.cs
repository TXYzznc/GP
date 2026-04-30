using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AAAGame.Audio
{
    public class BGMManager : IDisposable
    {
        private AudioSource _bgmSource;
        private AudioResourceManager _resourceManager;
        private VolumeManager _volumeManager;
        private AudioManager _audioManager;
        private Queue<BGMPlayRequest> _bgmQueue = new();
        private CancellationTokenSource _fadeCts;

        public BGMManager(AudioResourceManager resourceManager, VolumeManager volumeManager, AudioManager audioManager)
        {
            _resourceManager = resourceManager;
            _volumeManager = volumeManager;
            _audioManager = audioManager;
            _bgmSource = CreateAudioSource("BGM", isLoop: true, is3D: false);
        }

        public void Play(BGMPlayRequest request)
        {
            if (request.IsImmediate)
            {
                PlayImmediate(request);
            }
            else
            {
                _bgmQueue.Enqueue(request);
                ProcessQueueAsync().Forget();
            }
        }

        private void PlayImmediate(BGMPlayRequest request)
        {
            _fadeCts?.Cancel();
            _fadeCts = new CancellationTokenSource();

            var clip = _resourceManager.GetAudioClip(request.AudioId);
            if (clip == null)
            {
                DebugEx.Error("BGMManager", $"❌ BGM 音效加载失败: ID={request.AudioId}");
                return;
            }

            _bgmSource.clip = clip;
            _bgmSource.volume = _volumeManager.GetEffectiveVolume(AudioTrackType.BGM);
            _bgmSource.Play();

            DebugEx.Log("BGMManager", $"✓ 立即播放 BGM: {clip.name}");
            FadeInAsync(request.FadeInTime, _fadeCts.Token).Forget();
        }

        private async UniTask ProcessQueueAsync()
        {
            while (_bgmQueue.Count > 0)
            {
                var request = _bgmQueue.Dequeue();
                await QueuePlayAsync(request);
            }
        }

        private async UniTask QueuePlayAsync(BGMPlayRequest request)
        {
            if (_bgmSource.isPlaying)
            {
                await FadeOutAsync(request.FadeOutTime);
            }

            var clip = _resourceManager.GetAudioClip(request.AudioId);
            if (clip == null)
            {
                DebugEx.Error("BGMManager", $"❌ BGM 音效加载失败: ID={request.AudioId}");
                return;
            }

            _bgmSource.clip = clip;
            _bgmSource.Play();
            DebugEx.Log("BGMManager", $"✓ 切换 BGM: {clip.name}");

            await FadeInAsync(request.FadeInTime);

            request.OnComplete?.Invoke();
        }

        private async UniTask FadeInAsync(float duration, CancellationToken cancellationToken = default)
        {
            float elapsed = 0;
            float targetVolume = _volumeManager.GetEffectiveVolume(AudioTrackType.BGM);
            _bgmSource.volume = 0;

            while (elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested) return;
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(0, targetVolume, elapsed / duration);
                await UniTask.Yield(cancellationToken);
            }

            _bgmSource.volume = targetVolume;
        }

        private async UniTask FadeOutAsync(float duration)
        {
            float elapsed = 0;
            float startVolume = _bgmSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                await UniTask.Yield();
            }

            _bgmSource.volume = 0;
            _bgmSource.Stop();
        }

        public void Stop(float fadeOutTime = 0.5f)
        {
            _fadeCts?.Cancel();
            _fadeCts = new CancellationTokenSource();
            FadeOutAsync(fadeOutTime).Forget();
        }

        public void Pause()
        {
            _bgmSource?.Pause();
        }

        public void Resume()
        {
            _bgmSource?.UnPause();
        }

        private AudioSource CreateAudioSource(string name, bool isLoop, bool is3D)
        {
            var go = new GameObject($"AudioSource_{name}");
            go.transform.SetParent(_audioManager.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = isLoop;
            source.spatialBlend = is3D ? 1f : 0f;
            source.priority = 128;
            return source;
        }

        public void Dispose()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            if (_bgmSource != null)
            {
                UnityEngine.Object.Destroy(_bgmSource.gameObject);
            }
            _bgmQueue.Clear();
        }
    }
}
