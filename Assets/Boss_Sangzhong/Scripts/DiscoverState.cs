using UnityEngine;

public class DiscoverState : State
{
    private Transform _playerTransform;

    public DiscoverState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        _playerTransform = GetPlayerTransform();

        _boss.StopMovement();
        _boss.SetSpeed(0f);

        if (_boss.Animator != null)
        {
            _boss.Animator.SetBool("IsMoving", false);
            _boss.Animator.SetTrigger("OnDiscover");
        }

        Debug.Log("Boss: Discover - Enter");
    }

    public override void OnUpdate()
    {
        if (_playerTransform == null)
        {
            _playerTransform = GetPlayerTransform();
            if (_playerTransform == null)
            {
                return;
            }
        }

        float distance = Vector2.Distance(_boss.Transform.position, _playerTransform.position);
        if (distance < _boss.ImmediateFightDistance)
        {
            _stateMachine.ChangeState(BossStateType.Fight);
        }
    }

    public override void OnExit()
    {
        Debug.Log("Boss: Discover - Exit");
    }

    public override void OnEvent(BossEvent eventType, object data = null)
    {
        if (eventType == BossEvent.TakeDamage)
        {
            _stateMachine.ChangeState(BossStateType.Hurt);
            return;
        }

        if (eventType == BossEvent.PlayerLost)
        {
            if (Random.value > 0.5f)
            {
                _stateMachine.ChangeState(BossStateType.Idle);
            }
            else
            {
                _stateMachine.ChangeState(BossStateType.Patrol);
            }

            return;
        }

        if (eventType == BossEvent.IntoFight)
        {
            _stateMachine.ChangeState(BossStateType.Fight);
        }
    }

    private Transform GetPlayerTransform()
    {
        return _boss.PlayerDetector?.GetDetectedPlayer();
    }
}
