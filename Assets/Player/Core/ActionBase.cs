using UnityEngine;

public abstract class ActionBase
{
    public int ActionID { get; protected set; }
    public string ActionName { get; protected set; }

    public virtual int Priority => 0;           // 优先级：越高越容易打断低优先级
    public virtual bool CanInterrupt => false;  // 执行中是否可被高优先级打断

    public virtual void Init(int id, string name)
    {
        ActionID = id;
        ActionName = name;
    }

    /// <summary>
    /// 检查执行条件（每帧检查，不满足则进入缓冲等待）
    /// </summary>
    public virtual bool CheckCondition(CharacterActionSystem owner) => true;

    /// <summary>
    /// 动作开始：条件满足，正式执行时调用
    /// </summary>
    public virtual void OnEnter(CharacterActionSystem owner) { }

    /// <summary>
    /// 动作更新：执行中每帧调用
    /// </summary>
    public virtual void OnUpdate(CharacterActionSystem owner, float deltaTime) { }

    /// <summary>
    /// 动作结束：执行完成时调用
    /// </summary>
    public virtual void OnExit(CharacterActionSystem owner) { }

    /// <summary>
    /// 判断动作是否自然结束
    /// </summary>
    public virtual bool IsFinished(CharacterActionSystem owner) => true;

    //  新增：缓冲等待期间的回调（预输入期间可播放预备动画等）
    public virtual void OnBufferUpdate(CharacterActionSystem owner, float deltaTime) { }

    /// <summary>
    /// 缓冲超时被丢弃时的回调（可选：播放失败反馈）
    /// </summary>
    public virtual void OnBufferTimeout(CharacterActionSystem owner) { }
}