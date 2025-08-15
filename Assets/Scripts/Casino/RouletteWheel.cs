using System.Collections;
using UnityEngine;

public class RouletteWheel : MonoBehaviour
{
    [SerializeField] private Transform wheel; 
    [SerializeField] private float spinTime = 4f; 
    [SerializeField] private float startSpeed = 500f;
    private const int slots = 37; 

    public IEnumerator Spin(int winningNumber)
    {
        float anglePerSlot = 360f / slots;

        float targetAngle = -winningNumber * anglePerSlot;

        float totalRotation = targetAngle - wheel.eulerAngles.z - (360f * 5);

        float startAngle = wheel.eulerAngles.z;
        float elapsed = 0f;

        while (elapsed < spinTime)
        {
            float t = elapsed / spinTime;
            float currentAngle = Mathf.Lerp(startAngle, startAngle + totalRotation, EaseOutCubic(t));
            wheel.eulerAngles = new Vector3(0, 0, currentAngle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        wheel.eulerAngles = new Vector3(0, 0, startAngle + totalRotation);
    }

    private float EaseOutCubic(float t)
    {
        t--;
        return t * t * t + 1;
    }
}
