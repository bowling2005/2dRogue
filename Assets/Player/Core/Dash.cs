using UnityEngine;

public class DashAction : ActionBase
{
    private float duration = 0.25f;
    private float speed = 25f;
    private float elapsed = 0f;
    private Vector2 direction = Vector2.right;

    public override int Priority => 100;
    public override bool CanInterrupt => false;

    public DashAction(int id, string name)
    {
        Init(id, name);
    }

    public override bool CheckCondition(CharacterActionSystem owner)
    {
        return true;
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        // 获取玩家
        PlayerController player = owner.GetComponent<PlayerController>();
        if (player == null) return;

        // 获取方向：从 context 强转，没有就用默认
        ActionNode node = owner.CurrentActionNode;
        if (node != null && node.context != null)
        {
            direction = (Vector2)node.context;
        }
        else
        {
            direction = player.transform.right;
        }

        // 方向归一化
        direction = direction.normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        // 播放动画
        player.PlayDashAnim();

        // 设置速度
        Rigidbody2D rb = player.GetRB();
        if (rb != null)
        {
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        }

        elapsed = 0f;
        Debug.Log("Dash Start");
    }

    public override void OnUpdate(CharacterActionSystem owner, float dt)
    {
        elapsed += dt;

        PlayerController player = owner.GetComponent<PlayerController>();
        Rigidbody2D rb = player?.GetRB();

        if (rb != null)
        {
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        }
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        return elapsed >= duration;
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        PlayerController player = owner.GetComponent<PlayerController>();
        Rigidbody2D rb = player?.GetRB();

        if (rb != null)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        Debug.Log("Dash End");
    }
}