using UnityEngine;

public class JumpAction : ActionBase
{
    private float jumpForce = 10f;

    public override int Priority => 80;
    public override bool CanInterrupt => false;

    public JumpAction(int id, string name, float force)
    {
        Init(id, name);
        jumpForce = force;
    }

    public override bool CheckCondition(CharacterActionSystem owner)
    {
        PlayerController player = owner.GetComponent<PlayerController>();
        if (player == null) return false;

        return player.IsGrounded();
    }

    public override void OnEnter(CharacterActionSystem owner)
    {
        PlayerController player = owner.GetComponent<PlayerController>();
        if (player == null) return;

        player.PlayJumpAnim();

        Rigidbody2D rb = player.GetRB();
        if (rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        Debug.Log("Jump Start");
    }

    public override bool IsFinished(CharacterActionSystem owner)
    {
        PlayerController player = owner.GetComponent<PlayerController>();
        if (player == null) return true;

        return player.IsGrounded();
    }

    public override void OnExit(CharacterActionSystem owner)
    {
        PlayerController player = owner.GetComponent<PlayerController>();
        player?.ResetJumpBool();

        Debug.Log("Jump End");
    }
}