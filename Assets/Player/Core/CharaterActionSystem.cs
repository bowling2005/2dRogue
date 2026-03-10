using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色动作管理器
/// 挂载在 Player 或 Enemy 身上
/// </summary>
public class CharacterActionSystem : MonoBehaviour
{
    [Header("Settings")]
    public int maxQueueSize = 5;

    // 动作队列 (存储 ID 和状态)
    public Queue<ActionNode> actionQueue = new Queue<ActionNode>();

    // 当前正在执行的动作节点
    public ActionNode currentActionNode = null;

    private ActionDictionary actionLibrary = new ActionDictionary();

    // 当前激活的动作逻辑实例
    private ActionBase currentActionLogic = null;


    private void Update()
    {
        float dt = Time.deltaTime;
        ProcessQueue(dt);
        UpdateCurrentAction(dt);
    }

    public void QueueAction(int actionID)
    {
        if (actionQueue.Count >= maxQueueSize) return;

        // 检查是否已禁用
        if (IsActionDisabled(actionID))
        {
            Debug.Log($"Action {actionID} is Disabled.");
            return;
        }

        ActionNode node = new ActionNode(actionID);
        node.state = ActionState.Queued;
        actionQueue.Enqueue(node);
        Debug.Log($"Queued Action: {actionID}");
    }

    public void RegisterAction(ActionBase action){actionLibrary.Add(action.ActionID, action);}

    private void ProcessQueue(float dt)
    {
        // 如果当前没有动作，尝试从队列取一个
        if (currentActionNode == null && actionQueue.Count > 0)
        {
            ActionNode nextNode = actionQueue.Peek(); // 先 peek 检查条件(牛逼，gpt还知道peek)

            ActionBase logic = actionLibrary.Get(nextNode.actionID);

            if (logic!=null)
            {
                if (logic.CheckCondition(this))
                {
                    // 检查打断
                    if (currentActionLogic != null && !currentActionLogic.CanInterrupt)
                    {
                        // 如果不能打断，则暂时不处理，等待当前动作结束
                        return;
                    }

                    // 结束旧动作
                    if (currentActionLogic != null)
                    {
                        currentActionLogic.OnExit(this);
                        currentActionNode.state = ActionState.Completed;
                        currentActionNode = null;
                        currentActionLogic = null;
                    }

                    // 开始新动作
                    actionQueue.Dequeue();
                    currentActionNode = nextNode;
                    currentActionNode.state = ActionState.Processing;
                    currentActionLogic = logic;
                    currentActionLogic.OnEnter(this);
                }
                else
                {
                    // 条件不满足，标记为禁用并丢弃
                    actionQueue.Dequeue();
                    nextNode.state = ActionState.Disabled;
                    Debug.Log($"Action {nextNode.actionID} Disabled by Condition.");
                }
            }
        }
    }

    private void UpdateCurrentAction(float dt)
    {
        if (currentActionNode != null && currentActionLogic != null)
        {
            currentActionLogic.OnUpdate(this, dt);

            // 检查是否完成
            if (currentActionLogic.IsFinished(this))
            {
                currentActionLogic.OnExit(this);
                currentActionNode.state = ActionState.Completed;
                currentActionNode = null;
                currentActionLogic = null;
            }
        }
    }

    private bool IsActionDisabled(int id)
    {
        //检测逻辑
        return false;
    }

    // --- 组件引用 ---
    public Rigidbody2D characterRigidbody2d;
    public Animator characterAnimator;

    // --- 动画控制接口 ---

    public void PlayAnimTrigger(string triggerName)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        characterAnimator?.SetTrigger(triggerName);
        Debug.Log($"[Anim] Trigger: {triggerName}");
    }

    public void SetAnimBool(string paramName, bool value)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        characterAnimator?.SetBool(paramName, value);
    }

    public void SetAnimFloat(string paramName, float value)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        characterAnimator?.SetFloat(paramName, value);
    }

    public void SetAnimInt(string paramName, int value)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        characterAnimator?.SetInteger(paramName, value);
    }

    // --- 移动接口（统一用 MoveCharacter）---

    public void MoveCharacter(Vector2 dir, float speed = 10f, bool useForce = false, bool preserveY = true)
    {
        if (characterRigidbody2d == null)
            characterRigidbody2d = GetComponent<Rigidbody2D>();

        if (characterRigidbody2d == null) return;

        Vector2 targetVelocity = dir.normalized * speed;

        if (preserveY)
            targetVelocity.y = characterRigidbody2d.velocity.y;

        if (useForce)
            characterRigidbody2d.AddForce(targetVelocity, ForceMode2D.Force);
        else
            characterRigidbody2d.velocity = targetVelocity;
    }

    // --- 辅助接口 ---

    private Transform _cachedTransform;
    public Transform GetTransform()
    {
        return _cachedTransform ??= transform;
    }

    public Vector2 GetVelocity()
    {
        return characterRigidbody2d != null ? characterRigidbody2d.velocity : Vector2.zero;
    }

    public bool IsGrounded(Transform groundCheckPoint, float checkRadius, LayerMask groundLayer)
    {
        return Physics2D.OverlapCircle(groundCheckPoint.position, checkRadius, groundLayer);
    }
}