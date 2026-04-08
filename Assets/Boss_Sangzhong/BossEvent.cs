using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossEvent
{
    PlayerSpotted,    // 发现玩家
    PlayerLost,         //玩家丢失
    IntoFight,         //进入战斗
    TakeDamage,       // 受到伤害
    HealthLow,        // 低血量
    Attack_1,
    Attack_2,
    Attack_3
    
}