using UnityEngine;

public class WalkAction : ActionBase
{
    [SerializeField] private float _walkSpeed = 5f;

    private Vector2 _moveInput;
    private Transform _groundCheckPoint;
    private float _checkRadius = 0.2f;
    private LayerMask _groundLayer;

    public WalkAction() => Init(1002, "Walk");

    public override bool CanInterrupt => true;   // 行走可被冲刺/跳跃打断
    public override int Priority => 0;           // 最低优先级

    public void SetMoveInput(Vector2 input) => _moveInput = input;
    public void SetGroundCheck(Transform checkPoint, float radius, LayerMask layer)
    {
        _groundCheckPoint = checkPoint;
        _checkRadius = radius;
        _groundLayer = layer;
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        base.OnEnter(owner);
        owner.SetAnimBool("IsMoving", _moveInput != Vector2.zero);
        Debug.Log($"[Walk] Start | Input: {_moveInput}");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        base.OnUpdate(owner, deltaTime);

        if (_moveInput != Vector2.zero)
        {
            owner.MoveCharacter(_moveInput, _walkSpeed, useForce: true, preserveY: true);

            owner.SetAnimFloat("Horizontal", _moveInput.x);
            owner.SetAnimFloat("Speed", Mathf.Abs(_moveInput.x) * _walkSpeed);
        }

        if (_moveInput == Vector2.zero)
        {
            owner.SetAnimBool("IsMoving", false);
        }
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        // 输入为 0 或 不接地时结束（空中由 JumpAction 接管）
        return _moveInput == Vector2.zero || !owner.IsGrounded(_groundCheckPoint, _checkRadius, _groundLayer);
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        base.OnExit(owner);
        owner.SetAnimBool("IsMoving", false);
        Debug.Log("[Walk] End");
    }
}