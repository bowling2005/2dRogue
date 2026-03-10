using UnityEngine;

public class IdleAction : ActionBase
{
    public IdleAction() => Init(1000, "Idle");

    public override int Priority => 0;
    public override bool CanInterrupt => true;  // 待机可被任何动作打断

    public override bool CheckCondition(CharacterActionSystem owner)
    {
        // 接地才能待机
        return true;  // 接地检查由 PlayerController 控制入队
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        owner.SetAnimBool("IsMoving", false);
        Debug.Log("[Idle] Start");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        // 待机不移动，只保持动画状态
    }

    // 被高优先级动作打断时自动结束
    public override bool IsFinished(CharacterActionSystem owner) => false;
}