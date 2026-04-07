using System.Collections.Generic;
using UnityEngine;

public class SkillManager
{
    private Boss _boss;
    // 这里未来会存放所有技能实例
    // private List<Skill> _skills = new List<Skill>(); 

    public SkillManager(Boss boss)
    {
        _boss = boss;
    }

    // 未来状态类会调用类似这样的方法
    public void TryCastSkill(int skillId)
    {
        // 检查冷却 -> 实例化技能 -> 播放特效
        Debug.Log($"SkillManager: 尝试释放技能 {skillId}");
    }

    public void OnUpdate()
    {
        // 更新所有技能的冷却时间
    }
}