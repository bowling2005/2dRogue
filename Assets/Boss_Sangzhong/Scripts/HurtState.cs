using UnityEngine;

public class HurtState : State
{
    public HurtState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        Debug.Log("Boss: Hurt - 受到击退！");

        _boss.ApplyVelocityDampen(0.1f);

        if (_boss.Animator != null)
        {
            _boss.Animator.SetTrigger("IsHurting");
        }
    }

    public override void OnUpdate()
    {
        // 方案 A (推荐): 等待 Animator Event 回调 Boss.OnHurtAnimationEnd()
    }

    public override void OnExit()
    {
        Debug.Log("Boss: Hurt - 恢复行动");
    }

    public override void OnEvent(BossEvent eventType, object data = null)
    {
        // 但如果收到"死亡"事件，应该可以打断受伤直接进入死亡状态

        if (eventType == BossEvent.TakeDamage)
        {
             _boss.Animator.SetTrigger("IsHurting"); 
             _boss.ApplyVelocityDampen(0.1f);
        }
    }
}