using System;
using System.Collections.Generic;

public class ActionDictionary
{
    private Dictionary<int, ActionBase> actionLibrary;

    public void Init()
    {
        actionLibrary = new Dictionary<int, ActionBase>();
    }

    public bool Add(int id, ActionBase action)
    {

        if (actionLibrary.ContainsKey(id))
        {
            //动作已存在，将覆盖原有动作
            actionLibrary[id] = action;
            return false;
        }
        actionLibrary.Add(id, action);
        return true;
    }

    public bool Delete(int id)
    {
        if (actionLibrary == null)
        {
            throw new InvalidOperationException("请先调用 Init() 初始化字典");
        }
        return actionLibrary.Remove(id);
    }
    public ActionBase Get(int id)
    {
        if (actionLibrary == null)
        {
            throw new InvalidOperationException("请先调用 Init() 初始化字典");
        }

        actionLibrary.TryGetValue(id, out ActionBase action);
        return action;
    }
    public bool ContainsKey(int id)
    {
        if (actionLibrary == null)
        {
            throw new InvalidOperationException("请先调用 Init() 初始化字典");
        }

        return actionLibrary.ContainsKey(id);
    }
    public void Clear()
    {
        if (actionLibrary != null)
        {
            actionLibrary.Clear();
        }
    }
}