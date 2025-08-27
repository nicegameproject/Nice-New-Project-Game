using System.Collections.Generic;
using UnityEngine;

namespace Game.CasinoSystem
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class MainCasinoSoundsManager : MonoBehaviour
    {
        public static MainCasinoSoundsManager Instance { get; private set; }

        [Header("Lifecycle")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Emitter Pool")]
        [SerializeField] private GameObject emitterPrefab;
        [SerializeField] private int maxPoolSize = 32;
        [SerializeField] private bool expandIfNeeded = true;

        [Header("Audio Defaults")]
        [SerializeField] private float defaultVolume = 1f;
        public float DefaultVolume => defaultVolume;

        private readonly Dictionary<Transform, Queue<AudioEmitter>> pools = new Dictionary<Transform, Queue<AudioEmitter>>();
        private readonly Dictionary<Transform, HashSet<AudioEmitter>> actives = new Dictionary<Transform, HashSet<AudioEmitter>>();
        private readonly Dictionary<AudioEmitter, Transform> emitterOwner = new Dictionary<AudioEmitter, Transform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsurePoolPrewarmed(Transform emittersContainer, int count)
        {
            if (emittersContainer == null || count <= 0) return;

            var pool = GetPool(emittersContainer);
            var activeSet = GetActiveSet(emittersContainer);

            int total = pool.Count + activeSet.Count;
            while (total < count)
            {
                var emitter = CreateEmitterForContainer(emittersContainer);
                pool.Enqueue(emitter);
                total++;
            }
        }

        public void PlayOneShot(
            AudioClip clip,
            Vector3? worldPosition,
            Transform spawnOrigin,
            Transform emittersContainer)
        {
            if (clip == null) return;

            var pos = GetSpawnPosition(spawnOrigin, worldPosition);
            var emitter = SpawnEmitter(emittersContainer, pos);
            if (emitter == null) return;

            emitter.Play(clip, loop: false, defaultVolume);
        }

        public Vector3 GetSpawnPosition(Transform spawnOrigin, Vector3? requested)
        {
            if (requested.HasValue) return requested.Value;
            Vector3 origin = spawnOrigin != null ? spawnOrigin.position : transform.position;
            return origin;
        }

        public AudioEmitter SpawnEmitter(Transform emittersContainer, Vector3 worldPos)
        {
            if (emittersContainer == null) return null;

            var emitter = GetFromPool(emittersContainer);
            if (emitter == null) return null;

            emitterOwner[emitter] = emittersContainer;

            var activeSet = GetActiveSet(emittersContainer);
            activeSet.Add(emitter);

            var go = emitter.gameObject;
            if (go.transform.parent != emittersContainer)
                go.transform.SetParent(emittersContainer, true);

            go.transform.position = worldPos;
            go.SetActive(true);
            return emitter;
        }

        public void ReleaseEmitter(AudioEmitter emitter)
        {
            if (emitter == null) return;

            Transform container;
            if (!emitterOwner.TryGetValue(emitter, out container) || container == null)
            {
                container = emitter.transform.parent;
            }

            var activeSet = GetActiveSet(container);
            if (activeSet.Contains(emitter)) activeSet.Remove(emitter);

            var go = emitter.gameObject;
            go.SetActive(false);

            var pool = GetPool(container);
            pool.Enqueue(emitter);
        }


        private Queue<AudioEmitter> GetPool(Transform container)
        {
            Queue<AudioEmitter> q;
            if (!pools.TryGetValue(container, out q))
            {
                q = new Queue<AudioEmitter>();
                pools[container] = q;
            }
            return q;
        }

        private HashSet<AudioEmitter> GetActiveSet(Transform container)
        {
            HashSet<AudioEmitter> s;
            if (!actives.TryGetValue(container, out s))
            {
                s = new HashSet<AudioEmitter>();
                actives[container] = s;
            }
            return s;
        }

        private AudioEmitter GetFromPool(Transform container)
        {
            var pool = GetPool(container);
            if (pool.Count > 0) return pool.Dequeue();

            var activeSet = GetActiveSet(container);
            int total = pool.Count + activeSet.Count;
            if (total < maxPoolSize || expandIfNeeded)
                return CreateEmitterForContainer(container);

            return null;
        }

        private AudioEmitter CreateEmitterForContainer(Transform container)
        {
            GameObject go;
            if (emitterPrefab != null)
            {
                go = Instantiate(emitterPrefab);
            }
            else
            {
                go = new GameObject("AudioEmitter");
                go.AddComponent<AudioSource>();
            }

            if (container != null)
                go.transform.SetParent(container, false);

            var emitter = go.GetComponent<AudioEmitter>();
            if (emitter == null) emitter = go.AddComponent<AudioEmitter>();

            go.SetActive(false);

            emitter.OnRequestRelease = ReleaseEmitter;

            emitterOwner[emitter] = container;

            return emitter;
        }
    }
}