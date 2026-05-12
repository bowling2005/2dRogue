using UnityEngine;

public class FightState : State
{
    private enum SubState
    {
        Idle,
        Seeking,
        Casting,
        Retreating
    }

    private readonly SkillDecisionMaker _skillDM;
    private readonly MovementDecisionMaker _moveDM;

    private Transform _playerCache;
    private Skill _pendingSkill;
    private bool _isActionLocked;
    private SubState _currentSubState = SubState.Idle;

    public FightState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss)
    {
        _skillDM = _boss.SkillManager.GetDecisionMaker();
        _moveDM = _boss.MovementDecisionMaker;
    }

    public override void OnEnter()
    {
        Debug.Log("Boss: Fight - Enter");

        _boss.PlayerDetector?.ExpandDetectionRange(_boss.FightDetectionMultiplier);
        _boss.SetSpeed(_boss.BaseMoveSpeed * 1.2f);
        _playerCache = GetPlayerTransform();

        ResetState();
    }

    public override void OnUpdate()
    {
        if (_boss.CurrentHealth <= 0f)
        {
            _stateMachine.HandleEvent(BossEvent.ZeroHealth);
            return;
        }

        _playerCache = GetPlayerTransform();
        if (_playerCache == null)
        {
            return;
        }

        if (CheckAnimationLock())
        {
            _skillDM.Freeze();
            _moveDM.Freeze();
            return;
        }

        _skillDM.Unfreeze();
        _moveDM.Unfreeze();

        if (_pendingSkill != null)
        {
            HandleSkillExecution();
            return;
        }

        Skill selectedSkill = _skillDM.SelectSkill(_boss.PlayerDetector);
        if (selectedSkill != null)
        {
            _pendingSkill = selectedSkill;
            _currentSubState = SubState.Seeking;
            return;
        }

        if (_moveDM.TryDecide(_boss.PlayerDetector))
        {
            HandleMovementCommand(_moveDM.CurrentCommand);
        }
    }

    public override void OnExit()
    {
        _boss.PlayerDetector?.RestoreDetectionRange();
        _pendingSkill = null;
        _boss.StopMovement();

        if (_boss.Animator != null)
        {
            _boss.Animator.SetBool("IsActing", false);
        }
    }

    public override void OnEvent(BossEvent eventType, object data = null)
    {
        if (eventType == BossEvent.TakeDamage)
        {
            _pendingSkill = null;
            _stateMachine.ChangeState(BossStateType.Hurt);
            return;
        }

        if (eventType == BossEvent.ZeroHealth)
        {
            _stateMachine.ChangeState(BossStateType.Death);
            return;
        }

        if (eventType == BossEvent.PlayerLost)
        {
            Debug.LogWarning("FightState: PlayerLost ignored while in combat.");
        }
    }

    private void ResetState()
    {
        _isActionLocked = false;
        _pendingSkill = null;
        _currentSubState = SubState.Idle;
        _skillDM.ResetDecisionTimer();
        _moveDM.ResetTimer();
    }

    private void HandleSkillExecution()
    {
        if (_pendingSkill == null || _playerCache == null)
        {
            return;
        }

        float distance = Vector2.Distance(_boss.Transform.position, _playerCache.position);
        if (distance > _pendingSkill.castRange)
        {
            _currentSubState = SubState.Seeking;
            _boss.MoveTowardsTarget(_playerCache.position, _boss.MoveSpeed);
            return;
        }

        _currentSubState = SubState.Casting;
        if (_boss.SkillManager.TryCastSkill(_pendingSkill.skillId, _playerCache))
        {
            OnSkillCastSuccess();
            return;
        }

        _pendingSkill = null;
        _currentSubState = SubState.Idle;
    }

    private void HandleMovementCommand(MoveCommand cmd)
    {
        if (_playerCache == null)
        {
            return;
        }

        if (cmd == MoveCommand.Towards)
        {
            _currentSubState = SubState.Seeking;
            _boss.MoveTowardsTarget(_playerCache.position, _boss.MoveSpeed);
            return;
        }

        if (cmd == MoveCommand.Away)
        {
            _currentSubState = SubState.Retreating;
            _boss.MoveAwayFromTarget(_playerCache.position, _boss.MoveSpeed);
            return;
        }

        _currentSubState = SubState.Idle;
        _boss.StopMovement();
    }

    private void OnSkillCastSuccess()
    {
        _pendingSkill = null;
        _skillDM.ResetDecisionTimer();
        _moveDM.ResetTimer();
    }

    private bool CheckAnimationLock()
    {
        if (_boss.Animator == null)
        {
            return false;
        }

        if (_boss.Animator.GetBool("IsActing"))
        {
            _isActionLocked = true;
            return true;
        }

        if (_isActionLocked)
        {
            _isActionLocked = false;
            Debug.Log("Boss: Fight action lock released.");
        }

        return false;
    }

    private Transform GetPlayerTransform()
    {
        return _boss.PlayerDetector?.GetDetectedPlayer();
    }
}
