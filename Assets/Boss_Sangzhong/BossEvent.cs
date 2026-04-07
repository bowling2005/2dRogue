using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossEvent
{
    PlayerSpotted,    // 发现玩家
    PlayerLost,       // 丢失玩家
    TakeDamage,       // 受到伤害
    HealthLow,        // 低血量
    SkillCooldownEnd  // 技能冷却结束
}