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

    [Header("References")]
    public Transform Target;

    public AILocomotion Locomotion { get; private set; }
    public AIAnimationController Animation { get; private set; }
    public VisionSensor Vision { get; private set; }
    public HearingSensor Hearing { get; private set; }
    public CombatModule Combat { get; private set; }
    public Health Health { get; private set; }

    public Blackboard Blackboard { get; private set; }
    public AIStateMachine StateMachine { get; private set; }

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
            Vision.ApplyConfig(Config);
            Hearing.ApplyConfig(Config);
            Locomotion.ApplyConfig(Config);
            Combat.ApplyConfig(Config);
            Health.ApplyConfig(Config);
        }

        Blackboard.Reset();
        Blackboard.Target = Target;

        StateMachine.ChangeState(new SpawnState(this, Blackboard));
    }

    void Update()
    {
        if (Blackboard.IsDead) return;

        Vision.Tick(Blackboard);
        Hearing.Tick(Blackboard);

        StateMachine.Update();
    }

    public void OnDeath()
    {
        Blackboard.IsDead = true;
        StateMachine.ChangeState(new DeathState(this, Blackboard));
    }
}
