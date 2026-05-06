# Boss_Sangzhong 代码架构解析

## 概述

`Boss_Sangzhong` 是一个基于 **状态机模式 (State Machine Pattern)** 和 **决策系统 (Decision System)** 的 Unity Boss AI 框架。该设计实现了模块化、可扩展的 Boss 行为控制，支持多种状态切换、技能释放决策和移动决策。

---

## 核心架构

### 1. 状态机系统 (State Machine)

#### 架构图
```
┌─────────────┐
│    Boss     │ ──── 挂载在游戏物体上的主控制器
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ StateMachine│ ──── 状态管理器，管理所有状态的切换
└──────┬──────┘
       │
       ├──► IdleState      (待机状态)
       ├──► PatrolState    (巡逻状态)
       ├──► DiscoverState  (发现玩家状态)
       ├──► FightState     (战斗状态)
       ├──► HurtState      (受伤状态)
       └──► DeathState     (死亡状态)
```

#### 关键类说明

**StateMachine.cs** - 状态机核心
- `_states`: 字典存储所有状态实例，通过枚举快速查找
- `CurrentState`: 当前激活的状态
- `Initialize()`: 实例化所有状态并设置初始状态
- `ChangeState()`: 核心状态切换逻辑，支持受伤中断机制
- `HandleEvent()`: 将事件转发给当前状态处理

**State.cs** - 状态基类
- 抽象类，定义所有状态的统一接口
- `OnEnter()`: 进入状态时调用（初始化属性、播放动画）
- `OnUpdate()`: 每帧更新（处理移动、逻辑判断）
- `OnExit()`: 退出状态时调用（清理资源、还原属性）
- `OnEvent()`: 处理事件（如受伤、玩家发现）

---

### 2. Boss 主控制器

**Boss.cs** - Boss 游戏物体的主脚本

#### 核心职责
1. **模块引用管理**: 提供 `StateMachine`、`SkillManager`、`PlayerDetector` 等模块的访问接口
2. **Unity 组件封装**: 暴露 `Animator`、`Rigidbody2D`、`Transform` 供状态类使用
3. **基础属性管理**: 血量、速度、攻击速度等
4. **事件入口**: 
   - `OnPlayerSpotted()`: 玩家被检测到，累计计数达到阈值后进入战斗
   - `OnPlayerLost()`: 玩家离开检测范围
   - `TakeDamage()`: 受到伤害，触发受伤状态或死亡

#### 特色设计
- **速度修正器**: `_speedModifier` 允许状态临时修改速度，退出时还原
- **巡逻系统**: 支持多个巡逻点往返巡逻
- **面向控制**: `CheckFlipSprite()` 根据速度方向自动翻转精灵
- **调试 GUI**: `OnGUI()` 在编辑器中显示当前状态

---

### 3. 技能决策系统

#### 架构图
```
┌──────────────┐
│ SkillManager │ ──── 技能注册、冷却管理、决策执行
└──────┬───────┘
       │
       ▼
┌───────────────────┐
│SkillDecisionMaker │ ──── 基于影响因子的技能选择算法
└──────┬────────────┘
       │
       ├──► InfluenceFactor (影响因子基类)
       │    ├── HealthLossFactor (血量越低越倾向远程)
       │    └── DistanceFactor (距离越近越倾向近战)
       │
       └──► Skill (技能基类)
            ├── MeleeAttackSkill (近战技能)
            └── ProjectileSkill (远程技能)
```

#### 核心机制

**SkillDecisionMaker.cs** - 技能决策器
- **影响因子评分**: 每个因子根据当前环境计算 0~1 的分数，乘以技能权重
- **加权求和**: 所有因子分数累加得到技能总分
- **Top-2 随机**: 从得分前 2 的技能中随机选择一个，增加行为多样性
- **冻结机制**: 
  - `Freeze()`: 暂停决策冷却（技能动画播放时）
  - `Unfreeze()`: 恢复决策冷却
  - `ResetDecisionTimer()`: 立即允许重新决策

**InfluenceFactor.cs** - 影响因子基类
```csharp
public abstract float CalculateScore(Boss boss, PlayerDetector detector);
```
子类重写此方法，根据特定条件（血量、距离等）返回 0~1 的激活程度。

**示例配置** (在 Boss.Awake 中):
```csharp
// 血量越低，越倾向于远程 (权重：Melee 0.3, Range 0.8)
float[] healthWeights = new float[] { 0.3f, 0.8f };
SkillManager.GetDecisionMaker().AddFactor(new HealthLossFactor(healthWeights));

// 距离越近，越倾向于近战 (权重：Melee 0.9, Range 0.2)
float[] distWeights = new float[] { 0.9f, 0.2f };
SkillManager.GetDecisionMaker().AddFactor(new DistanceFactor(distWeights));
```

---

### 4. 移动决策系统

**MovementDecisionMaker.cs** - 移动决策器
- **决策命令**: `Towards`(接近)、`Away`(远离)、`Idle`(静止)
- **影响因子**: 类似技能决策，但只计算两个方向的分数
- **冻结机制**: 与技能决策器同步冻结/解冻

**使用示例** (FightState 中):
```csharp
if (_moveDM.TryDecide(_boss.PlayerDetector))
{
    HandleMovementCommand(_moveDM.CurrentCommand);
}
```

