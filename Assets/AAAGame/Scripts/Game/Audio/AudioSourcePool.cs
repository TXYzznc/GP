using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace AAAGame.Audio
{
    public class AudioSourcePool : IDisposable
    {
        private string _poolName;
        private AudioClip _clip;
        private Queue<AudioSource> _available = new();
        private HashSet<AudioSource> _inUse = new();
        private Transform _parentTransform;

        public AudioSourcePool(string poolName, AudioClip clip, AudioManager audioManager, int initialSize = 5)
        {
            _poolName = poolName;
            _clip = clip;

            var parent = new GameObject(_poolName).transform;
            parent.SetParent(audioManager.transform, false);
            _parentTransform = parent;

            for (int i = 0; i < initialSize; i++)
            {
                var source = CreateAudioSource();
                _available.Enqueue(source);
            }
        }

        public AudioSource Get()
        {
            AudioSource source;
            if (_available.Count > 0)
            {
                source = _available.Dequeue();
            }
            else
            {
                source = CreateAudioSource();
            }

            source.clip = _clip;
            source.gameObject.SetActive(true);
            _inUse.Add(source);
            return source;
        }

        public void Return(AudioSource source)
        {
            if (_inUse.Remove(source))
            {
                source.Stop();
                source.gameObject.SetActive(false);
                _available.Enqueue(source);
            }
        }

        public void StopAll()
        {
            foreach (var source in _inUse)
            {
                source.Stop();
            }
        }

        private AudioSource CreateAudioSource()
        {
            var go = new GameObject($"SFXSource_{_poolName}_{_available.Count + _inUse.Count}");
            go.transform.SetParent(_parentTransform, false);

            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.priority = 64;
            go.SetActive(false);
            return source;
        }

        public void Dispose()
        {
            foreach (var source in _available)
            {
                UnityEngine.Object.Destroy(source.gameObject);
            }
            foreach (var source in _inUse)
            {
                UnityEngine.Object.Destroy(source.gameObject);
            }
            UnityEngine.Object.Destroy(_parentTransform.gameObject);
            _available.Clear();
            _inUse.Clear();
        }
    }
}
