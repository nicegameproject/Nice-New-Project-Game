using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    [Tooltip("Kolejne punkty patrolu; u¿yj dzieci jako punktów lub podaj rêcznie.")]
    public Transform[] Waypoints;

    public int Count => Waypoints != null ? Waypoints.Length : 0;

    public Transform GetWaypoint(int index)
    {
        if (Waypoints == null || Waypoints.Length == 0) return null;
        index = Mathf.Clamp(index, 0, Waypoints.Length - 1);
        return Waypoints[index];
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Waypoints == null || Waypoints.Length == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < Waypoints.Length; i++)
        {
            if (Waypoints[i] == null) continue;
            Gizmos.DrawSphere(Waypoints[i].position, 0.2f);
            int j = (i + 1) % Waypoints.Length;
            if (Waypoints[j] != null)
            {
                Gizmos.DrawLine(Waypoints[i].position, Waypoints[j].position);
            }
        }
    }
#endif
}