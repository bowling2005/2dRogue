using UnityEngine;

public class HealthLossFactor : InfluenceFactor
{
    public HealthLossFactor(float[] weights) : base("HealthLoss", weights) { }

    public override float CalculateScore(Boss boss, PlayerDetector detector)
    {
        if (boss.MaxHealth <= 0) return 0f;

        // 计算损失百分比 (0.0 ~ 1.0)
        float lossPercent = (boss.MaxHealth - boss.CurrentHealth) / boss.MaxHealth;

        // 归一化返回 (本身就是 0~1)
        return Mathf.Clamp01(lossPercent);
    }
}