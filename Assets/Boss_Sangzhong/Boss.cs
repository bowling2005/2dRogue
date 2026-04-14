using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
// 1. 继承 MonoBehaviour，挂在 Boss 游戏物体上
public class Boss : MonoBehaviour
{
    // --- 系统模块引用 (提供给 State 使用) ---
    // 设为 public get，方便 State 类通过 _boss 引用访问
    public StateMachine StateMachine { get; private set; }
    public SkillManager SkillManager { get; private set; }
    public PlayerDetector PlayerDetector { get; private set; }

    public MovementDecisionMaker MovementDecisionMaker { get; private set; }

    public BossStateType PreviousState { get; set; } = BossStateType.Idle;

    // --- Unity 组件引用 (提供给 State 使用) ---
    // 状态类需要控制动画、移动、位置，所以暴露这些组件
    public Animator Animator { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public Transform Transform { get; private set; }

    // --- 基础属性 (示例) ---
    public float MaxHealth = 100f;
    public float CurrentHealth = 100f;
    public float MoveSpeed = 3f;
    public float AttackSpeed = 2f;
    public float Auncel = 0f;
    public bool IsFacingRight = true;

    //发现玩家参数设置
    private int spotThreshold = 3;  // 触发多少次后切换状态
    private int spotCount = 0;

    // 临时属性修正 (用于状态进入/退出时修改)
    private float _speedModifier = 0f;

    public Transform[] patrolPoints;  // 在 Inspector 中拖入巡逻点空物体
    public float idleWaitTime = 2f;   // 到达巡逻点后待机多少秒
    public float patrolSpeed = 2f;    // 巡逻时的移动速度

    public float discoverLookSpeed = 5f;  // Discover 状态下转向玩家的速度

    // 运行时数据
    private int _currentPatrolIndex = 0;
    private float _idleTimer = 0f;
    private bool _isPatrolForward = true; // 控制巡逻方向（往返）

 // 1. 创建技能实例 (需要赋值预制体)
    //public GameObject meleeEffectPrefab; 
    public GameObject projectilePrefab;

    // --- 初始化 (Awake) ---
    private void Awake()
    {
        Animator = GetComponent<Animator>();
        Rb = GetComponent<Rigidbody2D>();
        Transform = GetComponent<Transform>();

        //初始化探测器
        PlayerDetector = GetComponent<PlayerDetector>();
        // 初始化技能系统
        SkillManager = new SkillManager(this);

        Skill melee = new MeleeAttackSkill(this);
        Skill range = new ProjectileSkill(this, null); // 记得在 Inspector 赋值预制体

        SkillManager.RegisterSkill(melee);
        SkillManager.RegisterSkill(range);

        // 2. 创建影响因子并配置权重
        // 假设有 2 个技能：索引 0=Melee, 1=Range
        // 血量越低，越倾向于远程 (权重：Melee 0.3, Range 0.8)
        float[] healthWeights = new float[] { 0.3f, 0.8f };
        SkillManager.GetDecisionMaker().AddFactor(new HealthLossFactor(healthWeights));

        // 距离越近，越倾向于近战 (权重：Melee 0.9, Range 0.2)
        float[] distWeights = new float[] { 0.9f, 0.2f };
        SkillManager.GetDecisionMaker().AddFactor(new DistanceFactor(distWeights));

        // 初始化移动决策器
        MovementDecisionMaker = new MovementDecisionMaker(this);
        // 配置移动因子 (示例：血量低时 80% 概率远离)
        MovementDecisionMaker.AddFactor(new HealthLossMovementFactor(new float[] { 0.2f, 0.6f }));

        StateMachine = new StateMachine();
        StateMachine.Initialize(this);
    }

    // --- 生命周期转发 (Update) ---
    private void Update()
    {
        CheckFlipSprite();
        StateMachine?.OnUpdate();
        SkillManager?.OnUpdate();
    }

    // --- 事件触发入口 (供外部调用) ---
    public void OnPlayerSpotted()
    {
        spotCount++;

        if(spotCount >= 0 && spotCount < spotThreshold)
        {
            StateMachine?.HandleEvent(BossEvent.PlayerSpotted);
        }

        else if (spotCount >= spotThreshold)
        {
            StateMachine?.HandleEvent(BossEvent.IntoFight);
            spotCount = 0;  // 重置计数
        }
    }

    public void OnPlayerLost()
    {
        if (spotCount >= 0 && spotCount < spotThreshold)
        {
            StateMachine?.HandleEvent(BossEvent.PlayerLost);
        }
    }

    private void CheckFlipSprite()
    {
        Vector3 localScale = Transform.localScale;
        float velocityThreshold = 0.1f;

        if (Mathf.Abs(Rb.velocity.x) > velocityThreshold)
        {
            IsFacingRight = Rb.velocity.x > 0;
        }
        if (IsFacingRight)
        {
            localScale.x = Mathf.Abs(localScale.x);
        }
        else
        {
            localScale.x = -Mathf.Abs(localScale.x);
        }
        Transform.localScale = localScale;
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        // 触发受伤事件
        StateMachine?.HandleEvent(BossEvent.TakeDamage, damage);

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0;
            StateMachine?.HandleEvent(BossEvent.ZeroHealth);
        }
        else if (CurrentHealth < MaxHealth * 0.3f)
        {
            StateMachine?.HandleEvent(BossEvent.HealthLow);
        }
    }

