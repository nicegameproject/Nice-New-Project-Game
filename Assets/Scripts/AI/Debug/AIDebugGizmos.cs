using UnityEngine;
using UnityEditor;

[DisallowMultipleComponent]
public class AIDebugGizmos : MonoBehaviour
{
#if UNITY_EDITOR
    private AIController _ai;

    [Range(8, 128)] public int CircleSegments = 48;
    public float CircleYOffset = 0.01f;

    [Header("AI State (Debug)")]
    [SerializeField] private string _currentState;
    [SerializeField] private string _previousState;

    public string CurrentState => _currentState;
    public string PreviousState => _previousState;

    private static readonly Color ViewColor = new Color(1f, 0.5f, 0f, 1f);
    private static readonly Color ViewArcColorTransparent = new Color(1f, 0.5f, 0f, 0.35f);

    private static readonly Color HearingColor = new Color(0f, 1f, 1f, 1f);
    private static readonly Color HearingFill = new Color(0f, 1f, 1f, 0.25f);

    void Awake()
    {
        _ai = GetComponent<AIController>();
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (_ai == null) return;
        if (_ai.StateMachine == null) return;

        string newName = _ai.StateMachine.Current != null ? _ai.StateMachine.Current.Name : "<none>";
        if (newName != _currentState)
        {
            _previousState = _currentState;
            _currentState = newName;
        }
    }

    void OnDrawGizmos()
    {
        if (_ai == null)
            _ai = GetComponent<AIController>();

        if (_ai == null || _ai.Config == null || !_ai.Config.DrawGizmos) return;

        var cfg = _ai.Config;
        var origin = transform.position + Vector3.up * CircleYOffset;

        PerceptionMode mode = cfg.Perception;

        if (_ai.Blackboard != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_ai.Blackboard.CurrentDestination, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_ai.Blackboard.LastKnownTargetPos, 0.2f);
        }

        Gizmos.color = Color.yellow;
        DrawCircleXZ(origin, cfg.SearchRadius, CircleSegments);
        Handles.color = new Color(1f, 1f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.SearchRadius);

        Gizmos.color = Color.red;
        DrawCircleXZ(origin, cfg.PreferredAttackRange, CircleSegments);
        Handles.color = new Color(1f, 0f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.PreferredAttackRange);

        if (mode == PerceptionMode.HearingOnly || mode == PerceptionMode.VisionAndHearing)
        {
            DrawHearingOcclusion(origin, cfg.HearingRadius, Mathf.Max(16, CircleSegments));
        }

        if (mode == PerceptionMode.VisionOnly || mode == PerceptionMode.VisionAndHearing)
        {
            Gizmos.color = ViewColor;
            DrawCircleXZ(origin, cfg.ViewDistance, CircleSegments);
            DrawFOV(origin, cfg.ViewDistance, cfg.ViewAngle);
            Handles.color = new Color(1f, 0.5f, 0f, 0.6f);
            Handles.DrawWireDisc(transform.position, Vector3.up, cfg.ViewDistance);
        }
    }

    private void DrawHearingOcclusion(Vector3 origin, float radius, int segments)
    {
        if (_ai == null || _ai.Config == null) return;
        if (segments < 8) segments = 8;

        Gizmos.color = HearingColor;
        DrawCircleXZ(origin, radius, segments);
        Handles.color = new Color(HearingColor.r, HearingColor.g, HearingColor.b, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, radius);

        int mask = _ai.Config.VisionObstacles;

        Vector3 ear = transform.position;
        float stepDeg = 360f / segments;

        var points = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float angle = stepDeg * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

            Vector3 endPoint;
            if (Physics.Raycast(ear, dir, out RaycastHit hit, radius, mask))
            {
                endPoint = hit.point;
            }
            else
            {
                endPoint = ear + dir * radius;
            }

            points[i] = new Vector3(endPoint.x, origin.y, endPoint.z);
        }

        Gizmos.color = HearingColor;
        Vector3 prev = points[0];
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 next = points[i];
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.DrawLine(points[points.Length - 1], points[0]);

        Handles.color = HearingFill;
        for (int i = 1; i < points.Length; i++)
        {
            Handles.DrawAAConvexPolygon(origin, points[i - 1], points[i]);
        }
    }

    private void DrawFOV(Vector3 origin, float radius, float angleFull)
    {
        if (angleFull <= 0f) return;
        if (_ai == null || _ai.Config == null) return;

        var mask = _ai.Config.VisionObstacles;
        Vector3 eyeOrigin = transform.position;

        float half = angleFull * 0.5f;
        Vector3 fwd = transform.forward;

        int segs = Mathf.Max(2, Mathf.CeilToInt(CircleSegments * (angleFull / 360f)));
        float step = angleFull / segs;

        var arcPoints = new Vector3[segs + 1];

        for (int i = 0; i <= segs; i++)
        {
            float a = -half + step * i;
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * fwd;

            Vector3 end3D;
            if (Physics.Raycast(eyeOrigin, dir, out RaycastHit hit, radius, mask))
            {
                end3D = hit.point;
            }
            else
            {
                end3D = eyeOrigin + dir * radius;
            }

            arcPoints[i] = new Vector3(end3D.x, origin.y, end3D.z);
        }

        Gizmos.color = ViewColor;
        Gizmos.DrawLine(origin, arcPoints[0]);
        Gizmos.DrawLine(origin, arcPoints[arcPoints.Length - 1]);

        Vector3 prev = arcPoints[0];
        for (int i = 1; i < arcPoints.Length; i++)
        {
            Vector3 next = arcPoints[i];
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Handles.color = ViewArcColorTransparent;
        for (int i = 1; i < arcPoints.Length; i++)
        {
            Handles.DrawAAConvexPolygon(origin, arcPoints[i - 1], arcPoints[i]);
        }
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
#endif
}