using UnityEngine;

public abstract class Skill
{
    public string skillId;
    public float cooldown;
    public float castRange;       // 释放所需最小距离
    public float effectRange;     // 技能生效范围
    public bool isOnCooldown { get; protected set; }
    protected float cooldownTimer;
    protected Boss owner;

    public Skill(string id, float cd, float range, Boss bossOwner)
    {
        skillId = id;
        cooldown = cd;
        castRange = range;
        owner = bossOwner;
        isOnCooldown = false;
        cooldownTimer = 0f;
    }

    // 1. 检查是否可释放 (冷却、距离、目标有效性)
    public virtual bool CanCast(Transform target)
    {
        if (isOnCooldown) return false;
        if (target == null) return false;

        float dist = Vector2.Distance(owner.Transform.position, target.position);
        // 距离必须在释放范围内 (允许一点误差)
        if (dist > castRange + 1f) return false;

        return true;
    }

    // 2. 执行技能效果 (子类重写)
    public abstract void OnCast(Transform target);

    // 3. 更新冷却 (由 SkillManager 调用)
    public void UpdateCooldown(float delta)
    {
        if (isOnCooldown)
        {
            cooldownTimer -= delta;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                OnCooldownEnd();
            }
        }
    }

    // 4. 启动冷却
    public void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = cooldown;
        OnCooldownStart();
    }

    protected virtual void OnCooldownStart() { }
    protected virtual void OnCooldownEnd() { }
}