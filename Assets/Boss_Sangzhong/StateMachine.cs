using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    // 1. 状态字典：通过枚举快速查找状态对象
    // 避免每次切换都 new 新对象，而是复用初始化好的状态实例
    private Dictionary<BossStateType, State> _states = new Dictionary<BossStateType, State>();

    // 2. 当前状态
    public State CurrentState { get; private set; }

    // 3. Boss 引用 (用于初始化状态时传递)
    private Boss _boss;

    // 4. 初始化 (在 Boss.Awake 中调用)
    // 负责实例化所有可能的状态，并放入字典
    public void Initialize(Boss boss)
    {
        _boss = boss;

        // 实例化所有状态 (这里先写框架，具体状态类在下一步完善)
        _states.Add(BossStateType.Idle, new IdleState(this, boss));
        _states.Add(BossStateType.Patrol, new PatrolState(this, boss));
        _states.Add(BossStateType.Discover, new DiscoverState(this, boss));
        _states.Add(BossStateType.Fight, new FightState(this, boss));
        _states.Add(BossStateType.Hurt, new HurtState(this, boss));
        _states.Add(BossStateType.Death, new DeathState(this, boss)); 

        // 默认进入待机状态
        ChangeState(BossStateType.Idle);
    }

    // 5. 帧更新 (在 Boss.Update 中调用)
    public void OnUpdate()
    {
        CurrentState?.OnUpdate();
    }

    // 6. 状态切换 (核心逻辑)
    public void ChangeState(BossStateType newStateType)
    {
        // 1. 如果新状态是 Hurt，先备份当前状态 (中断逻辑的核心)
        if (newStateType == BossStateType.Hurt && CurrentState != null)
        {
            // 如果当前已经是 Hurt，避免重复嵌套备份
            if (CurrentState is HurtState) return;

            _boss.PreviousState = GetCurrentStateType(); 
            Debug.Log($"StateMachine: Interrupted! Saving state: {_boss.PreviousState}");
        }
        CurrentState?.OnExit();

        // 2. 获取新状态
        if (_states.TryGetValue(newStateType, out State newState))
        {
            CurrentState = newState;
            // 3. 进入新状态
            CurrentState.OnEnter();
        }
        else
        {
            Debug.LogError($"State {newStateType} not found in StateMachine!");
        }
    }
    public BossStateType GetCurrentStateType()
    {
        if (CurrentState is IdleState) return BossStateType.Idle;
        if (CurrentState is PatrolState) return BossStateType.Patrol;
        if (CurrentState is DiscoverState) return BossStateType.Discover;
        if (CurrentState is FightState) return BossStateType.Fight;
        if (CurrentState is HurtState) return BossStateType.Hurt;
        return BossStateType.Idle;
    }

    // 7. 事件分发 (在 Boss 接收到外部事件时调用)
    public void HandleEvent(BossEvent eventType, object data = null)
    {
        // 将事件转发给当前状态处理
        CurrentState?.OnEvent(eventType, data);
    }
}