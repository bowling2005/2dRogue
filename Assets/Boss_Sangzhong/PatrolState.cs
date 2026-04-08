using UnityEngine;

public class PatrolState : State
{
    private Transform _targetPoint;
    private float _arrivalThreshold = 0.3f;  // 到达判定距离

    public PatrolState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        // 1. 获取目标点
        _targetPoint = _boss.GetCurrentPatrolPoint();
        if (_targetPoint == null)
        {
            Debug.LogWarning("PatrolState: No patrol points assigned!");
            _stateMachine.ChangeState(BossStateType.Idle);
            return;
        }

        _boss.SetSpeed(_boss.patrolSpeed);

        if (_boss.Animator != null)
            _boss.Animator.SetBool("IsMoving", true);

        //Debug.Log($"Boss: Patrol - 前往点 {_targetPoint.name}");
    }

    public override void OnUpdate()
    {
        if (_targetPoint == null) return;

        Vector2 direction = (_targetPoint.position - _boss.Transform.position).normalized;
        _boss.Rb.velocity = direction * _boss.MoveSpeed;

        float distance = Vector2.Distance(_boss.Transform.position, _targetPoint.position);
        if (distance <= _arrivalThreshold)
        {
            // 到达！停止移动
            _boss.Rb.velocity = Vector2.zero;
            // 更新下一个巡逻点索引 (为下次巡逻准备)
            _boss.AdvancePatrolPoint();
            _stateMachine.ChangeState(BossStateType.Idle);
            return;
        }
    }

    public override void OnExit()
    {
        // 清理速度，避免惯性
        _boss.Rb.velocity = Vector2.zero;
        Debug.Log("Boss: Patrol - 结束巡逻");
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
            // 发现玩家，立即中断巡逻
            _boss.Rb.velocity = Vector2.zero; // 急停
            _stateMachine.ChangeState(BossStateType.Discover);
        }
    }
}