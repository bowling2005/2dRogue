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

    // 动作库 (ID -> ActionBase 实例映射)
    // 建议在一个全局管理器中注册，这里为了演示写在本地
    private Dictionary<int, ActionBase> actionLibrary = new Dictionary<int, ActionBase>();

    // 当前激活的动作逻辑实例
    private ActionBase currentActionLogic = null;

    private void Update()
    {
        float dt = Time.deltaTime;
        ProcessQueue(dt);
        UpdateCurrentAction(dt);
    }

    /// <summary>
    /// 外部调用：添加新动作到队列
    /// </summary>
    public void QueueAction(int actionID)
    {
        if (actionQueue.Count >= maxQueueSize) return;

        // 检查是否已禁用 (可选逻辑)
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

    /// <summary>
    /// 注册动作逻辑 (游戏启动时调用)
    /// </summary>
    public void RegisterAction(ActionBase action)
    {
        if (!actionLibrary.ContainsKey(action.ActionID))
        {
            actionLibrary.Add(action.ActionID, action);
        }
    }

    private void ProcessQueue(float dt)
    {
        // 如果当前没有动作，尝试从队列取一个
        if (currentActionNode == null && actionQueue.Count > 0)
        {
            ActionNode nextNode = actionQueue.Peek(); // 先 peek 检查条件

            if (actionLibrary.TryGetValue(nextNode.actionID, out ActionBase logic))
            {
                // 检查条件
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
        // 这里可以检查全局冷却、状态锁等
        return false;
    }

    // --- 供 ActionBase 调用的辅助接口 ---
    public void PlayAnim(string animName)
    {
        // 连接你的 Animator
        // GetComponent<Animator>().Play(animName);
        Debug.Log($"[Anim] Play: {animName}");
    }

    public void MoveCharacter(Vector2 dir)
    {
        // 连接你的 CharacterController 或 Rigidbody
        Debug.Log($"[Move] Dir: {dir}");
    }

    public Transform GetTransform() => transform;
}