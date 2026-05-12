using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerDetector))]
[RequireComponent(typeof(BossAutoPathfinding))]
public class Boss : MonoBehaviour
{
    [System.Serializable]
    private class StatsSettings
    {
        public float maxHealth = 100f;
        public float baseMoveSpeed = 3f;
        public float attackSpeed = 2f;
    }

    [System.Serializable]
    private class DetectionSettings
    {
        [Min(1)] public int spotThreshold = 3;
    }

    [System.Serializable]
    private class PatrolSettings
    {
        public Transform[] patrolPoints;
        public Vector2 idleWaitRange = new Vector2(1.5f, 3.5f);
        public float patrolSpeed = 2f;
    }

    [System.Serializable]
    private class CombatSettings
    {
        public GameObject projectilePrefab;
        [Range(0.05f, 1f)] public float lowHealthRatio = 0.3f;
        public float fightDetectionMultiplier = 5f;
        public float immediateFightDistance = 2f;
        [Range(0.1f, 1f)] public float retreatSpeedMultiplier = 0.7f;
    }

    [Header("Config")]
    [SerializeField] private StatsSettings stats = new StatsSettings();
    [SerializeField] private DetectionSettings detection = new DetectionSettings();
    [SerializeField] private PatrolSettings patrol = new PatrolSettings();
    [SerializeField] private CombatSettings combat = new CombatSettings();

    [Header("Debug")]
    [SerializeField] private bool showStateLabel = true;

    public StateMachine StateMachine { get; private set; }
    public SkillManager SkillManager { get; private set; }
    public PlayerDetector PlayerDetector { get; private set; }
    public MovementDecisionMaker MovementDecisionMaker { get; private set; }

