using UnityEngine;

namespace Game.CasinoSystem
{
    [DisallowMultipleComponent]
    public class RouletteSoundsManager : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip ruletteWheelTick;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;

        [Header("Emitter Context")]
        [SerializeField] private Transform emittersContainer;
        [SerializeField] private Transform spawnOrigin;

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 8;

        private AudioEmitter tickEmitter;

        private void Awake()
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m != null && emittersContainer != null)
                m.EnsurePoolPrewarmed(emittersContainer, initialPoolSize);
        }

        public void PlayRuletteWheelTick(Vector3? worldPosition = null)
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null || ruletteWheelTick == null) return;

            if (tickEmitter != null)
            {
                tickEmitter.StopAndRelease();
                tickEmitter = null;
            }

            var spawnPos = m.GetSpawnPosition(spawnOrigin, worldPosition);
            tickEmitter = m.SpawnEmitter(emittersContainer, spawnPos);
            if (tickEmitter == null) return;

            tickEmitter.OnRequestRelease = e =>
            {
                if (e == tickEmitter) tickEmitter = null;
                MainCasinoSoundsManager.Instance.ReleaseEmitter(e);
            };

            tickEmitter.Play(ruletteWheelTick, loop: false, m.DefaultVolume);
        }

        public void PlayWin(Vector3? worldPosition = null)
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null) return;
            m.PlayOneShot(winClip, worldPosition, spawnOrigin, emittersContainer);
        }

        public void PlayLose(Vector3? worldPosition = null)
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null) return;
            m.PlayOneShot(loseClip, worldPosition, spawnOrigin, emittersContainer);
        }
    }
}