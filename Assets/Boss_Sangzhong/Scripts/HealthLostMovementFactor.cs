using UnityEngine;

public class HealthLossMovementFactor : MovementFactor
{
    // 权重：血量低时，远离权重高 (0.2, 0.8)
    public HealthLossMovementFactor(float[] weights) : base("HealthLossMove", weights) { }

    public override float CalculateScore(Boss boss, PlayerDetector detector)
    {
        if (boss.MaxHealth <= 0) return 0f;
        // 损失百分比 0~1
        float lossPercent = (boss.MaxHealth - boss.CurrentHealth) / boss.MaxHealth;
        return Mathf.Clamp01(lossPercent);
    }
}