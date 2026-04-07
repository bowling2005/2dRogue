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

    // --- Unity 组件引用 (提供给 State 使用) ---
    // 状态类需要控制动画、移动、位置，所以暴露这些组件
    public Animator Animator { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public Transform Transform { get; private set; }

    // --- 基础属性 (示例) ---
    public float MaxHealth = 100f;
    public float CurrentHealth = 100f;
    public float MoveSpeed = 3f;

    // 临时属性修正 (用于状态进入/退出时修改)
    private float _speedModifier = 0f;

    // --- 初始化 (Awake) ---
    private void Awake()
    {
        // 1. 获取 Unity 组件
        Animator = GetComponent<Animator>();
        Rb = GetComponent<Rigidbody2D>();
        Transform = GetComponent<Transform>();

        // 2. 初始化纯 C# 系统 (注意顺序)
        // 先初始化技能管理器，因为状态机可能间接依赖它
        SkillManager = new SkillManager(this);

        // 再初始化状态机，并传入 Boss 本体引用
        StateMachine = new StateMachine();
        StateMachine.Initialize(this);
    }

    // --- 生命周期转发 (Update) ---
    private void Update()
    {
        // 将帧更新交给状态机和技能管理器
        // 这样 State 类里的 OnUpdate 才能每帧被执行
        StateMachine?.OnUpdate();
        SkillManager?.OnUpdate();
    }

    // --- 事件触发入口 (供外部调用) ---
    // 例如：碰撞检测脚本检测到玩家后，调用此方法
    public void OnPlayerSpotted()
    {
        StateMachine?.HandleEvent(BossEvent.PlayerSpotted);
    }

    // 例如：受到伤害时
    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        // 触发受伤事件
        StateMachine?.HandleEvent(BossEvent.TakeDamage, damage);

        // 检查是否低血量
        if (CurrentHealth < MaxHealth * 0.3f)
        {
            StateMachine?.HandleEvent(BossEvent.HealthLow);
        }
    }

    // --- 属性访问接口 (供 State 类调用) ---
    // 状态类不应该直接修改 public 变量，而是通过方法
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

    // 为了方便调试，在 Inspector 上显示当前状态
    private void OnGUI()
    {
        if (StateMachine != null && StateMachine.CurrentState != null)
        {
            GUILayout.Label($"Current State: {StateMachine.CurrentState.GetType().Name}");
        }
    }
}