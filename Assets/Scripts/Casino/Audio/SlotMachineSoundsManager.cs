using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CasinoSystem
{
    [DisallowMultipleComponent]
    public class SlotMachineSoundsManager : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip reelsLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip singleLineWinClip;
        [SerializeField] private AudioClip[] winSequenceClips;
        [SerializeField] private AudioClip lostClip;
        [SerializeField] private AudioClip pullLeverClip;

        [Header("Emitter Context")]
        [SerializeField] private Transform emittersContainer;
        [SerializeField] private Transform spawnOrigin;

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 8;

        private AudioEmitter activeReelsLoop;
        private Coroutine winSequenceRoutine;

    
        private void Awake()
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m != null && emittersContainer != null)
                m.EnsurePoolPrewarmed(emittersContainer, initialPoolSize);
        }

        public void StartReelsLoop()
        {
            if (activeReelsLoop != null && activeReelsLoop.IsPlaying)
                return;

            var m = MainCasinoSoundsManager.Instance;
            if (m == null || reelsLoopClip == null) return;

            var spawnPos = m.GetSpawnPosition(spawnOrigin, null);
            activeReelsLoop = m.SpawnEmitter(emittersContainer, spawnPos);
            if (activeReelsLoop == null) return;

            activeReelsLoop.OnRequestRelease = e =>
            {
                if (e == activeReelsLoop) activeReelsLoop = null;
                m.ReleaseEmitter(e);
            };

            activeReelsLoop.Play(reelsLoopClip, loop: true, m.DefaultVolume);
        }

        public void StopReelsLoop()
        {
            if (activeReelsLoop == null) return;
            activeReelsLoop.StopAndRelease();
            activeReelsLoop = null;
        }

        public void PlayReelStopAt(Vector3 worldPosition)
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null) return;
            m.PlayOneShot(reelStopClip, worldPosition, spawnOrigin, emittersContainer);
        }

        public void PlayLeverPull()
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null) return;
            m.PlayOneShot(pullLeverClip, null, spawnOrigin, emittersContainer);
        }

        public void PlayLostClip()
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null) return;
            m.PlayOneShot(lostClip, null, spawnOrigin, emittersContainer);
        }

        public Coroutine StartWinSequence(IList<int[]> linesPerPattern, float stepDelay)
        {
            StopWinSequence();
            winSequenceRoutine = StartCoroutine(PlayWinSequenceCo(linesPerPattern, stepDelay));
            return winSequenceRoutine;
        }

        public void StopWinSequence()
        {
            if (winSequenceRoutine != null)
            {
                StopCoroutine(winSequenceRoutine);
                winSequenceRoutine = null;
            }
        }

        public IEnumerator PlayWinSequenceCo(IList<int[]> linesPerPattern, float stepDelay)
        {
            var m = MainCasinoSoundsManager.Instance;
            if (m == null || linesPerPattern == null || linesPerPattern.Count == 0) yield break;

            for (int i = 0; i < linesPerPattern.Count; i++)
            {
                AudioClip clip = (winSequenceClips != null && winSequenceClips.Length > 0)
                    ? winSequenceClips[Mathf.Min(i, winSequenceClips.Length - 1)]
                    : singleLineWinClip;

                m.PlayOneShot(clip, null, spawnOrigin, emittersContainer);

                yield return new WaitForSeconds(stepDelay);
            }

            winSequenceRoutine = null;
        }
    }
}