using UnityEngine;
[System.Serializable]
public class ActionNode
{
    public int actionID;
    public ActionState state;
    public float createTime; // 用于超时处理等

    public ActionNode(int id)
    {
        actionID = id;
        state = ActionState.Unused;
        createTime = Time.time;
    }
}