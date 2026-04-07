using UnityEngine;

// 状态基类 (抽象类，禁止直接实例化)
public abstract class State
{
    // 1. 核心依赖引用
    protected StateMachine _stateMachine;
    protected Boss _boss;
    // 方案 B：通过 Boss 访问技能管理器，实现复用
    protected SkillManager _skillManager;

    // 2. 构造函数 (依赖注入)
    // 在状态机创建状态时，必须传入这两个引用
    public State(StateMachine stateMachine, Boss boss)
    {
        _stateMachine = stateMachine;
        _boss = boss;
        // 提前获取技能管理器引用，方便子类直接使用
        _skillManager = boss.SkillManager;
    }

    // 3. 生命周期方法 (虚函数，允许子类重写)

    // 进入状态：处理属性变化、播放进入动画、初始化计时器
    public virtual void OnEnter() { }

    // 帧更新：处理移动、逻辑判断、技能冷却检查
    public virtual void OnUpdate() { }

    // 退出状态：还原属性、停止音效、清理缓存
    public virtual void OnExit() { }

    // 4. 事件处理 (局部事件透传)
    // eventType: 事件类型 (如玩家发现、受伤)
    //  附加数据 (如伤害值、玩家位置)
    public virtual void OnEvent(BossEvent eventType, object data = null) { }
}