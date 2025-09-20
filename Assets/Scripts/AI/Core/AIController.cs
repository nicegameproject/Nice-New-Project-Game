using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class AIController : MonoBehaviour
{
    [Header("Config")]
    public EnemyConfig Config;

    [Header("Debug")]
    public bool DebugStates = true;

    public AILocomotion Locomotion { get; private set; }
    public AIAnimationController Animation { get; private set; }
    public VisionSensor Vision { get; private set; }
    public HearingSensor Hearing { get; private set; }
    public CombatModule Combat { get; private set; }
    public Health Health { get; private set; }

    public Blackboard Blackboard { get; private set; }
    public AIStateMachine StateMachine { get; private set; }

    private bool _deathHandled;
    private PerceptionMode _perceptionMode = PerceptionMode.VisionAndHearing;

    void Awake()
    {
        var agent = GetComponent<NavMeshAgent>();
        Locomotion = GetComponent<AILocomotion>();
        Locomotion.Initialize(agent);

        Animation = GetComponent<AIAnimationController>();
        if (Animation == null) Animation = gameObject.AddComponent<AIAnimationController>();

        Vision = GetComponent<VisionSensor>();
        if (Vision == null) Vision = gameObject.AddComponent<VisionSensor>();

        Hearing = GetComponent<HearingSensor>();
        if (Hearing == null) Hearing = gameObject.AddComponent<HearingSensor>();

        Combat = GetComponent<CombatModule>();
        if (Combat == null) Combat = gameObject.AddComponent<CombatModule>();

        Health = GetComponent<Health>();
        if (Health == null) Health = gameObject.AddComponent<Health>();

        Blackboard = new Blackboard();
        StateMachine = new AIStateMachine(this, DebugStates);
    }

    void Start()
    {
        if (Config != null)
        {
            _perceptionMode = Config.Perception;
            if (_perceptionMode != PerceptionMode.HearingOnly && Vision != null)
                Vision.ApplyConfig(Config);
            if (_perceptionMode != PerceptionMode.VisionOnly && Hearing != null)
                Hearing.ApplyConfig(Config);
            Locomotion.ApplyConfig(Config);
            Combat.ApplyConfig(Config);
            Health.ApplyConfig(Config);
        }

        if (_perceptionMode == PerceptionMode.HearingOnly && Vision != null)
            Vision.enabled = false;
        if (_perceptionMode == PerceptionMode.VisionOnly && Hearing != null)
            Hearing.enabled = false;

        Blackboard.Reset();
        StateMachine.ChangeState(new SpawnState(this, Blackboard));
    }

    void Update()
    {
        if (Health != null && Health.IsDead) return;

        if (_perceptionMode != PerceptionMode.VisionOnly && Hearing != null && Hearing.enabled)
        {
            Hearing.PullHeardInfo(Blackboard);
        }

        if (_perceptionMode != PerceptionMode.HearingOnly && Vision != null && Vision.enabled)
        {
            Vision.Tick(Blackboard);
        }

        if (_perceptionMode != PerceptionMode.VisionOnly && Hearing != null && Hearing.enabled)
        {
            Hearing.Tick(Blackboard);
        }

        if (Blackboard.Target != null)
        {
            Vector3 targetPos = Blackboard.Target.position;
            float dist = Vector3.Distance(transform.position, targetPos);

            Blackboard.DistanceToTarget = dist;
            Blackboard.LastKnownTargetPos = targetPos;

            for (int i = 0; i < Blackboard.TrackedCount; i++)
            {
                var t = Blackboard.TrackedTargets[i];
                if (t.Transform == Blackboard.Target)
                {
                    t.Distance = dist;
                    t.LastKnownPos = targetPos;
                    break;
                }
            }
        }

        StateMachine.Update();
    }

    public void OnDeath()
    {
        if (_deathHandled) return;
        _deathHandled = true;
        StateMachine.ChangeState(new DeathState(this, Blackboard));
    }
}
