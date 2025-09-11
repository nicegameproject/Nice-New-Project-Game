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

        if (_ai.Blackboard != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_ai.Blackboard.CurrentDestination, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_ai.Blackboard.LastKnownTargetPos, 0.2f);
        }

        // Search radius 
        Gizmos.color = Color.yellow;
        DrawCircleXZ(origin, cfg.SearchRadius, CircleSegments);
        Handles.color = new Color(1f, 1f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.SearchRadius);

        // PreferredAttackRange 
        Gizmos.color = Color.red;
        DrawCircleXZ(origin, cfg.PreferredAttackRange, CircleSegments);
        Handles.color = new Color(1f, 0f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.PreferredAttackRange);

        // Hearing Radius 
        Gizmos.color = Color.cyan;
        DrawCircleXZ(origin, cfg.HearingRadius, CircleSegments);
        Handles.color = new Color(1f, 1f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.HearingRadius);

        // View distance
        Gizmos.color = ViewColor;
        DrawCircleXZ(origin, cfg.ViewDistance, CircleSegments);

        DrawFOV(origin, cfg.ViewDistance, cfg.ViewAngle);

        Handles.color = new Color(1f, 0.5f, 0f, 0.6f);
        Handles.DrawWireDisc(transform.position, Vector3.up, cfg.ViewDistance);

    }

    private void DrawFOV(Vector3 origin, float radius, float angleFull)
    {
        if (angleFull <= 0f) return;
        if (_ai == null || _ai.Config == null) return;

        var mask = _ai.Config.VisionObstacles;
        Vector3 eyeOrigin = transform.position;

        float half = angleFull * 0.5f;
        Vector3 fwd = transform.forward;
        Quaternion qLeft = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion qRight = Quaternion.AngleAxis(half, Vector3.up);
        Vector3 leftDir = qLeft * fwd;
        Vector3 rightDir = qRight * fwd;

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