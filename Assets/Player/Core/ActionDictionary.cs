using System.Collections.Generic;
using UnityEngine;

public class ActionDictionary
{
    private readonly Dictionary<int, ActionBase> _dictionary = new Dictionary<int, ActionBase>();

    public bool Add(int id, ActionBase action)
    {
        if (action == null)
        {
            Debug.LogWarning($"[ActionDictionary] 尝试添加 null 动作 (ID:{id})");
            return false;
        }

        bool isNew = !_dictionary.ContainsKey(id);
        _dictionary[id] = action;  // 直接赋值，支持覆盖

        return isNew;
    }
    public ActionBase Get(int id)
    {
        _dictionary.TryGetValue(id, out ActionBase action);
        return action;
    }

    /// <summary>
    /// 删除动作
    /// </summary>
    public bool Remove(int id)
    {
        return _dictionary.Remove(id);
    }

    public bool ContainsKey(int id)
    {
        return _dictionary.ContainsKey(id);
    }

    public void Clear()
    {
        _dictionary.Clear();
    }

    public int Count => _dictionary.Count;
}