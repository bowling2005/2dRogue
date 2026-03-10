using UnityEngine;

public class WalkAction : ActionBase
{
    [SerializeField] private float _defaultSpeed = 5f;

    public WalkAction() => Init(1002, "Walk");

    public override int Priority => 0;
    public override bool CanInterrupt => true;  // 行走可被跳跃/冲刺打断

    public override bool CheckCondition(CharacterActionSystem owner)
    {
        // 接地才能行走（由上层控制入队，这里做兜底）
        return true;
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        UpdateMovement(owner, 0f);  // 初始更新
        Debug.Log("[Walk] Start");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        UpdateMovement(owner, deltaTime);
    }

    public override void OnBufferUpdate(CharacterActionSystem owner, float deltaTime)
    {
        // 缓冲期间：可以播放"预备移动"动画或音效
        // owner.SetAnimBool("IsMoving", true);  // 可选
    }

    private void UpdateMovement(CharacterActionSystem owner, float dt)
    {
        // 从上下文获取动态输入（支持预输入后方向变化）
        var ctx = owner.CurrentActionNode?.GetContext<WalkContext>();
        Vector2 dir = ctx?.direction ?? Vector2.zero;
        float speed = ctx?.speed ?? _defaultSpeed;

        if (dir != Vector2.zero)
        {
            owner.MoveCharacter(dir, speed, useForce: true, preserveY: true);
            owner.SetAnimBool("IsMoving", true);
            owner.SetAnimFloat("Horizontal", dir.x);
        }
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        // 输入为 0 或 不接地时结束（交还给调度系统）
        var ctx = owner.CurrentActionNode?.GetContext<WalkContext>();
        return (ctx?.direction ?? Vector2.zero) == Vector2.zero;
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        owner.SetAnimBool("IsMoving", false);
        Debug.Log("[Walk] End");
    }
}