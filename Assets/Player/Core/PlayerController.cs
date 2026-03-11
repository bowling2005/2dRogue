using UnityEngine;

[RequireComponent(typeof(CharacterActionSystem))]
public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public CharacterActionSystem actionSystem;
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Action IDs")]
    public int dashID = 1;
    public int jumpID = 2;

    private Vector2 input;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (actionSystem == null)
        {
            actionSystem = GetComponent<CharacterActionSystem>();
        }

        // 注册动作
        actionSystem.RegisterAction(new DashAction(dashID, "Dash"));
        actionSystem.RegisterAction(new JumpAction(jumpID, "Jump", jumpForce));
    }

    private void Update()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            h = -1f;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            h = 1f;
        }
        input = new Vector2(h, 0);

        // 检测地面
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        Debug.Log($"[Debug] 输入 X:{h} | 地面:{isGrounded} | 系统执行中:{actionSystem.IsProcessing}");

        // 处理输入
        HandleInput();

        // 更新基础动画（Idle/Run）
        UpdateBaseAnimation();
    }

    private void HandleInput()
    {
        // 冲刺：按 X 或 LeftShift
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            actionSystem.QueueAction(dashID, 0.15f, input);
            Debug.Log("Dash Input");
        }

        // 跳跃：按 Space 或 Z，必须在地面
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z)) && isGrounded)
        {
            actionSystem.QueueAction(jumpID, 0.2f, null);
            Debug.Log("Jump Input");
        }
    }

    private void UpdateBaseAnimation()
    {
        // 【调试】检查是否被跳过
        if (actionSystem.IsProcessing)
        {
            Debug.Log("[Debug] 跳过基础移动 - 系统正在执行动作");
            return;
        }

        Debug.Log("[Debug] 执行基础移动逻辑");

        // 移动
        if (input != Vector2.zero && isGrounded)
        {
            Debug.Log($"[Debug] 移动：速度={input.x * moveSpeed}");
            rb.velocity = new Vector2(input.x * moveSpeed, rb.velocity.y);
            anim.SetFloat("Speed", Mathf.Abs(input.x));

            // 翻转角色
            if (input.x > 0)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (input.x < 0)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
        else if (isGrounded)
        {
            Debug.Log("[Debug] 待机状态");
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat("Speed", 0);
        }
        else
        {
            Debug.Log("[Debug] 空中状态，不控制水平移动");
        }
    }

    // ============ 供 Action 调用的方法 ============

    public void PlayJumpAnim()
    {
        anim.SetTrigger("JumpTrigger");
    }

    public void PlayDashAnim()
    {
        anim.SetTrigger("DashTrigger");
    }

    public void ResetJumpBool()
    {
        anim.SetBool("IsJumping", false);
    }

    public Rigidbody2D GetRB()
    {
        return rb;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }
}