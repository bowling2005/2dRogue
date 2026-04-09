using UnityEngine;

// 影响因子基类
public abstract class InfluenceFactor
{
    public string factorName;
    // 权重数组：对应每个技能 ID 的权重 [Skill1_Weight, Skill2_Weight, ...]
    public float[] weights;

    public InfluenceFactor(string name, float[] skillWeights)
    {
        factorName = name;
        weights = skillWeights;
    }

    // 核心方法：计算当前环境下该因子的得分 (0~1)
    public abstract float CalculateScore(Boss boss, PlayerDetector detector);
}