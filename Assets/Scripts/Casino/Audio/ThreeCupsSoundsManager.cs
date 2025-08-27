using UnityEngine;

namespace Game.CasinoSystem
{
    [DisallowMultipleComponent]
    public class ThreeCupsSoundsManager : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip wooshClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;

        [Header("Emitter Context")]
        [SerializeField] private Transform emittersContainer;
        [SerializeField] private Transform spawnOrigin;

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 8;

       
        private void Awake()
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m != null && emittersContainer != null)
                m.EnsurePoolPrewarmed(emittersContainer, initialPoolSize);
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

        public void PlayShuffleWoosh(Vector3? worldPosition = null)
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null) return;
            m.PlayOneShot(wooshClip, worldPosition, spawnOrigin, emittersContainer);
        }
    }
}