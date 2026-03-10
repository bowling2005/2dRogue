using UnityEngine;

public class DashAction : ActionBase
{

    [SerializeField] private float _dashDuration = 0.3f;
    [SerializeField] private float _dashSpeed = 15f;
    private float _elapsedTime;
    private Vector2 _dashDirection;
    private Vector2 _inputDirection;
    private bool _hasCustomDirection;

    public DashAction()
    {
        Init(1001, "Dash");
    }

    public override bool CanInterrupt => false;  // 冲刺中不可打断
    public override int Priority => 1;           // 优先级高于普通移动

    public void SetInputDirection(Vector2 direction)
    {
        _inputDirection = direction.normalized;
        _hasCustomDirection = true;
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        base.OnEnter(owner);
        _elapsedTime = 0f;

        _dashDirection = _hasCustomDirection && _inputDirection != Vector2.zero
            ? _inputDirection
            : owner.GetTransform().right;

        owner.PlayAnimTrigger("Dash");

        Debug.Log($"[Dash] Start | Direction: {_dashDirection}");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        base.OnUpdate(owner, deltaTime);
        _elapsedTime += deltaTime;

        owner.MoveCharacter(_dashDirection, _dashSpeed, useForce: false, preserveY: true);
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        return _elapsedTime >= _dashDuration;
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        base.OnExit(owner);

        owner.SetAnimBool("IsDashing", false);

        Debug.Log("[Dash] End");
    }
}