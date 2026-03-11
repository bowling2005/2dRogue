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

    private void ProcessQueue(float dt)
    {
        // 如果当前动作正在执行且不可打断，跳过队列处理
        if (currentActionLogic != null && !currentActionLogic.CanInterrupt)
        {
            // 但仍需要更新队列中节点的缓冲计时（预输入）
            UpdateQueueBuffer(dt);
            return;
        }

        if (actionQueue.Count == 0) return;

        Debug.Log($"[Queue] 队列检查 | 数量:{actionQueue.Count} | 当前动作:{currentActionLogic?.ActionName ?? "无"}");

        // 优先级排序：从队列中找出最高优先级的可执行动作
        ActionNode bestNode = null;
        ActionBase bestLogic = null;

        var tempQueue = new Queue<ActionNode>();
        while (actionQueue.Count > 0)
        {
            var node = actionQueue.Dequeue();
            var logic = actionLibrary.Get(node.actionID);

            if (logic != null && logic.CheckCondition(this))
            {
                if (bestLogic == null || logic.Priority > bestLogic.Priority)
                {
                    // 如果找到更高优先级，释放之前的候选节点回队列
                    if (bestNode != null) tempQueue.Enqueue(bestNode);
                    bestNode = node;
                    bestLogic = logic;
                }
                else
                {
                    tempQueue.Enqueue(node);
                }
            }
            else if (node.UpdateBuffer(dt))
            {
                // 条件不满足但仍在缓冲期内
                logic?.OnBufferUpdate(this, dt);
                tempQueue.Enqueue(node);
            }
            else
            {
                actionQueue.Dequeue();
                // 缓冲超时，丢弃
                node.state = ActionState.Disabled;
                logic?.OnBufferTimeout(this);

                Debug.Log($"[Buffer] 超时丢弃：{logic.ActionName}");
            }
        }

        // 恢复未执行的节点回队列
        while (tempQueue.Count > 0)
            actionQueue.Enqueue(tempQueue.Dequeue());

        // 执行最高优先级的动作
        if (bestNode != null && bestLogic != null)
        {
            // 打断当前动作（如果需要）
            if (currentActionLogic != null)
            {
                currentActionLogic.OnExit(this);
                currentActionNode.state = ActionState.Completed;
            }

            StartAction(bestNode, bestLogic);
        }
    }

    // 辅助：单独更新队列缓冲（当当前动作不可打断时）
    private void UpdateQueueBuffer(float dt)
    {
        var tempQueue = new Queue<ActionNode>();
        while (actionQueue.Count > 0)
        {
            var node = actionQueue.Dequeue();
            var logic = actionLibrary.Get(node.actionID);

            if (node.UpdateBuffer(dt))
            {
                logic?.OnBufferUpdate(this, dt);
                tempQueue.Enqueue(node);
            }
            else
            {
                node.state = ActionState.Disabled;
                logic?.OnBufferTimeout(this);
            }
        }
        while (tempQueue.Count > 0)
            actionQueue.Enqueue(tempQueue.Dequeue());
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
                Debug.Log($"[System] 动作结束：{currentActionLogic.ActionName}");
                currentActionLogic.OnExit(this);
                currentActionNode.state = ActionState.Completed;
                currentActionNode = null;      // ← 确保清空
                currentActionLogic = null;     // ← 确保清空
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