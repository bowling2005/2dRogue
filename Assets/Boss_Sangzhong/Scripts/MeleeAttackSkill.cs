using UnityEngine;

public class MeleeAttackSkill : Skill
{
    private float _damage = 10f;
    private string _animTrigger = "Attack_Melee";

    public MeleeAttackSkill(Boss boss) : base("Melee_01", 2.0f, 2.5f, boss) { }

    public override void OnCast(Transform target)
    {
        Debug.Log($"Skill: 释放近战攻击！目标：{target.name}");

        // 1. 播放动画
        if (owner.Animator != null)
            owner.Animator.SetTrigger(_animTrigger);

        // 2. 造成伤害 (简单示例)
        // 实际项目中这里会调用玩家的受伤接口
        // target.GetComponent<Player>()?.TakeDamage(_damage);

        // 3. 屏幕震动等效果
        // CameraShake.Instance.Shake(0.2f);
    }
}