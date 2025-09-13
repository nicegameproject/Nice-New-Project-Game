using System;
using UnityEngine;

public interface IAIState
{
    string Name { get; }
    void Enter();
    void Update();
    void Exit();
}

public sealed class AIStateMachine
{
    public IAIState Current { get; private set; }
    public IAIState Previous { get; private set; }

    public event Action<IAIState, IAIState> StateChanged;

    private readonly MonoBehaviour _owner;
    private readonly bool _enableLogging;

    public AIStateMachine(MonoBehaviour owner = null, bool enableLogging = true)
    {
        _owner = owner;
        _enableLogging = enableLogging;
    }

    public void ChangeState(IAIState next)
    {
        if (Current == next) return;

        var prev = Current;

        Current?.Exit();
        Previous = Current;
        Current = next;
        Current?.Enter();

        if (_enableLogging)
        {
            string ownerName = _owner != null ? _owner.name : "AI";
            string prevName = prev != null ? prev.Name : "<none>";
            string currName = Current != null ? Current.Name : "<none>";
            Debug.Log($"[AI][{ownerName}] State change: {prevName} -> {currName}");
        }

        StateChanged?.Invoke(prev, Current);
    }

    public void Update()
    {
        Current?.Update();
    }
}