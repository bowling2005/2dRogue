using System.Collections.Generic;
using UnityEngine;

public enum ActionType { Idle, Run, Jump, Climb, Fall }

public struct PathAction
{
    public Vector2 targetPos;
    public ActionType action;
}

public class BossAutoPathfinding : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Boss boss;
    [SerializeField] private BoxCollider2D selfCollider;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Movement")]
    [SerializeField] private float replanInterval = 0.5f;
    [SerializeField] private float rayDistance = 0.4f;
    [SerializeField] private float groundExtra = 0.1f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float climbSpeedY = 2.5f;

    private readonly Queue<PathAction> _actionQueue = new Queue<PathAction>();
    private PathAction _currentAction;
    private Vector2 _finalTarget;
    private float _moveSpeed;
    private float _replanTimer;
    private ActionType _currentState = ActionType.Idle;

    private void Awake()
    {
        if (boss == null)
        {
            boss = GetComponent<Boss>();
        }

        if (selfCollider == null)
        {
            selfCollider = GetComponent<BoxCollider2D>();
        }
    }

    public void MoveTowardsTarget(Vector2 targetPos, float speed)
    {
        _finalTarget = targetPos;
        _moveSpeed = speed;
        ForceReplan();
    }

    public void MoveAwayFromTarget(Vector2 targetPos, float speed)
    {
        _finalTarget = (Vector2)transform.position * 2f - targetPos;
        _moveSpeed = speed;
        ForceReplan();
    }

    private void Update()
    {
        if (boss?.Rb == null || selfCollider == null)
        {
            return;
        }

        UpdateReplanTimer();
        ExecuteCurrentAction();
        SyncAnimator();
    }

    private void UpdateReplanTimer()
    {
        _replanTimer -= Time.deltaTime;
        if (_replanTimer > 0f)
        {
            return;
        }

        ReplanQueue();
        _replanTimer = replanInterval;
    }

    private void ForceReplan()
    {
        _replanTimer = 0f;
        ReplanQueue();
    }

    private void ReplanQueue()
    {
        _actionQueue.Clear();

        Vector2 position = transform.position;
        float direction = Mathf.Sign(_finalTarget.x - position.x);
        if (Mathf.Abs(direction) < 0.01f)
        {
            EnqueueAction(position, ActionType.Idle);
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.right * direction, rayDistance * 4f, obstacleLayers);
        if (hit.collider == null)
        {
            EnqueueAction(_finalTarget, ActionType.Run);
            return;
        }

        float obstacleHeight = GetColliderHeight(hit.collider);
        float bossHeight = selfCollider.size.y * Mathf.Abs(transform.localScale.y);
        float hitX = hit.point.x;

        EnqueueAction(new Vector2(hitX - direction * 0.15f, position.y), ActionType.Run);

        if (obstacleHeight <= bossHeight * 0.5f)
        {
            EnqueueAction(new Vector2(hitX, position.y), ActionType.Jump);
        }
        else if (obstacleHeight <= bossHeight * 1.5f)
        {
            EnqueueAction(new Vector2(hitX, position.y + obstacleHeight), ActionType.Climb);
        }
        else
        {
            EnqueueAction(new Vector2(hitX - direction * 0.3f, position.y), ActionType.Idle);
        }

        EnqueueAction(new Vector2(_finalTarget.x, position.y), ActionType.Run);
    }

    private void EnqueueAction(Vector2 targetPos, ActionType action)
    {
        _actionQueue.Enqueue(new PathAction { targetPos = targetPos, action = action });
        _currentAction = _actionQueue.Peek();
    }

    private void ExecuteCurrentAction()
    {
        if (_actionQueue.Count == 0)
        {
            _currentState = ActionType.Idle;
            return;
        }

        Vector2 position = transform.position;
        float direction = Mathf.Sign(_finalTarget.x - position.x);
        bool onGround = CheckGround();

        if (HasReachedCurrentAction(position, direction))
        {
            _actionQueue.Dequeue();
            if (_actionQueue.Count == 0)
            {
                _currentState = ActionType.Idle;
                return;
            }

            _currentAction = _actionQueue.Peek();
        }

        _currentState = onGround ? _currentAction.action : ActionType.Fall;
        ApplyVelocity(direction, onGround);
    }

    private bool HasReachedCurrentAction(Vector2 position, float direction)
    {
        return (direction > 0f && position.x >= _currentAction.targetPos.x) ||
               (direction < 0f && position.x <= _currentAction.targetPos.x) ||
               Vector2.Distance(position, _currentAction.targetPos) < 0.15f;
    }

    private void ApplyVelocity(float direction, bool onGround)
    {
        Vector2 velocity = boss.Rb.velocity;

        switch (_currentState)
        {
            case ActionType.Run:
                velocity.x = direction * _moveSpeed;
                break;
            case ActionType.Jump:
                if (onGround && velocity.y <= 0.1f)
                {
                    velocity.y = jumpForce;
                }

                velocity.x = direction * _moveSpeed;
                break;
            case ActionType.Climb:
                velocity.x = direction * _moveSpeed * 0.6f;
                velocity.y = climbSpeedY;
                break;
            default:
                velocity.x = Mathf.Lerp(velocity.x, 0f, Time.deltaTime * 8f);
                break;
        }

        boss.Rb.velocity = velocity;
    }

    private bool CheckGround()
    {
        float distance = selfCollider.size.y * 0.5f * Mathf.Abs(transform.localScale.y) + groundExtra;
        return Physics2D.Raycast(transform.position, Vector2.down, distance, obstacleLayers);
    }

    private float GetColliderHeight(Collider2D col)
    {
        if (col is BoxCollider2D box)
        {
            return box.size.y * Mathf.Abs(col.transform.localScale.y);
        }

        return col.bounds.size.y;
    }

    private void SyncAnimator()
    {
        if (boss?.Animator == null)
        {
            return;
        }

        boss.Animator.SetBool("isFalling", _currentState == ActionType.Fall);
        boss.Animator.SetBool("isRunning", _currentState == ActionType.Run);
        boss.Animator.SetBool("isClimbing", _currentState == ActionType.Climb);
        boss.Animator.SetBool("isJumping", _currentState == ActionType.Jump);
        boss.Animator.SetFloat("speed", Mathf.Abs(boss.Rb.velocity.x));
    }
}
