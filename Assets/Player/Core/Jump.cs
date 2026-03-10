using UnityEngine;

public class JumpAction : ActionBase
{
    [SerializeField] private float _defaultJumpForce = 8f;
    [SerializeField] private float _airControl = 3f;

    private bool _hasLeftGround;

    public JumpAction() => Init(1003, "Jump");

    public override int Priority => 2;  // 高优先级
    public override bool CanInterrupt => false;  // 跳跃中不可打断

    public override bool CheckCondition(CharacterActionSystem owner)
    {
        // 必须接地才能起跳
        return owner.IsGrounded(
            owner.GetTransform().Find("GroundCheck") ?? owner.GetTransform(),
            0.2f,
            LayerMask.GetMask("Ground_checkLayer")
        );
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        _hasLeftGround = false;

        // 执行跳跃：覆盖 Y 速度
        var ctx = owner.CurrentActionNode?.GetContext<JumpContext>();
        float force = ctx?.jumpForce ?? _defaultJumpForce;

        owner.MoveCharacter(Vector2.up, force, useForce: false, preserveY: false);
        owner.PlayAnimTrigger("Jump");

        Debug.Log($"[Jump] Start | Force:{force}");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        // 检测是否已离开地面
        if (!_hasLeftGround)
        {
            var checkPoint = owner.GetTransform().Find("GroundCheck") ?? owner.GetTransform();
            if (!owner.IsGrounded(checkPoint, 0.2f, LayerMask.GetMask("Ground_checkLayer")))
            {
                _hasLeftGround = true;
            }
        }

        // 空中水平控制（可选）
        if (_hasLeftGround)
        {
            float h = Input.GetAxisRaw("Horizontal");
            if (h != 0f)
            {
                owner.MoveCharacter(new Vector2(h, 0f), _airControl, useForce: true, preserveY: true);
            }
        }
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        // 落地即结束
        var checkPoint = owner.GetTransform().Find("GroundCheck") ?? owner.GetTransform();
        return owner.IsGrounded(checkPoint, 0.2f, LayerMask.GetMask("Ground_checkLayer"));
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        owner.PlayAnimTrigger("Land");
        Debug.Log("[Jump] End");
    }

    public override void OnBufferUpdate(CharacterActionSystem owner, float deltaTime)
    {
        // 缓冲期间：播放"预备跳跃"动画（可选）
        // owner.PlayAnimTrigger("JumpPrep");
    }
}