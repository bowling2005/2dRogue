using UnityEngine;

public enum ActionState
{
    Queued,      // 等待中（条件可能不满足）
    Processing,  // 执行中
    Completed,   // 已完成
    Disabled     // 被丢弃
}

public class ActionNode
{
    public int actionID;
    public ActionState state;

    // 缓冲相关
    public float bufferDuration;        // 最大缓冲时间（秒）
    public float elapsedBufferTime;     // 已等待时间
    public object context;              // 传递参数：如输入方向、跳跃力度等

    public ActionNode(int id, float bufferSec = 0.2f, object ctx = null)
    {
        actionID = id;
        state = ActionState.Queued;
        bufferDuration = bufferSec;
        elapsedBufferTime = 0f;
        context = ctx;
    }

    /// <summary>
    /// 更新缓冲计时
    /// </summary>
    /// <returns>true=仍在缓冲, false=超时</returns>
    public bool UpdateBuffer(float deltaTime)
    {
        elapsedBufferTime += deltaTime;
        return elapsedBufferTime < bufferDuration;
    }

    /// <summary>
    /// 获取泛型上下文（方便类型安全）
    /// </summary>
    public T GetContext<T>() where T : class
    {
        return context as T;
    }
}