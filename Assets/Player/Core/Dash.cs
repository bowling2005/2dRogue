using UnityEngine;

// 1. 定义新动作类
public class DashAction : ActionBase
{
    private float dashDuration = 0.3f;
    private float timer = 0f;
    private Vector2 dashDir;

    public DashAction()
    {
        // 在构造函数或 Init 中设定 ID
        // 建议定义一个全局 ActionID 枚举来管理这些数字
        Init(1001, "Dash");
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        base.OnEnter(owner);
        timer = 0f;
        // 获取输入方向或角色朝向
        dashDir = owner.GetTransform().right;
        owner.PlayAnim("Dash");
        Debug.Log("Dash Start!");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        base.OnUpdate(owner, deltaTime);
        timer += deltaTime;
        // 执行移动逻辑
        owner.MoveCharacter(dashDir * 10f);
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        // 时间到了就结束
        return timer >= dashDuration;
    }

    public override bool CheckCondition(CharacterActionSystem owner)
    {
        // 例如：只有在地面才能冲刺
        // return owner.IsGrounded; 
        return true;
    }
}