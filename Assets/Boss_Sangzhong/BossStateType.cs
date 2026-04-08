// 定义 Boss 的四种核心状态
public enum BossStateType
{
    Idle,       // 待机
    Patrol,     // 巡逻
    Discover,   // 发现 (确认目标，准备战斗)
    Fight,       // 战斗 (释放技能)
    Hurt           //受伤
}