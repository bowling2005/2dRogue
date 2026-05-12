using UnityEngine;

public class PatrolState : State
{
    private const float ArrivalThreshold = 0.3f;

    private Transform _targetPoint;

    public PatrolState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        _targetPoint = _boss.GetCurrentPatrolPoint();
        if (_targetPoint == null)
        {
            Debug.LogWarning("PatrolState: No patrol points assigned.");
            _stateMachine.ChangeState(BossStateType.Idle);
            return;
        }

        _boss.SetSpeed(_boss.PatrolSpeed);

        if (_boss.Animator != null)
        {
            _boss.Animator.SetBool("IsMoving", true);
        }
    }

    public override void OnUpdate()
    {
        if (_targetPoint == null)
        {
            return;
        }

        Vector2 direction = (_targetPoint.position - _boss.Transform.position).normalized;
        _boss.Rb.velocity = direction * _boss.MoveSpeed;

        float distance = Vector2.Distance(_boss.Transform.position, _targetPoint.position);
        if (distance > ArrivalThreshold)
        {
            return;
        }

        _boss.StopMovement();
        _boss.AdvancePatrolPoint();
        _stateMachine.ChangeState(BossStateType.Idle);
    }

    public override void OnExit()
    {
        _boss.StopMovement();
        Debug.Log("Boss: Patrol - Exit");
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
            _boss.StopMovement();
            _stateMachine.ChangeState(BossStateType.Discover);
        }
    }
}
