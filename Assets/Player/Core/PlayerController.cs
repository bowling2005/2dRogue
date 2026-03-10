using UnityEngine;

[RequireComponent(typeof(CharacterActionSystem))]
public class PlayerController : MonoBehaviour
{
    // 配置参数
    [Header("Action IDs")]
    [SerializeField] private int _actionIdWalk = 1002;
    [SerializeField] private int _actionIdDash = 1001;
    [SerializeField] private int _actionIdJump = 1003;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheckPoint;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;  // 设置为 "Ground_checkLayer"

    // 组件缓存
    private CharacterActionSystem _actionSystem;
    private CharacterActionSystem ActionSystem => _actionSystem ??= GetComponent<CharacterActionSystem>();

    // 输入状态
    private Vector2 _moveInput;
    private bool _jumpRequested;
    private bool _dashRequested;

    // 接地状态
    private bool _isGrounded;
    public bool IsGrounded => _isGrounded;

    private void Awake()
    {
        _actionSystem = GetComponent<CharacterActionSystem>();
        RegisterActions();

        if ((_groundLayer.value & (1 << LayerMask.NameToLayer("Ground_checkLayer"))) == 0)
        {
            Debug.LogWarning("[Player] Ground Layer 未包含 'Ground_checkLayer'！");
        }
    }

    private void RegisterActions()
    {
        ActionSystem.RegisterAction(new WalkAction());
        ActionSystem.RegisterAction(new DashAction());
        ActionSystem.RegisterAction(new JumpAction());
        Debug.Log("[Player] Actions Registered");
    }

    private void Update()
    {
        CollectInput();
        UpdateGroundCheck();
        RequestActions();
        UpdateAnimationParams();
    }

    private void CollectInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        _moveInput = new Vector2(horizontal, 0f).normalized;

        
        _jumpRequested = Input.GetKeyDown(KeyCode.Space);
        _dashRequested = Input.GetKeyDown(KeyCode.L);
    }

    private void UpdateGroundCheck()
    {
        _isGrounded = ActionSystem.IsGrounded(_groundCheckPoint, _groundCheckRadius, _groundLayer);
    }

    private void RequestActions()
    {
        //优先级：跳跃 > 冲刺 > 行走

        // 跳跃（需接地）
        if (_jumpRequested && _isGrounded)
        {
            var jump = new JumpAction();
            jump.SetGroundCheck(_groundCheckPoint, _groundCheckRadius, _groundLayer);

            //通过反射或修改 JumpAction 来设置 jumpForce，这里简化处理
            ActionSystem.RegisterAction(jump);
            ActionSystem.QueueAction(_actionIdJump);

            _jumpRequested = false;
            return;
        }

        // 冲刺（需有方向）
        if (_dashRequested && _moveInput != Vector2.zero)
        {
            var dash = new DashAction();
            dash.SetInputDirection(_moveInput);
            ActionSystem.RegisterAction(dash);
            ActionSystem.QueueAction(_actionIdDash);

            _dashRequested = false;
            return;
        }

        // 行走（接地且有输入时）
        if (_isGrounded && _moveInput != Vector2.zero)
        {
                var walk = new WalkAction();
                walk.SetMoveInput(_moveInput);
                walk.SetGroundCheck(_groundCheckPoint, _groundCheckRadius, _groundLayer);
                ActionSystem.RegisterAction(walk);
                ActionSystem.QueueAction(_actionIdWalk);
        }
        // 待机
        else if (_isGrounded && _moveInput == Vector2.zero)
        {
            ActionSystem.SetAnimBool("IsMoving", false);
        }
    }

    private void UpdateAnimationParams()
    {
        // 基础状态参数
        ActionSystem.SetAnimBool("IsGrounded", _isGrounded);
        ActionSystem.SetAnimFloat("Horizontal", _moveInput.x);
        if (_moveInput.x > 0.1f)
            transform.localScale = new Vector3(1, 1, 1); // 朝右
        else if (_moveInput.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1); // 朝左
    }

}