using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    private readonly Dictionary<BossStateType, State> _states = new Dictionary<BossStateType, State>();
    private Boss _boss;

    public State CurrentState { get; private set; }

    public void Initialize(Boss boss)
    {
        _boss = boss;

        _states.Add(BossStateType.Idle, new IdleState(this, boss));
        _states.Add(BossStateType.Patrol, new PatrolState(this, boss));
        _states.Add(BossStateType.Discover, new DiscoverState(this, boss));
        _states.Add(BossStateType.Fight, new FightState(this, boss));
        _states.Add(BossStateType.Hurt, new HurtState(this, boss));
        _states.Add(BossStateType.Death, new DeathState(this, boss));

        ChangeState(BossStateType.Idle);
    }

    public void OnUpdate()
    {
        CurrentState?.OnUpdate();
    }

    public void ChangeState(BossStateType newStateType)
    {
        if (newStateType == BossStateType.Hurt && CurrentState != null)
        {
            if (CurrentState is HurtState)
            {
                return;
            }

            _boss.PreviousState = GetCurrentStateType();
            Debug.Log($"StateMachine: Interrupted. Save state {_boss.PreviousState}.");
        }

        CurrentState?.OnExit();

        if (!_states.TryGetValue(newStateType, out State newState))
        {
            Debug.LogError($"StateMachine: State {newStateType} not found.");
            return;
        }

        CurrentState = newState;
        CurrentState.OnEnter();
    }

    public BossStateType GetCurrentStateType()
    {
        if (CurrentState is IdleState) return BossStateType.Idle;
        if (CurrentState is PatrolState) return BossStateType.Patrol;
        if (CurrentState is DiscoverState) return BossStateType.Discover;
        if (CurrentState is FightState) return BossStateType.Fight;
        if (CurrentState is HurtState) return BossStateType.Hurt;
        if (CurrentState is DeathState) return BossStateType.Death;
        return BossStateType.Idle;
    }

    public void HandleEvent(BossEvent eventType, object data = null)
    {
        CurrentState?.OnEvent(eventType, data);
    }
}
