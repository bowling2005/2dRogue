using UnityEngine;

public class DistanceFactor : InfluenceFactor
{
    private float _maxDetectionRange = 10f; // 最大有效距离

    public DistanceFactor(float[] weights) : base("Distance", weights) { }

    public override float CalculateScore(Boss boss, PlayerDetector detector)
    {
        if (!detector.HasPlayerDetected()) return 0f;

        float dist = Vector2.Distance(boss.Transform.position, detector.GetDetectedPlayer().position);

        // 距离越近，分数越高 (1.0 ~ 0.0)
        float score = 1f - Mathf.Clamp01(dist / _maxDetectionRange);

        return score;
    }
}