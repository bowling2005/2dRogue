using System.Collections.Generic;
using UnityEngine;

public enum MoveCommand { Towards, Away, Idle }

public class MovementDecisionMaker
{
    private List<MovementFactor> _factors;
    private Boss _boss;

    [SerializeField] private float _decisionInterval = 0.5f;
    private float _lastDecisionTime = 0f;

    // 冻结机制
    private bool _isFrozen = false;
    private float _frozenRemainingTime = 0f;

    public MoveCommand CurrentCommand { get; private set; } = MoveCommand.Idle;

    public MovementDecisionMaker(Boss boss)
    {
        _boss = boss;
        _factors = new List<MovementFactor>();
    }

    public void AddFactor(MovementFactor factor) => _factors.Add(factor);

    public bool TryDecide(PlayerDetector detector)
    {
        // 0. 冻结检查
        if (_isFrozen) return false;

        // 1. 间隔检查
        if (Time.time - _lastDecisionTime < _decisionInterval)
        {
            return false;
        }

        // 2. 计算接近 vs 远离 的总分
        float scoreTowards = 0f;
        float scoreAway = 0f;

        foreach (var factor in _factors)
        {
            float factorScore = Mathf.Clamp01(factor.CalculateScore(_boss, detector));
            if (factor.weights.Length > 0) scoreTowards += factorScore * factor.weights[0];
            if (factor.weights.Length > 1) scoreAway += factorScore * factor.weights[1];
        }

        // 3. 判定指令
        if (scoreAway > scoreTowards)
            CurrentCommand = MoveCommand.Away;
        else if (scoreTowards > 0.1f)
            CurrentCommand = MoveCommand.Towards;
        else
            CurrentCommand = MoveCommand.Idle;

        _lastDecisionTime = Time.time;
        Debug.Log($"MoveDecision: {CurrentCommand} (T:{scoreTowards:F2}, A:{scoreAway:F2})");
        return true;
    }

    // === 冻结机制 ===
    public void Freeze()
    {
        if (_isFrozen) return;
        _isFrozen = true;
        float elapsed = Time.time - _lastDecisionTime;
        _frozenRemainingTime = Mathf.Max(0f, _decisionInterval - elapsed);
    }

    public void Unfreeze()
    {
        if (!_isFrozen) return;
        _isFrozen = false;
        _lastDecisionTime = Time.time - (_decisionInterval - _frozenRemainingTime);
        _frozenRemainingTime = 0f;
    }

    public void ResetTimer()
    {
        _isFrozen = false;
        _lastDecisionTime = 0f;
        _frozenRemainingTime = 0f;
    }

    public bool IsFrozen() => _isFrozen;
}