using UnityEngine;

public class DashAction : ActionBase
{
    [SerializeField] private float _defaultDuration = 0.3f;
    [SerializeField] private float _defaultSpeed = 15f;

    private float _elapsed;
    private Vector2 _direction;

    public DashAction() => Init(1001, "Dash");

    public override int Priority => 1;
    public override bool CanInterrupt => false;

    public override void OnEnter(CharacterActionSystem owner)
    {
        _elapsed = 0f;

        // 从上下文获取参数
        var ctx = owner.CurrentActionNode?.GetContext<DashContext>();
        _direction = (ctx?.direction ?? owner.GetTransform().right).normalized;
        float speed = ctx?.speed ?? _defaultSpeed;

        owner.PlayAnimTrigger("Dash");
        Debug.Log($"[Dash] Start | Dir:{_direction}");
    }

    public override void OnUpdate(CharacterActionSystem owner, float deltaTime)
    {
        _elapsed += deltaTime;
        var ctx = owner.CurrentActionNode?.GetContext<DashContext>();
        float speed = ctx?.speed ?? _defaultSpeed;

        owner.MoveCharacter(_direction, speed, useForce: false, preserveY: true);
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        var ctx = owner.CurrentActionNode?.GetContext<DashContext>();
        float duration = ctx != null ? _defaultDuration : _defaultDuration;  // 可扩展
        return _elapsed >= duration;
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        Debug.Log("[Dash] End");
    }
}