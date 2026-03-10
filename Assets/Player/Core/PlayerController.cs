using UnityEngine;

[RequireComponent(typeof(CharacterActionSystem))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _jumpForce = 8f;

    [Header("Action IDs")]
    [SerializeField] private int _idIdle = 1000;
    [SerializeField] private int _idWalk = 1002;
    [SerializeField] private int _idJump = 1003;
    [SerializeField] private int _idDash = 1001;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheckPoint;
    [SerializeField] private float _groundRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;  // 设为 "Ground_checkLayer"

    private CharacterActionSystem _system;
    private CharacterActionSystem System => _system ??= GetComponent<CharacterActionSystem>();

    // 输入状态
    private Vector2 _moveInput;
    private bool _jumpRequested;
    private bool _dashRequested;
    private bool _isGrounded;

    // 缓冲时间配置
    private const float BUFFER_JUMP = 0.2f;   // 跳跃缓冲 0.2 秒
    private const float BUFFER_DASH = 0.15f;  // 冲刺缓冲 0.15 秒
    private const float BUFFER_WALK = 0.1f;   // 行走缓冲 0.1 秒

    private void Awake()
    {
        _system = GetComponent<CharacterActionSystem>();
        RegisterActions();
    }

    private void RegisterActions()
    {
        System.RegisterAction(new IdleAction());
        System.RegisterAction(new WalkAction());
        System.RegisterAction(new JumpAction());
        System.RegisterAction(new DashAction());
    }

    private void Update()
    {
        CollectInput();
        UpdateGroundCheck();
        RequestActions();
    }

    private void CollectInput()
    {
        // 获取输入
        float h = Input.GetAxisRaw("Horizontal");
        _moveInput = new Vector2(h, 0f).normalized;
        if (_moveInput.x > 0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (_moveInput.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);

        // 请求标记（按键按下瞬间触发）
        _jumpRequested |= Input.GetKeyDown(KeyCode.Space);
        _dashRequested |= Input.GetKeyDown(KeyCode.L);
    }

    private void UpdateGroundCheck()
    {
        _isGrounded = System.IsGrounded(_groundCheckPoint, _groundRadius, _groundLayer);
    }

    private void RequestActions()
    {
        //  优先级：跳跃 > 冲刺 > 行走 > 待机

        //跳跃请求（支持预输入缓冲）
        if (_jumpRequested)
        {
            var ctx = new JumpContext { jumpForce = _jumpForce };
            System.QueueAction(_idJump, BUFFER_JUMP, ctx);
            _jumpRequested = false;  // 消耗请求
        }

        //冲刺请求
        if (_dashRequested)
        {
            var ctx = new DashContext { direction = _moveInput, speed = _dashSpeed };
            System.QueueAction(_idDash, BUFFER_DASH, ctx);
            _dashRequested = false;
        }

        //行走请求（持续请求，系统会去重更新）
        // 只有接地时才允许行走
        if (_isGrounded)
        {
            var ctx = new WalkContext { direction = _moveInput, speed = _walkSpeed };
            System.QueueOrUpdateAction(_idWalk, BUFFER_WALK, ctx);
        }

        //待机请求：接地 + 无输入 + 无高优先级请求时
        if (_isGrounded && _moveInput == Vector2.zero && !_jumpRequested && !_dashRequested)
        {
            System.QueueOrUpdateAction(_idIdle, 0f);  // 待机不需要缓冲
        }
    }

}

//上下文类：传递动作参数（类型安全）
public class WalkContext { public Vector2 direction; public float speed; }
public class JumpContext { public float jumpForce; }
public class DashContext { public Vector2 direction; public float speed; }