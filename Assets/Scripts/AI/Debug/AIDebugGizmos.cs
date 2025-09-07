using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class AIDebugGizmos : MonoBehaviour
{
    private AIController _ai;

    [Range(8, 128)] public int CircleSegments = 48;
    public float CircleYOffset = 0.01f;

    private static readonly Color ViewColor = new Color(1f, 0.5f, 0f, 1f);
    private static readonly Color ViewArcColorTransparent = new Color(1f, 0.5f, 0f, 0.35f);

    void Awake()
    {
        _ai = GetComponent<AIController>();
    }

    void OnDrawGizmos()
    {
        if (_ai == null)
            _ai = GetComponent<AIController>();

        if (_ai == null || _ai.Config == null || !_ai.Config.DrawGizmos) return;

        var cfg = _ai.Config;
        var origin = transform.position + Vector3.up * CircleYOffset;


        if (_ai.Blackboard != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_ai.Blackboard.CurrentDestination, 0.2f);


            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_ai.Blackboard.LastKnownTargetPos, 0.2f);
        }

        // Line to target
        if (_ai.Blackboard != null && _ai.Blackboard.Target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.6f,
                            _ai.Blackboard.Target.position + Vector3.up * 1.6f);
        }

        // Search radius
        Gizmos.color = Color.yellow;
        DrawCircleXZ(origin, cfg.SearchRadius, CircleSegments);

#if UNITY_EDITOR
        Handles.color = new Color(1f, 1f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.SearchRadius);
#endif

        // View distance circle
        Gizmos.color = ViewColor;
        DrawCircleXZ(origin, cfg.ViewDistance, CircleSegments);

        // Field of View (wedge)
        DrawFOV(origin, cfg.ViewDistance, cfg.ViewAngle);

#if UNITY_EDITOR
        Handles.color = new Color(1f, 0.5f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.ViewDistance);
#endif
    }

    private void DrawFOV(Vector3 origin, float radius, float angleFull)
    {
        if (angleFull <= 0f) return;

        float half = angleFull * 0.5f;
        Vector3 fwd = transform.forward;
        Quaternion qLeft = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion qRight = Quaternion.AngleAxis(half, Vector3.up);
        Vector3 leftDir = qLeft * fwd;
        Vector3 rightDir = qRight * fwd;

        // Edge rays
        Gizmos.color = ViewColor;
        Gizmos.DrawLine(origin, origin + leftDir * radius);
        Gizmos.DrawLine(origin, origin + rightDir * radius);

        // Arc (only the sector, not full circle)
        int segs = Mathf.Max(2, Mathf.CeilToInt(CircleSegments * (angleFull / 360f)));
        float step = angleFull / segs;
        Vector3 prev = origin + leftDir * radius;
        for (int i = 1; i <= segs; i++)
        {
            float a = -half + step * i;
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * fwd;
            Vector3 next = origin + dir * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

#if UNITY_EDITOR
        // Opcjonalny pó³przezroczysty wype³niony sektor (Handles)
        Handles.color = ViewArcColorTransparent;
        Handles.DrawSolidArc(origin, Vector3.up, leftDir, angleFull, radius);
#endif
    }

    private void DrawCircleXZ(Vector3 center, float radius, int segments)
    {
        if (segments < 4) segments = 4;
        float step = Mathf.PI * 2f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}