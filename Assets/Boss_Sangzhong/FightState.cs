using UnityEngine;

public class FightState : State
{
    // --- 决策器引用 ---
    private SkillDecisionMaker _skillDM;
    private MovementDecisionMaker _moveDM;

    // --- 运行时缓存 ---
    private Transform _playerCache;
    private Skill _pendingSkill;

    // --- 核心控制 ---
    private bool _isActionLocked = false;
    private enum SubState { Idle, Seeking, Casting, Retreating }
    private SubState _currentSubState = SubState.Idle;

    // --- 战斗专属配置 ---
    [SerializeField] private float fightDetectionMultiplier = 5f;  // 战斗时检测范围倍数

    public FightState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss)
    {
        _skillDM = _boss.SkillManager.GetDecisionMaker();
        _moveDM = _boss.MovementDecisionMaker;
    }

    public override void OnEnter()
    {
        Debug.Log("Boss: Fight - Enter");

        // 1. 进入战斗时扩大检测范围，实现"战斗内永不脱战"
        _boss.PlayerDetector?.ExpandDetectionRange(fightDetectionMultiplier);

        // 2. 属性与状态初始化
        _boss.SetSpeed(_boss.MoveSpeed * 1.2f);
        _playerCache = GetPlayerTransform();
        ResetState();
    }

    private void ResetState()
    {
        _isActionLocked = false;
        _pendingSkill = null;
        _currentSubState = SubState.Idle;
        _skillDM.ResetDecisionTimer();
        _moveDM.ResetTimer();
    }

    public override void OnUpdate()
    {
        // 0. 基础检查
        if (_boss.CurrentHealth <= 0f) { _stateMachine.HandleEvent(BossEvent.ZeroHealth); return; }

        _playerCache = GetPlayerTransform();
        // 战斗状态下，即使玩家暂时离开原范围，扩大后的范围也能检测到,所以这里不需要处理"丢失"，除非真的跑出扩大后的范围（极罕见）
        if (_playerCache == null)
        {
            return;
        }

        // 1.动作锁检查
        if (CheckAnimationLock())
        {
            // 锁定时冻结两个决策器，防止决策干扰动画
            _skillDM.Freeze();
            _moveDM.Freeze();
            return;
        }
        else
        {
            // 解锁时恢复决策器
            _skillDM.Unfreeze();
            _moveDM.Unfreeze();
        }
        // 2. 技能执行阶段 (优先级最高)
        if (_pendingSkill != null)
        {
            HandleSkillExecution();
            return;
        }
        // 3. 决策阶段 (互斥逻辑)
        // A. 尝试技能决策 (高优先级)
        Skill selectedSkill = _skillDM.SelectSkill(_boss.PlayerDetector);
        if (selectedSkill != null)
        {
            _pendingSkill = selectedSkill;
            _currentSubState = SubState.Seeking;
            return;
        }

        // B. 技能决策器冷却中 -> 尝试移动决策
        if (_moveDM.TryDecide(_boss.PlayerDetector))
        {
            HandleMovementCommand(_moveDM.CurrentCommand);
        }
        // 如果移动决策器也冷却，保持上一帧速度（惯性）
    }

    // --- 技能执行逻辑 ---
    private void HandleSkillExecution()
    {
        if (_pendingSkill == null) return;

        float dist = Vector2.Distance(_boss.Transform.position, _playerCache.position);

        // 阶段 1: 定位
        if (dist > _pendingSkill.castRange)
        {
            _currentSubState = SubState.Seeking;
            _boss.MoveTowardsTarget(_playerCache.position, _boss.MoveSpeed);
            return;
        }

        // 阶段 2: 释放
        if (dist <= _pendingSkill.castRange)
        {
            _currentSubState = SubState.Casting;

            if (_boss.SkillManager.TryCastSkill(_pendingSkill.skillId, _playerCache))
            {
                OnSkillCastSuccess();
            }
            else
            {
                _pendingSkill = null;
                _currentSubState = SubState.Idle;
            }
        }
    }

    private void OnSkillCastSuccess()
    {
        _pendingSkill = null;
        // 技能释放成功，立即允许重新决策
        _skillDM.ResetDecisionTimer();
        _moveDM.ResetTimer();
        // 注意：_isActionLocked 由 CheckAnimationLock 管理，确保动画播完
    }

    // --- 移动执行逻辑 ---
    private void HandleMovementCommand(MoveCommand cmd)
    {
        if (cmd == MoveCommand.Towards)
        {
            _currentSubState = SubState.Seeking;
            _boss.MoveTowardsTarget(_playerCache.position, _boss.MoveSpeed);
        }
        else if (cmd == MoveCommand.Away)
        {
            _currentSubState = SubState.Retreating;
            _boss.MoveAwayFromTarget(_playerCache.position, _boss.MoveSpeed);
        }
        else
        {
            _currentSubState = SubState.Idle;
            _boss.Rb.velocity = Vector2.zero;
        }
    }

    // --- 动作锁检测 ---
    private bool CheckAnimationLock()
    {
        if (_boss.Animator == null) return false;

        // 检测 Animator 的 "IsActing" 参数
        if (_boss.Animator.GetBool("IsActing"))
        {
            _isActionLocked = true;
            return true;
        }

        // 动画结束，解锁
        if (_isActionLocked)
        {
            _isActionLocked = false;
            Debug.Log("Boss: Fight ActionLock Released");
        }
        return false;
    }

    public override void OnEvent(BossEvent eventType, object data = null)
    {
        // 1. 受伤中断 (最高优先级)
        if (eventType == BossEvent.TakeDamage)
        {
            _pendingSkill = null;
            _stateMachine.ChangeState(BossStateType.Hurt);
            return;
        }

        // 2. 死亡
        if (eventType == BossEvent.ZeroHealth)
        {
            _stateMachine.ChangeState(BossStateType.Death);
            return;
        }

        // 3. 玩家丢失事件 (战斗状态下理论上不会触发，但防御性编程)
        if (eventType == BossEvent.PlayerLost)
        {
            // 这里按设计：战斗状态永不脱战，所以忽略此事件
            Debug.LogWarning("FightState: PlayerLost received but ignored (combat lock)");
        }
    }

    public override void OnExit()
    {
        _boss.PlayerDetector?.RestoreDetectionRange();

        _pendingSkill = null;
        _boss.Rb.velocity = Vector2.zero;
        if (_boss.Animator != null)
            _boss.Animator.SetBool("IsActing", false);

       // Debug.Log("Boss: Fight - Exit, detection range restored");
    }

    private Transform GetPlayerTransform()
    {
        return _boss.PlayerDetector?.GetDetectedPlayer();
    }
}