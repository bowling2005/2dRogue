using UnityEngine;

public abstract class ActionBase
{
    public int ActionID { get; protected set; }
    public string ActionName { get; protected set; }

    public virtual int Priority => 0;
    public virtual bool CanInterrupt => false;

    public virtual void Init(int id, string name)
    {
        ActionID = id;
        ActionName = name;
    }

    public virtual bool CheckCondition(CharacterActionSystem owner)
    {
        return true;
    }

    /// 动作开始：进入 Processing 状态时调用
    /// 在此播放动画、播放音效、初始化数据
    public virtual void OnEnter(CharacterActionSystem owner) { }

    /// 动作更新：每帧调用 (在 Processing 状态下)
    /// 在此处理移动、检测结束条件
    public virtual void OnUpdate(CharacterActionSystem owner, float deltaTime) { }

    /// 动作结束：进入 Completed 状态时调用
    /// 在此清理数据、重置状态
    public virtual void OnExit(CharacterActionSystem owner) { }

    /// 判断动作是否自然结束 (由动作内部逻辑决定，如动画播完、位移结束)
    public virtual bool IsFinished(CharacterActionSystem owner)
    {
        return true;
    }
}