using System.Collections.Generic;
using UnityEngine;

public class SkillManager
{
    private Boss _boss;
    private Dictionary<string, Skill> _skillDict;
    private SkillDecisionMaker _decisionMaker; // 持有决策器引用

    public SkillManager(Boss boss)
    {
        _boss = boss;
        _skillDict = new Dictionary<string, Skill>();
        _decisionMaker = new SkillDecisionMaker(boss);
    }

    // 获取决策器 (供 FightState 使用)
    public SkillDecisionMaker GetDecisionMaker() => _decisionMaker;

    // 注册技能
    public void RegisterSkill(Skill skill)
    {
        if (!_skillDict.ContainsKey(skill.skillId))
        {
            _skillDict.Add(skill.skillId, skill);
            _decisionMaker.RegisterSkill(skill);
        }
    }

    // 尝试释放技能
    public bool TryCastSkill(string skillId, Transform target)
    {
        if (_skillDict.TryGetValue(skillId, out Skill skill))
        {
            if (skill.CanCast(target))
            {
                skill.OnCast(target);
                skill.StartCooldown();
                return true;
            }
        }
        return false;
    }

    // 每帧更新
    public void OnUpdate()
    {
        _decisionMaker.Update(Time.deltaTime);
    }
}