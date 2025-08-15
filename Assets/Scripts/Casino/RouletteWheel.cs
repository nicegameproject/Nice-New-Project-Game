using System.Collections;
using UnityEngine;

public class RouletteWheel : MonoBehaviour
{
    [SerializeField] private Transform wheel;
    [SerializeField] private float spinTime = 4f;
    [SerializeField] private int totalSlots = 16;
    [SerializeField] private int extraSpins = 3;
    private AnimationCurve spinCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 0, 0));

    public IEnumerator Spin(int result)
    {
        float startRotation = wheel.eulerAngles.z;
        float degreesPerSlot = 360f / totalSlots;

        float targetAngle = result * degreesPerSlot;

        float endRotation = -(extraSpins * 360f + targetAngle);

        float elapsed = 0f;
        while (elapsed < spinTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spinTime);
            float curveValue = spinCurve.Evaluate(t);
            float currentRotation = Mathf.Lerp(startRotation, endRotation, curveValue);
            wheel.eulerAngles = new Vector3(0, 0, currentRotation);
            yield return null;
        }

        wheel.eulerAngles = new Vector3(0, 0, endRotation);
    }
}
