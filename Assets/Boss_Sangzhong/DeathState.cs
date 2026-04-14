using UnityEngine;

public class DeathState : State
{
    public DeathState(StateMachine stateMachine, Boss boss) : base(stateMachine, boss) { }

    public override void OnEnter()
    {
        Debug.Log("Boss: Death - 死亡");
        // 1. 停止移动
        _boss.Rb.velocity = Vector2.zero;
        _boss.SetSpeed(0f);

        // 2. 播放死亡动画
        if (_boss.Animator != null)
        {
            _boss.Animator.SetTrigger("Die");
            // 禁用碰撞体防止穿模
            var colliders = _boss.GetComponents<Collider2D>();
            foreach (var c in colliders) c.enabled = false;
        }

        // 3. 可选：一段时间后销毁对象
        // GameObject.Destroy(_boss.gameObject, 3f);
    }

    public override void OnUpdate()
    {
        // 死亡状态通常不更新逻辑，除非有尸体处理逻辑
    }

    public override void OnEvent(BossEvent eventType, object data = null)
    {
        // 死亡后忽略大多数事件
    }
}