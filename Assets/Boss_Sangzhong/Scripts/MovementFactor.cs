using UnityEngine;

// 移动决策因子基类
public abstract class MovementFactor
{
    public string factorName;
    // 权重：[Towards_Weight, Away_Weight]  0:接近，1:远离
    public float[] weights;

    public MovementFactor(string name, float[] moveWeights)
    {
        factorName = name;
        weights = moveWeights;
    }

    // 计算当前环境下该因子的得分 (0~1)
    public abstract float CalculateScore(Boss boss, PlayerDetector detector);
}