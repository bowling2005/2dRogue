using System.Collections.Generic;
using UnityEngine;

public class SkillDecisionMaker
{
    private List<InfluenceFactor> _factors;
    private List<Skill> _availableSkills;
    private Boss _boss;

    // 决策频率控制
    private float _decisionInterval = 0.5f;
    private float _lastDecisionTime = 0f;

    public SkillDecisionMaker(Boss boss)
    {
        _boss = boss;
        _factors = new List<InfluenceFactor>();
        _availableSkills = new List<Skill>();
    }

    public void AddFactor(InfluenceFactor factor) => _factors.Add(factor);
    public void RegisterSkill(Skill skill) => _availableSkills.Add(skill);

    // 更新冷却 (每帧调用)
    public void Update(float delta)
    {
        foreach (var skill in _availableSkills)
        {
            skill.UpdateCooldown(delta);
        }
    }

    // 核心决策方法 (由 FightState 调用)
    public Skill SelectSkill(PlayerDetector detector)
    {
        // 1. 频率检查
        if (Time.time - _lastDecisionTime < _decisionInterval)
        {
            return null; // 冷却中，不重新决策
        }

        if (_availableSkills.Count == 0) return null;

        // 2. 计算每个技能的总分
        Dictionary<Skill, float> skillScores = new Dictionary<Skill, float>();

        foreach (var skill in _availableSkills)
        {
            float totalScore = 0f;
            int skillIndex = _availableSkills.IndexOf(skill);

            // 遍历所有因子
            foreach (var factor in _factors)
            {
                // 确保权重数组长度足够
                if (skillIndex < factor.weights.Length)
                {
                    // 因子得分 (0~1) * 该技能对此因子的权重
                    float factorScore = Mathf.Clamp01(factor.CalculateScore(_boss, detector));
                    totalScore += factorScore * factor.weights[skillIndex];
                }
            }
            skillScores[skill] = totalScore;
        }

        // 3. 排序取前 2
        // 按分数降序排序
        var sortedSkills = new List<Skill>(skillScores.Keys);
        sortedSkills.Sort((a, b) => skillScores[b].CompareTo(skillScores[a]));

        // 4. 随机选择前 2 中的一个
        Skill selectedSkill = null;
        int count = Mathf.Min(2, sortedSkills.Count);
        if (count > 0)
        {
            int randomIndex = Random.Range(0, count);
            selectedSkill = sortedSkills[randomIndex];
        }

        // 5. 刷新决策计时
        _lastDecisionTime = Time.time;

        Debug.Log($"Decision: Selected {selectedSkill?.skillId} with score {skillScores[selectedSkill]}");
        return selectedSkill;
    }

    // 外部强制刷新决策 (如技能释放完成后)
    public void ResetDecisionTimer()
    {
        _lastDecisionTime = 0f;
    }
}