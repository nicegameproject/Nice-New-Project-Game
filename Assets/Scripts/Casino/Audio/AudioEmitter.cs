using System.Collections;
using UnityEngine;

namespace Game.CasinoSystem
{
    [DisallowMultipleComponent]
    public class AudioEmitter : MonoBehaviour
    {
        public System.Action<AudioEmitter> OnRequestRelease;

        private AudioSource source;
        private Coroutine autoReleaseCo;

        public bool IsPlaying => source != null && source.isPlaying;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        internal void Configure(SlotMachineSoundsManager owner)
        {
            if (source == null) source = GetComponent<AudioSource>();
          
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.1f;
            source.maxDistance = 20f;
        }

        public void Play(AudioClip clip, bool loop, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) { RequestRelease(); return; }

            if (autoReleaseCo != null)
            {
                StopCoroutine(autoReleaseCo);
                autoReleaseCo = null;
            }

            source.clip = clip;
            source.loop = loop;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, -3f, 3f);
            source.Play();

            if (!loop)
                autoReleaseCo = StartCoroutine(AutoReleaseWhenDone());
        }

        public void StopAndRelease()
        {
            if (autoReleaseCo != null)
            {
                StopCoroutine(autoReleaseCo);
                autoReleaseCo = null;
            }
            if (source != null && source.isPlaying) source.Stop();
            RequestRelease();
        }

        private IEnumerator AutoReleaseWhenDone()
        {
            while (source != null && source.isPlaying)
                yield return null;

            RequestRelease();
        }

        private void RequestRelease()
        {
            OnRequestRelease?.Invoke(this);
        }
    }
}