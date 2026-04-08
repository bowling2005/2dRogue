using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightState : State { public FightState(StateMachine sm, Boss b) : base(sm, b) { } }

//记得加入
//if (eventType == BossEvent.TakeDamage)
//{
//    _stateMachine.ChangeState(BossStateType.Hurt);
//    return;
//}
