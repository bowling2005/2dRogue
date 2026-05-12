using System.Collections.Generic;
using UnityEngine;

public class SkillDecisionMaker
{
    private List<InfluenceFactor> _factors;
    private List<Skill> _availableSkills;
    private Boss _boss;

    // 决策频率控制
    [SerializeField] private float _decisionInterval = 2.5f;
    private float _lastDecisionTime = 0f;

    // 冻结机制：当为 true 时，决策冷却计时暂停
    private bool _isFrozen = false;
    private float _frozenRemainingTime = 0f; // 冻结时保存的剩余冷却时间

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
        // 更新技能冷却 (不受冻结影响，技能冷却是全局的)
        foreach (var skill in _availableSkills)
        {
            skill.UpdateCooldown(delta);
        }
    }

    // 核心决策方法
    public Skill SelectSkill(PlayerDetector detector)
    {
        // 0. 冻结检查：如果被冻结，直接返回 null 表示"不决策"
        if (_isFrozen)
        {
            return null;
        }

        // 1. 频率检查
        if (Time.time - _lastDecisionTime < _decisionInterval)
        {
            return null; // 冷却中
        }

        if (_availableSkills.Count == 0) return null;

        // 2. 计算每个技能的总分
        Dictionary<Skill, float> skillScores = new Dictionary<Skill, float>();

        foreach (var skill in _availableSkills)
        {
            float totalScore = 0f;
            int skillIndex = _availableSkills.IndexOf(skill);

            foreach (var factor in _factors)
            {
                if (skillIndex < factor.weights.Length)
                {
                    float factorScore = Mathf.Clamp01(factor.CalculateScore(_boss, detector));
                    totalScore += factorScore * factor.weights[skillIndex];
                }
            }
            skillScores[skill] = totalScore;
        }

        // 3. 排序取前 2
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

        Debug.Log($"SkillDecision: Selected {selectedSkill?.skillId} (Score: {skillScores[selectedSkill]:F2})");
        return selectedSkill;
    }

    // === 冻结机制核心方法 ===

    // 冻结决策：暂停冷却计时
    public void Freeze()
    {
        if (_isFrozen) return; // 避免重复冻结

        _isFrozen = true;
        // 计算当前剩余冷却时间并保存
        float elapsed = Time.time - _lastDecisionTime;
        _frozenRemainingTime = Mathf.Max(0f, _decisionInterval - elapsed);
        Debug.Log("SkillDecisionMaker: Frozen");
    }

    // 解冻决策：恢复冷却计时
    public void Unfreeze()
    {
        if (!_isFrozen) return;

        _isFrozen = false;
        // 将 _lastDecisionTime 设置为"现在 - 剩余冷却时间"，实现冷却续期
        _lastDecisionTime = Time.time - (_decisionInterval - _frozenRemainingTime);
        _frozenRemainingTime = 0f;
        Debug.Log("SkillDecisionMaker: Unfrozen");
    }

    // 强制重置：立即允许决策
    public void ResetDecisionTimer()
    {
        _isFrozen = false;
        _lastDecisionTime = 0f;
        _frozenRemainingTime = 0f;
    }

    // 查询状态 (供调试)
    public bool IsFrozen() => _isFrozen;
    public float GetRemainingCooldown()
    {
        if (_isFrozen) return _frozenRemainingTime;
        return Mathf.Max(0f, _decisionInterval - (Time.time - _lastDecisionTime));
    }
}