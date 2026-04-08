using UnityEngine;

public class IdleState : State
{
    private float _idleTimer;
    private float _randomWaitTime;

    public IdleState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        _boss.SetSpeed(0f); 

        if (_boss.Animator != null)
            _boss.Animator.SetBool("IsMoving", false);
        _randomWaitTime = Random.Range(1.5f, 3.5f);
        _idleTimer = _randomWaitTime;

        Debug.Log("Boss: Idle - 开始待机");
    }

    public override void OnUpdate()
    {
        _idleTimer -= Time.deltaTime;

        if (_idleTimer <= 0f)
        {
            _stateMachine.ChangeState(BossStateType.Patrol);
            return;
        }
    }

    public override void OnExit()
    {
        Debug.Log("Boss: Idle - 结束待机");
    }

    public override void OnEvent(BossEvent eventType, object data = null)
    {
        if (eventType == BossEvent.TakeDamage)
        {
            _stateMachine.ChangeState(BossStateType.Hurt);
            return;
        }


        if (eventType == BossEvent.PlayerSpotted)
        {
            // 发现玩家，立即中断待机，进入发现状态
            _stateMachine.ChangeState(BossStateType.Discover);
        }
    }
}