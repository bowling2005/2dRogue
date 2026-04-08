using UnityEngine;

public class DiscoverState : State
{
    private Transform _playerTransform;
    private float _lookSmoothTime = 0.1f;
    private Vector3 _currentLookAngle;

    public DiscoverState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        _playerTransform = GetPlayerTransform();

        // 急停
        _boss.Rb.velocity = Vector2.zero;
        _boss.SetSpeed(0f);

        if (_boss.Animator != null)
        {
            _boss.Animator.SetBool("IsMoving", false);
            _boss.Animator.SetTrigger("OnDiscover"); // 触发器
        }

        Debug.Log("Boss: Discover - 发现玩家，进入警戒");
    }

    public override void OnUpdate()
    {
        if (_playerTransform == null) return;


        // 检测玩家距离，如果太近可以提前进入战斗
         float dist = Vector2.Distance(_boss.Transform.position, _playerTransform.position);
         if (dist < 2f) _stateMachine.ChangeState(BossStateType.Fight);
    }

    public override void OnExit()
    {
        Debug.Log("Boss: Discover - 退出警戒");
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
            // 玩家丢失：50% 概率切 Idle，50% 切 Patrol，增加行为多样性
            if (Random.value > 0.5f)
                _stateMachine.ChangeState(BossStateType.Idle);
            else
                _stateMachine.ChangeState(BossStateType.Patrol);
        }
        else if (eventType == BossEvent.IntoFight)
        {
            // 满足战斗条件，进入战斗 (单向)
            Debug.Log("Boss: Discover -> Fight!");
            _stateMachine.ChangeState(BossStateType.Fight);
        }
    }

    private Transform GetPlayerTransform()
    {
         return _boss.PlayerDetector?.GetDetectedPlayer();
    }
  
}