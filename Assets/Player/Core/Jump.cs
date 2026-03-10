using UnityEngine;

public class JumpAction : ActionBase
{
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private float _airHorizontalSpeed = 3f;

    private Transform _groundCheckPoint;
    private float _checkRadius = 0.2f;
    private LayerMask _groundLayer;
    private bool _hasLeftGround;

    public JumpAction() => Init(1003, "Jump");

    public override bool CanInterrupt => false;
    public override int Priority => 2;  // 跳跃优先级最高

    // 配置接地检测参数
    public void SetGroundCheck(Transform checkPoint, float radius, LayerMask layer)
    {
        _groundCheckPoint = checkPoint;
        _checkRadius = radius;
        _groundLayer = layer;
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        base.OnEnter(owner);
        _hasLeftGround = false;

        // 跳跃核心：向上移动，preserveY=false 覆盖当前 Y 速度
        owner.MoveCharacter(Vector2.up, _jumpForce, useForce: false, preserveY: false);
        owner.PlayAnimTrigger("Jump");

        Debug.Log($"[Jump] Start | Force: {_jumpForce}");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        base.OnUpdate(owner, deltaTime);

        //  检测是否已离开地面
        if (!_hasLeftGround && !owner.IsGrounded(_groundCheckPoint, _checkRadius, _groundLayer))
        {
            _hasLeftGround = true;
        }

        // 空中水平控制（可选）
        if (_hasLeftGround)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            if (horizontal != 0f)
            {
                Vector2 moveDir = new Vector2(horizontal, 0f);
                owner.MoveCharacter(moveDir, _airHorizontalSpeed, useForce: true, preserveY: true);
            }
        }
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        //  落地即结束
        return owner.IsGrounded(_groundCheckPoint, _checkRadius, _groundLayer);
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        base.OnExit(owner);
        owner.PlayAnimTrigger("Land");
        Debug.Log("[Jump] End");
    }
}