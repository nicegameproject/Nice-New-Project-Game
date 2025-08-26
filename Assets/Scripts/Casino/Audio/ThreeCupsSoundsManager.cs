using System.Collections.Generic;
using UnityEngine;

namespace Game.CasinoSystem
{
    [DisallowMultipleComponent]
    public class ThreeCupsSoundsManager : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip wooshClip;

        [Header("Emitter Pool")]
        [SerializeField] private GameObject emitterPrefab;
        [SerializeField] private Transform emittersContainer;
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] private float spawnRadius = 0.25f;
        [SerializeField] private int initialPoolSize = 8;
        [SerializeField] private int maxPoolSize = 32;
        [SerializeField] private bool expandIfNeeded = true;

        [Header("Audio Defaults")]
        [SerializeField] private float defaultVolume = 1f;
        [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);

        private readonly Queue<AudioEmitter> pool = new Queue<AudioEmitter>();
        private readonly HashSet<AudioEmitter> active = new HashSet<AudioEmitter>();

        private void Awake()
        {
            for (int i = 0; i < Mathf.Max(0, initialPoolSize); i++)
                pool.Enqueue(CreateEmitter());
        }

        public void PlayShuffleWoosh(Vector3? worldPosition = null)
        {
            if (wooshClip == null) return;

            var emitter = SpawnEmitter(GetSpawnPosition(worldPosition));
            if (emitter == null) return;

            float pitch = Mathf.Clamp(Random.Range(pitchRange.x, pitchRange.y), 0.01f, 3f);
            emitter.Play(wooshClip, loop: false, defaultVolume, pitch);
        }

        public void PlayOneShot(AudioClip clip, Vector3? worldPosition = null, float pitch = 1f)
        {
            if (clip == null) return;

            var emitter = SpawnEmitter(GetSpawnPosition(worldPosition));
            if (emitter == null) return;

            emitter.Play(clip, loop: false, defaultVolume, pitch);
        }

        private Vector3 GetSpawnPosition(Vector3? requested)
        {
            if (requested.HasValue) return requested.Value;

            Vector3 origin = spawnOrigin != null ? spawnOrigin.position : transform.position;
            return origin + (spawnRadius > 0f ? Random.insideUnitSphere * spawnRadius : Vector3.zero);
        }

        private AudioEmitter SpawnEmitter(Vector3 worldPos)
        {
            var emitter = GetFromPool();
            if (emitter == null) return null;

            active.Add(emitter);
            var go = emitter.gameObject;
            if (emittersContainer != null) go.transform.SetParent(emittersContainer, true);

            go.transform.position = worldPos;
            go.SetActive(true);
            return emitter;
        }

        private AudioEmitter GetFromPool()
        {
            if (pool.Count > 0) return pool.Dequeue();

            int total = pool.Count + active.Count;
            if (total < maxPoolSize || expandIfNeeded)
                return CreateEmitter();

            return null;
        }

        private AudioEmitter CreateEmitter()
        {
            GameObject go;
            if (emitterPrefab != null)
            {
                go = Instantiate(emitterPrefab);
                if (emittersContainer != null) go.transform.SetParent(emittersContainer, false);
            }
            else
            {
                go = new GameObject("AudioEmitter");
                if (emittersContainer != null) go.transform.SetParent(emittersContainer, false);
                go.AddComponent<AudioSource>();
            }

            var emitter = go.GetComponent<AudioEmitter>();
            if (emitter == null) emitter = go.AddComponent<AudioEmitter>();

            go.SetActive(false);
            emitter.OnRequestRelease = ReleaseEmitter;
            return emitter;
        }

        private void ReleaseEmitter(AudioEmitter emitter)
        {
            if (emitter == null) return;

            if (active.Contains(emitter)) active.Remove(emitter);

            var go = emitter.gameObject;
            go.SetActive(false);
            pool.Enqueue(emitter);
        }
    }
}