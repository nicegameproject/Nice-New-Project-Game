using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CasinoSystem
{
    public class RouletteWheel : MonoBehaviour
    {
        [SerializeField] private Transform[] numberPoints;
        [SerializeField] private Transform arrowPoint;
        [SerializeField] private Transform wheel;
        [SerializeField] private float spinTime = 4f;
        [SerializeField] private int totalSlots = 16;
        [SerializeField] private int extraSpins = 3;

        [Header("Audio")]
        [SerializeField] private RouletteSoundsManager rouletteSoundsManager;

        [Header("High-speed culling")]
        [SerializeField] private bool cullEveryOtherAtHighSpeed = true;
        [Tooltip("Próg w deg/s, powy¿ej którego w³¹czamy culling (np. 2 obroty/s = 720 deg/s).")]
        [SerializeField] private float highSpeedThresholdDegPerSec = 210f;
        [Tooltip("Próg w deg/s, poni¿ej którego wy³¹czamy culling (histereza).")]
        [SerializeField] private float lowSpeedThresholdDegPerSec = 90f;

        private bool highSpeedCullActive = false;

        private AnimationCurve spinCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 0, 0));

        private readonly HashSet<Transform> hitsThisFrame = new HashSet<Transform>();

        public IEnumerator Spin(int result)
        {
            float startRotation = wheel.eulerAngles.z;
            float degreesPerSlot = 360f / totalSlots;

            float targetAngle = result * degreesPerSlot;
            float endRotation = -(extraSpins * 360f + targetAngle);

            float prevWheelRotation = startRotation;

            int[] lastHitAtFrame = new int[numberPoints.Length];
            for (int i = 0; i < lastHitAtFrame.Length; i++) lastHitAtFrame[i] = int.MinValue;

            float elapsed = 0f;
            while (elapsed < spinTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spinTime);
                float curveValue = spinCurve.Evaluate(t);
                float currentRotation = Mathf.Lerp(startRotation, endRotation, curveValue);
                wheel.eulerAngles = new Vector3(0, 0, currentRotation);

                float deltaWheelAngle = currentRotation - prevWheelRotation;

                float dt = Mathf.Max(Time.deltaTime, 1e-6f);
                float angularSpeedDegPerSec = Mathf.Abs(deltaWheelAngle) / dt;

                // Histereza cullingu
                if (cullEveryOtherAtHighSpeed)
                {
                    if (!highSpeedCullActive && angularSpeedDegPerSec >= highSpeedThresholdDegPerSec)
                        SetHighSpeedCulling(true);
                    else if (highSpeedCullActive && angularSpeedDegPerSec <= lowSpeedThresholdDegPerSec)
                        SetHighSpeedCulling(false);
                }

                Vector3 center = wheel.position;
                Vector2 arrowDir = (Vector2)(arrowPoint.position - center);
                float arrowAngle = Mathf.Atan2(arrowDir.y, arrowDir.x) * Mathf.Rad2Deg;

                hitsThisFrame.Clear();
                int frame = Time.frameCount;

                bool playedTickThisFrame = false;

                for (int i = 0; i < numberPoints.Length; i++)
                {
                    if (highSpeedCullActive && (i % 2 == 1))
                        continue;

                    Vector2 pointDir = (Vector2)(numberPoints[i].position - center);
                    float pointAngleCurr = Mathf.Atan2(pointDir.y, pointDir.x) * Mathf.Rad2Deg;

                    float pointAnglePrev = pointAngleCurr - deltaWheelAngle;

                    float relPrev = Mathf.DeltaAngle(pointAnglePrev, arrowAngle);
                    float relCurrUnwrapped = relPrev + deltaWheelAngle;

                    bool crossed =
                        (relPrev <= 0f && relCurrUnwrapped >= 0f) ||
                        (relPrev >= 0f && relCurrUnwrapped <= 0f);

                    if (crossed)
                    {
                        var hitTransform = numberPoints[i];

                        if (lastHitAtFrame[i] == frame) continue;
                        if (!hitsThisFrame.Add(hitTransform)) continue;

                        lastHitAtFrame[i] = frame;

                        if (!playedTickThisFrame && rouletteSoundsManager != null)
                        {
                            rouletteSoundsManager.PlayRuletteWheelTick();
                            playedTickThisFrame = true;
                        }
                    }
                }

                prevWheelRotation = currentRotation;
                yield return null;
            }

            SetHighSpeedCulling(false);

            wheel.eulerAngles = new Vector3(0, 0, endRotation);
        }

        private void SetHighSpeedCulling(bool enabled)
        {
            if (highSpeedCullActive == enabled) return;
            highSpeedCullActive = enabled;

            for (int i = 0; i < numberPoints.Length; i++)
            {
                bool shouldCull = enabled && (i % 2 == 1);
                numberPoints[i].gameObject.SetActive(!shouldCull);
            }
        }
    }
}