---

### 5. 玩家检测系统

**PlayerDetector.cs** - 玩家检测器
- **触发器检测**: 使用 `BoxCollider2D` 作为触发器检测玩家
- **观察计数机制**: 
  - 玩家进入触发器后开始计时
  - 每隔 `observeInterval` 秒调用 `OnPlayerSpotted()`
  - 累计达到 `spotThreshold` 次后进入战斗状态
- **动态范围调整**:
  - `ExpandDetectionRange(multiplier)`: 战斗时扩大检测范围
  - `RestoreDetectionRange()`: 退出战斗时恢复
- **方向自适应**: 检测区域随 Boss 面向自动偏移

---

## 状态流转图

```
                    ┌─────────────┐
                    │   Idle      │
                    │  (待机)     │
                    └──────┬──────┘
                           │ 等待随机时间
                           ▼
                    ┌─────────────┐
         ┌─────────►│   Patrol    │◄─────────┐
         │          │  (巡逻)     │          │ 到达巡逻点
         │          └──────┬──────┘          │
         │                 │ 发现玩家        │
         │                 ▼                 │
         │          ┌─────────────┐          │
         │          │  Discover   │          │
         │          │  (警戒)     │          │
         │          └──────┬──────┘          │
         │                 │ 满足战斗条件    │
         │                 ▼                 │
         │          ┌─────────────┐          │
         │◄────────►│    Fight    │──────────┘
         │          │  (战斗)     │  玩家丢失
         │          └──────┬──────┘
         │                 │ 受到伤害
         │                 ▼
         │          ┌─────────────┐
         └─────────►│    Hurt     │
                    │  (受伤)     │
                    └──────┬──────┘
                           │ 血量归零
                           ▼
                    ┌─────────────┐
                    │   Death     │
                    │  (死亡)     │
                    └─────────────┘
```

---

## 关键设计模式

### 1. 状态模式 (State Pattern)
- 每个状态封装特定行为的实现
- 状态切换通过 `StateMachine.ChangeState()` 统一管理
- 支持状态中断（受伤可打断任何状态）

### 2. 依赖注入 (Dependency Injection)
- 状态类通过构造函数接收 `StateMachine` 和 `Boss` 引用
- 避免全局单例，提高可测试性

### 3. 策略模式 (Strategy Pattern)
- `InfluenceFactor` 和 `MovementFactor` 作为可插拔的策略
- 易于扩展新的决策因素（如添加"玩家血量低时倾向追击"）

### 4. 观察者模式 (Observer Pattern)
- `PlayerDetector` 通过触发器事件通知 `Boss`
- `Boss` 通过 `HandleEvent()` 将事件分发给当前状态

---

## 战斗流程详解

### FightState 子状态机
```
┌──────────────────────────────────────────────┐
│              FightState                      │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐  │
│  │  Idle   │───►│ Seeking │───►│ Casting │  │
│  │ (空闲)  │    │ (定位)  │    │ (释放)  │  │
│  └─────────┘    └─────────┘    └─────────┘  │
│       ▲                              │       │
│       └──────────────────────────────┘       │
│              技能释放完成                     │
└──────────────────────────────────────────────┘
```

### 决策优先级
1. **动作锁检查**: 如果技能动画正在播放，冻结所有决策器
2. **技能执行**: 如果已有选中的技能，优先执行（定位→释放）
3. **技能决策**: 尝试选择新技能（高优先级）
4. **移动决策**: 技能冷却时执行移动

### 动作锁机制
```csharp
private bool CheckAnimationLock()
{
    if (_boss.Animator.GetBool("IsActing"))
    {
        _isActionLocked = true;
        return true; // 锁定中
    }
    _isActionLocked = false;
    return false; // 解锁
}
```
- 技能释放时设置 Animator 的 `IsActing` 参数为 true
- 动画播放期间禁止决策，防止干扰
- 动画结束通过 `OnActionEnd()` 回调重置参数

---

## 扩展指南

### 添加新状态
1. 继承 `State` 类
2. 重写 `OnEnter()`、`OnUpdate()`、`OnExit()`、`OnEvent()`
3. 在 `BossStateType.cs` 添加新枚举
4. 在 `StateMachine.Initialize()` 注册新状态

### 添加新技能
1. 继承 `Skill` 类
2. 实现 `OnCast()` 方法
3. 在 `Boss.Awake()` 中注册技能
4. 配置影响因子权重

### 添加新影响因子
1. 继承 `InfluenceFactor` 或 `MovementFactor`
2. 实现 `CalculateScore()` 方法
3. 在 `Boss.Awake()` 中添加到决策器

---

## 总结

`Boss_Sangzhong` 是一个设计精良的 Boss AI 框架，具有以下特点：

✅ **高度模块化**: 状态、技能、决策、检测完全解耦  
✅ **易于扩展**: 通过继承基类即可添加新行为  
✅ **行为多样**: 加权评分 + Top-2 随机避免重复模式  
✅ **中断友好**: 受伤状态可无缝打断任何行为  
✅ **调试方便**: 内置 GUI 显示和详细日志  

该架构适用于各种需要复杂行为树的 Boss 战场景，是 Unity 2D 动作游戏的优秀参考实现。
