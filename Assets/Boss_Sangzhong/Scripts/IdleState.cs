using UnityEngine;

public class IdleState : State
{
    private float _idleTimer;

    public IdleState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        _boss.SetSpeed(0f);
        _boss.StopMovement();

        if (_boss.Animator != null)
        {
            _boss.Animator.SetBool("IsMoving", false);
        }

        _idleTimer = _boss.GetRandomIdleDuration();
        Debug.Log("Boss: Idle - Enter");
    }

    public override void OnUpdate()
    {
        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0f)
        {
            _stateMachine.ChangeState(BossStateType.Patrol);
        }
    }

    public override void OnExit()
    {
        Debug.Log("Boss: Idle - Exit");
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
            _stateMachine.ChangeState(BossStateType.Discover);
        }
    }
}