    public bool IsInFightState()
    {
        if (StateMachine != null)
            return StateMachine.CurrentState is FightState;

        return false;
    }

    public int GetSpotCount() => spotCount;
    public void SetSpeed(float speed)
    {
        MoveSpeed = speed + _speedModifier;
    }

    public void AddSpeedModifier(float modifier)
    {
        _speedModifier = modifier;
        SetSpeed(MoveSpeed); // 重新应用
    }

    public void ClearSpeedModifier()
    {
        _speedModifier = 0f;
        SetSpeed(MoveSpeed);
    }

    public void MoveTowardsTarget(Vector2 targetPos, float speed)
    {
        //后续需要补充A*寻路接近目标代码
    }
    public void MoveAwayFromTarget(Vector2 targetPos, float speed)
    {
        //后续需要补充远离目标代码
    }
    public void ApplyVelocityDampen(float dampFactor = 0.1f)
    {
        if (Rb != null && Rb.velocity.x >= 0.3f)
        {
            Rb.velocity *= dampFactor;
        }
    }
    public Transform GetCurrentPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return null;
        return patrolPoints[_currentPatrolIndex];
    }
    public void AdvancePatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length <= 1) return;

        if (_isPatrolForward)
        {
            _currentPatrolIndex++;
            if (_currentPatrolIndex >= patrolPoints.Length - 1)
            {
                _currentPatrolIndex = patrolPoints.Length - 1;
                _isPatrolForward = false;
            }
        }
        else
        {
            _currentPatrolIndex--;
            if (_currentPatrolIndex <= 0)
            {
                _currentPatrolIndex = 0;
                _isPatrolForward = true;
            }
        }
    }


    public float GetIdleWaitTime() => idleWaitTime;
    public void SetIdleTimer(float time) => _idleTimer = time;
    public float GetIdleTimer() => _idleTimer;
    public void AddIdleTimer(float delta) => _idleTimer += delta;
    public void ResetIdleTimer() => _idleTimer = 0f;
    public void OnActionEnd()
    {
        if (Animator != null)
            Animator.SetBool("IsActing", false);
    }

    public void OnHurtAnimationEnd()
    {
        // 如果当前确实是受伤状态，则请求切回之前的状态
        if (StateMachine?.CurrentState is HurtState)
        {
            StateMachine.ChangeState(PreviousState);
        }
    }
    // 为了方便调试，在 Inspector 上显示当前状态
    private void OnGUI()
    {
        if (StateMachine != null && StateMachine.CurrentState != null)
        {
            GUILayout.Label($"Current State: {StateMachine.CurrentState.GetType().Name}");
        }
    }
}