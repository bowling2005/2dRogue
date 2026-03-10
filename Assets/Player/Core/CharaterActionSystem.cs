using System.Collections.Generic;
using UnityEngine;

public class CharacterActionSystem : MonoBehaviour
{
    [Header("Settings")]
    public int maxQueueSize = 5;
    public float defaultBufferTime = 0.2f;  // 默认缓冲时间

    public Queue<ActionNode> actionQueue = new Queue<ActionNode>();
    public ActionNode currentActionNode = null;
    private ActionDictionary actionLibrary = new ActionDictionary();
    private ActionBase currentActionLogic = null;

    public Rigidbody2D characterRigidbody2d;
    public Animator characterAnimator;

    private void Awake()
    {
        if (actionLibrary == null)
            actionLibrary = new ActionDictionary();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        ProcessQueue(dt);     
        UpdateCurrentAction(dt); // 再更新当前动作
    }
    public void QueueAction(int actionID)
    {
        QueueAction(actionID, defaultBufferTime);
    }

    public void QueueAction(int actionID, float bufferSec, object context = null)
    {
        if (actionQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning($"[ActionSystem] 队列已满，丢弃 Action {actionID}");
            return;
        }

        if (IsActionDisabled(actionID))
        {
            Debug.Log($"[ActionSystem] Action {actionID} 被禁用");
            return;
        }

        ActionNode node = new ActionNode(actionID, bufferSec, context);
        actionQueue.Enqueue(node);
        Debug.Log($"[ActionSystem] 入队: {actionID} (缓冲:{bufferSec}s)");
    }

    public void QueueOrUpdateAction(int actionID, float bufferSec = 0.2f, object context = null)
    {
        // 检查队列中是否已有相同 ID 的节点
        foreach (var node in actionQueue)
        {
            if (node.actionID == actionID && node.state == ActionState.Queued)
            {
                // 更新上下文和缓冲计时
                node.context = context;
                node.elapsedBufferTime = 0f;  // 重置缓冲
                Debug.Log($"[ActionSystem] 更新已有请求: {actionID}");
                return;
            }
        }
        // 没有则正常入队
        QueueAction(actionID, bufferSec, context);
    }

    public void RegisterAction(ActionBase action)
    {
        if (action == null) return;
        actionLibrary.Add(action.ActionID, action);
        Debug.Log($"[ActionSystem] 已注册: {action.ActionName}(ID:{action.ActionID})");
    }

    // 核心：处理队列（支持缓冲等待）
    private void ProcessQueue(float dt)
    {
        // 如果当前没有动作，尝试从队列取一个执行
        if (currentActionNode == null && actionQueue.Count > 0)
        {
            ActionNode nextNode = actionQueue.Peek();
            ActionBase logic = actionLibrary.Get(nextNode.actionID);

            if (logic == null)
            {
                // 找不到动作逻辑，直接丢弃
                actionQueue.Dequeue();
                Debug.LogWarning($"[ActionSystem] 未找到 Action ID:{nextNode.actionID}");
                return;
            }

            // 检查执行条件
            if (logic.CheckCondition(this))
            {
                // 条件满足：检查打断逻辑
                if (currentActionLogic != null && !currentActionLogic.CanInterrupt)
                {
                    // 当前动作不可打断，等待其结束
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
                StartAction(nextNode, logic);
            }
            else
            {
                // 条件不满足：进入缓冲等待
                if (nextNode.UpdateBuffer(dt))
                {
                    // 仍在缓冲期内，调用回调
                    logic.OnBufferUpdate(this, dt);
                    // 不 dequeue，下一帧继续检查
                }
                else
                {
                    // 缓冲超时，丢弃动作
                    actionQueue.Dequeue();
                    nextNode.state = ActionState.Disabled;
                    logic.OnBufferTimeout(this);
                    Debug.Log($"[ActionSystem] Action {nextNode.actionID} 缓冲超时");
                }
            }
        }
    }

    // 提取：开始执行动作的公共方法
    private void StartAction(ActionNode node, ActionBase logic)
    {
        node.state = ActionState.Processing;
        currentActionNode = node;
        currentActionLogic = logic;
        currentActionLogic.OnEnter(this);
        Debug.Log($"[ActionSystem] 开始执行: {logic.ActionName}");
    }

    private void UpdateCurrentAction(float dt)
    {
        if (currentActionNode != null && currentActionLogic != null)
        {
            currentActionLogic.OnUpdate(this, dt);

            if (currentActionLogic.IsFinished(this))
            {
                currentActionLogic.OnExit(this);
                currentActionNode.state = ActionState.Completed;
                currentActionNode = null;
                currentActionLogic = null;
            }
        }
    }

    private bool IsActionDisabled(int id) => false;  // 可扩展：全局冷却/状态锁

    // --- 动画接口 ---
    public void PlayAnimTrigger(string triggerName)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
        characterAnimator?.SetTrigger(triggerName);
    }

    public void SetAnimBool(string name, bool value)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
        characterAnimator?.SetBool(name, value);
    }

    public void SetAnimFloat(string name, float value)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
        characterAnimator?.SetFloat(name, value);
    }

    public void SetAnimInt(string name, int value)
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
        characterAnimator?.SetInteger(name, value);
    }

    // --- 移动接口 ---
    public void MoveCharacter(Vector2 dir, float speed = 10f, bool useForce = false, bool preserveY = true)
    {
        if (characterRigidbody2d == null)
            characterRigidbody2d = GetComponent<Rigidbody2D>();
        if (characterRigidbody2d == null) return;

        Vector2 targetVelocity = dir.normalized * speed;
        if (preserveY) targetVelocity.y = characterRigidbody2d.velocity.y;

        if (useForce)
            characterRigidbody2d.AddForce(targetVelocity, ForceMode2D.Force);
        else
            characterRigidbody2d.velocity = targetVelocity;
    }

    // --- 辅助接口 ---
    private Transform _cachedTransform;
    public Transform GetTransform() => _cachedTransform ??= transform;

    public Vector2 GetVelocity() => characterRigidbody2d?.velocity ?? Vector2.zero;

    public bool IsGrounded(Transform checkPoint, float radius, LayerMask layer)
    {
        return Physics2D.OverlapCircle(checkPoint.position, radius, layer);
    }

    // 供 Action 查询当前队列状态（可选）
    public ActionNode CurrentActionNode => currentActionNode;
    public ActionBase CurrentActionLogic => currentActionLogic;
    public bool IsProcessing => currentActionLogic != null;
}