using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts.Managers
{
    public class PersistentAudioSourceManager : MonoBehaviour
    {
        [SerializeField] private float objectLifetimeSeconds = 60f;

        private static PersistentAudioSourceManager _persistentAudioSourceManager;

        private Dictionary<AudioSource, Coroutine> _audioCoroutines = new();

        private void Awake()
        {
            if (_persistentAudioSourceManager != null && _persistentAudioSourceManager != this)
            {
                Destroy(gameObject);
                return;
            }

            _persistentAudioSourceManager = this;
            DontDestroyOnLoad(gameObject);
        }

        public static PersistentAudioSourceManager GetInstance()
        {
            return _persistentAudioSourceManager;
        }

        public void PlaySoundBasedOnRefencedSource(AudioSource referencedAudioSource)
        {
            var freeSource = _audioCoroutines.Keys.FirstOrDefault(item => !item.isPlaying);

            if  (!freeSource)
            {
                freeSource = Instantiate(referencedAudioSource, transform);
                _audioCoroutines.Add(freeSource, null);
            }
            else
            {
                var coroutine = _audioCoroutines[freeSource];
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }

                freeSource.transform.position = referencedAudioSource.transform.position;
                ConfigureAudioSource(freeSource, referencedAudioSource);
            }

            freeSource.Play();

            var newCoroutine = StartCoroutine(ReleaseAudioSourceAfterPlayAndExpiration(freeSource));
            _audioCoroutines[freeSource] = newCoroutine;
        }

        private void ConfigureAudioSource(AudioSource target, AudioSource reference)
        {
            target.clip = reference.clip;
            target.playOnAwake = reference.playOnAwake;
            target.loop = reference.loop;
            target.volume = reference.volume;
            target.pitch = reference.pitch;
            target.spatialBlend = reference.spatialBlend;
            target.maxDistance = reference.maxDistance;
            target.minDistance = reference.minDistance;
            target.SetCustomCurve(AudioSourceCurveType.CustomRolloff, reference.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        }

        private IEnumerator ReleaseAudioSourceAfterPlayAndExpiration(AudioSource source)
        {
            yield return new WaitForSeconds(source.clip.length);
            yield return new WaitForSeconds(objectLifetimeSeconds);

            _audioCoroutines.Remove(source);
            Destroy(source.gameObject);
        }
    }
}