    public Animator Animator { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public Transform CachedTransform { get; private set; }
    public Transform Transform => CachedTransform;

    public BossStateType PreviousState { get; set; } = BossStateType.Idle;
    public bool IsFacingRight { get; private set; } = true;

    public float MaxHealth => stats.maxHealth;
    public float CurrentHealth { get; private set; }
    public float BaseMoveSpeed => stats.baseMoveSpeed;
    public float MoveSpeed => Mathf.Max(0f, _stateMoveSpeed + _speedModifier);
    public float AttackSpeed => stats.attackSpeed;
    public float PatrolSpeed => patrol.patrolSpeed;
    public GameObject ProjectilePrefab => combat.projectilePrefab;
    public float FightDetectionMultiplier => combat.fightDetectionMultiplier;
    public float ImmediateFightDistance => combat.immediateFightDistance;
    public float LowHealthRatio => combat.lowHealthRatio;
    public float RetreatSpeedMultiplier => combat.retreatSpeedMultiplier;

    private BossAutoPathfinding _pathfinding;
    private float _stateMoveSpeed;
    private float _speedModifier;
    private int _spotCount;
    private int _currentPatrolIndex;
    private bool _isPatrolForward = true;

    private void Awake()
    {
        CacheComponents();
        InitializeRuntimeState();
        InitializeSystems();
    }

    private void Update()
    {
        UpdateFacingDirection();
        StateMachine?.OnUpdate();
        SkillManager?.OnUpdate();
    }

    private void CacheComponents()
    {
        Animator = GetComponent<Animator>();
        Rb = GetComponent<Rigidbody2D>();
        CachedTransform = transform;
        PlayerDetector = GetComponent<PlayerDetector>();
        _pathfinding = GetComponent<BossAutoPathfinding>();
    }

    private void InitializeRuntimeState()
    {
        CurrentHealth = MaxHealth;
        _stateMoveSpeed = BaseMoveSpeed;
    }

    private void InitializeSystems()
    {
        SkillManager = new SkillManager(this);
        RegisterSkills();
        ConfigureDecisionMakers();

        StateMachine = new StateMachine();
        StateMachine.Initialize(this);
    }

    private void RegisterSkills()
    {
        SkillManager.RegisterSkill(new MeleeAttackSkill(this));
        SkillManager.RegisterSkill(new ProjectileSkill(this, ProjectilePrefab));
    }

    private void ConfigureDecisionMakers()
    {
        SkillManager.GetDecisionMaker().AddFactor(new HealthLossFactor(new[] { 0.3f, 0.8f }));
        SkillManager.GetDecisionMaker().AddFactor(new DistanceFactor(new[] { 0.9f, 0.2f }));

        MovementDecisionMaker = new MovementDecisionMaker(this);
        MovementDecisionMaker.AddFactor(new HealthLossMovementFactor(new[] { 0.2f, 0.6f }));
    }

    public void OnPlayerSpotted()
    {
        _spotCount++;

        if (_spotCount < detection.spotThreshold)
        {
            StateMachine?.HandleEvent(BossEvent.PlayerSpotted);
            return;
        }

        StateMachine?.HandleEvent(BossEvent.IntoFight);
        _spotCount = 0;
    }

    public void OnPlayerLost()
    {
        if (_spotCount < detection.spotThreshold)
        {
            StateMachine?.HandleEvent(BossEvent.PlayerLost);
        }
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        StateMachine?.HandleEvent(BossEvent.TakeDamage, damage);

        if (CurrentHealth <= 0f)
        {
            StateMachine?.HandleEvent(BossEvent.ZeroHealth);
            return;
        }

        if (CurrentHealth <= MaxHealth * LowHealthRatio)
        {
            StateMachine?.HandleEvent(BossEvent.HealthLow);
        }
    }

    public bool IsInFightState()
    {
        return StateMachine?.CurrentState is FightState;
    }

    public int GetSpotCount() => _spotCount;

    public void SetSpeed(float speed)
    {
        _stateMoveSpeed = Mathf.Max(0f, speed);
    }

    public void ResetSpeed()
    {
        _stateMoveSpeed = BaseMoveSpeed;
    }

    public void AddSpeedModifier(float modifier)
    {
        _speedModifier = modifier;
    }

    public void ClearSpeedModifier()
    {
        _speedModifier = 0f;
    }

    public void StopMovement()
    {
        if (Rb != null)
        {
            Rb.velocity = Vector2.zero;
        }
    }

    public void MoveTowardsTarget(Vector2 targetPos, float speed)
    {
        _pathfinding?.MoveTowardsTarget(targetPos, speed);
    }

    public void MoveAwayFromTarget(Vector2 targetPos, float speed)
    {
        _pathfinding?.MoveAwayFromTarget(targetPos, speed * RetreatSpeedMultiplier);
    }

    public void ApplyVelocityDampen(float dampFactor = 0.1f)
    {
        if (Rb != null)
        {
            Rb.velocity *= Mathf.Clamp01(dampFactor);
        }
    }

    public Transform GetCurrentPatrolPoint()
    {
        if (patrol.patrolPoints == null || patrol.patrolPoints.Length == 0)
        {
            return null;
        }

        return patrol.patrolPoints[_currentPatrolIndex];
    }

    public void AdvancePatrolPoint()
    {
        if (patrol.patrolPoints == null || patrol.patrolPoints.Length <= 1)
        {
            return;
        }

        if (_isPatrolForward)
        {
            _currentPatrolIndex++;
            if (_currentPatrolIndex >= patrol.patrolPoints.Length - 1)
            {
                _currentPatrolIndex = patrol.patrolPoints.Length - 1;
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

    public float GetRandomIdleDuration()
    {
        float min = Mathf.Min(patrol.idleWaitRange.x, patrol.idleWaitRange.y);
        float max = Mathf.Max(patrol.idleWaitRange.x, patrol.idleWaitRange.y);
        return Random.Range(min, max);
    }

    public void OnActionEnd()
    {
        if (Animator != null)
        {
            Animator.SetBool("IsActing", false);
        }
    }

    public void OnHurtAnimationEnd()
    {
        if (StateMachine?.CurrentState is HurtState)
        {
            StateMachine.ChangeState(PreviousState);
        }
    }

    private void UpdateFacingDirection()
    {
        if (Rb == null)
        {
            return;
        }

        if (Mathf.Abs(Rb.velocity.x) > 0.1f)
        {
            IsFacingRight = Rb.velocity.x > 0f;
        }

        Vector3 localScale = CachedTransform.localScale;
        localScale.x = IsFacingRight ? Mathf.Abs(localScale.x) : -Mathf.Abs(localScale.x);
        CachedTransform.localScale = localScale;
    }

    private void OnGUI()
    {
        if (!showStateLabel || StateMachine?.CurrentState == null)
        {
            return;
        }

        GUILayout.Label($"Current State: {StateMachine.CurrentState.GetType().Name}");
    }
}
