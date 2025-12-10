using UnityEngine;
using RPG.Core;

namespace RPG.Core
{
    [RequireComponent(typeof(CharacterBase))]
    public class CharacterController2D : MonoBehaviour
    {
        [Header("输入设置")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string jumpButton = "Jump";

        [Header("移动设置")]
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private float maxSpeed = 8f;
        [SerializeField] private float jumpBufferTime = 0.2f;
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("物理材质")]
        [SerializeField] private PhysicsMaterial2D fullFriction;
        [SerializeField] private PhysicsMaterial2D noFriction;

        private CharacterBase character;
        private Rigidbody2D rb;
        private Collider2D col;

        private float moveInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private float jumpBufferCounter;
        private float coyoteTimeCounter;

        private void Awake()
        {
            character = GetComponent<CharacterBase>();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
        }

        private void Update()
        {
            HandleInput();
            HandleJumpBuffer();
            HandleCoyoteTime();
            HandleStateTransitions();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleJump();
            ApplyFriction();
        }

        private void HandleInput()
        {
            moveInput = Input.GetAxisRaw(horizontalAxis);

            if (Input.GetButtonDown(jumpButton))
            {
                jumpPressed = true;
                jumpBufferCounter = jumpBufferTime;
            }

            jumpHeld = Input.GetButton(jumpButton);
        }

        private void HandleJumpBuffer()
        {
            if (jumpBufferCounter > 0)
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        private void HandleCoyoteTime()
        {
            if (character.IsGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        private void HandleStateTransitions()
        {
            var currentState = character.CurrentState;

            // 跳过死亡状态转换
            if (currentState.HasFlag(CharacterState.Dead) ||
                currentState.HasFlag(CharacterState.Damaged))
                return;

            // 跳跃/下落状态
            if (rb.velocity.y > 0.1f && !character.IsGrounded)
            {
                character.ChangeState(CharacterState.Jumping);
            }
            else if (rb.velocity.y < -0.1f && !character.IsGrounded)
            {
                character.ChangeState(CharacterState.Falling);
            }
            // 跑步/空闲状态
            else if (character.IsGrounded)
            {
                if (Mathf.Abs(rb.velocity.x) > 0.1f && Mathf.Abs(moveInput) > 0.1f)
                {
                    character.ChangeState(CharacterState.Running);
                }
                else
                {
                    character.ChangeState(CharacterState.Idle);
                }
            }
        }

        private void HandleMovement()
        {
            if (character.CurrentState.HasFlag(CharacterState.Dead)) return;

            float targetSpeed = moveInput * character.MoveSpeed;
            float speedDiff = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.5f) * Mathf.Sign(speedDiff);

            rb.AddForce(movement * Vector2.right);

            // 限制水平速度
            Vector2 velocity = rb.velocity;
            velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
            rb.velocity = velocity;

            // 翻转角色
            if (Mathf.Abs(moveInput) > 0.1f)
            {
                transform.localScale = new Vector3(
                    Mathf.Sign(moveInput) * Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z
                );
            }
        }

        private void HandleJump()
        {
            // 空中不能跳（除非有多段跳）
            if (!character.IsGrounded && character.CurrentJumpCount >= character.MaxJumpCount)
                return;

            // 检查是否在跳跃缓冲时间内且有土狼时间
            bool canJump = jumpBufferCounter > 0 && coyoteTimeCounter > 0;

            if (canJump && !character.CurrentState.HasFlag(CharacterState.Dead))
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.AddForce(Vector2.up * character.JumpForce, ForceMode2D.Impulse);

                character.CurrentJumpCount++;
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;

                // 播放跳跃动画
                character.PlayAnimation("Jump");
            }

            // 短按跳跃（可变量跳跃高度）
            if (!jumpHeld && rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            }
        }

        private void ApplyFriction()
        {
            if (character.IsGrounded && Mathf.Abs(moveInput) < 0.1f)
            {
                col.sharedMaterial = fullFriction;
            }
            else
            {
                col.sharedMaterial = noFriction;
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
}