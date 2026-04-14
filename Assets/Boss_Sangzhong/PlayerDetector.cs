using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float baseDetectionRange = 5f;   // 基础检测范围
    [SerializeField] private float observeInterval = 1f;  // 每隔多少秒计数+1

    private Boss at_boss;
    private BoxCollider2D at_collider;
    private Rigidbody2D at_rigidbody;
    private bool isBossFacingRight;
    private Transform playerTransform;

    // 运行时数据
    private float _currentDetectionRange;  // 当前实际范围（可动态调整）
    private float _originalDetectionRange; // 保存原始范围用于恢复

    private float observeTimer;
    private bool isObserving;

    void Start()
    {
        at_boss = GetComponent<Boss>();
        at_collider = GetComponent<BoxCollider2D>();
        at_rigidbody = GetComponent<Rigidbody2D>();

        // 保存原始范围
        _originalDetectionRange = baseDetectionRange;
        _currentDetectionRange = baseDetectionRange;

        SetupCollider();
        JudgeDir();
    }

    private void JudgeDir()
    {
        if (at_rigidbody != null && at_rigidbody.velocity.x != 0f)
            isBossFacingRight = at_rigidbody.velocity.x >= 0f;
        else
            isBossFacingRight = transform.localScale.x >= 0f;
    }

    private void SetupCollider()
    {
        if (at_collider != null)
        {
            at_collider.size = new Vector2(_currentDetectionRange * 2f, at_collider.size.y);
            at_collider.isTrigger = true;
        }
    }

    // 临时扩大范围（如进入战斗时调用）
    public void ExpandDetectionRange(float multiplier)
    {
        _currentDetectionRange = _originalDetectionRange * multiplier;
        UpdateColliderSize();
        Debug.Log($"PlayerDetector: Range expanded to {_currentDetectionRange:F1} (x{multiplier})");
    }

    // 恢复原始范围
    public void RestoreDetectionRange()
    {
        _currentDetectionRange = _originalDetectionRange;
        UpdateColliderSize();
        Debug.Log($"PlayerDetector: Range restored to {_currentDetectionRange:F1}");
    }

    // 直接设置绝对范围值
    public void SetDetectionRange(float absoluteRange)
    {
        _currentDetectionRange = Mathf.Max(1f, absoluteRange); // 最小1米
        UpdateColliderSize();
    }

    // 更新碰撞体实际尺寸
    private void UpdateColliderSize()
    {
        if (at_collider != null)
        {
            // 只修改 X 轴尺寸（检测前方），保持 Y 轴不变
            at_collider.size = new Vector2(_currentDetectionRange * 2f, at_collider.size.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            playerTransform = collision.transform;
            StartObserving();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag) && isObserving)
        {
            playerTransform = collision.transform;
            UpdateObserveCount();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            StopObserving();
            at_boss?.OnPlayerLost();
        }
             
    }

    private void StartObserving()
    {
        isObserving = true;
        observeTimer = 0f;
    }

    private void StopObserving() => isObserving = false;

    private void UpdateObserveCount()
    {
        observeTimer += Time.deltaTime;
        if (observeTimer >= observeInterval)
        {
            observeTimer = 0f;
            at_boss?.OnPlayerSpotted();  
        }
    }

    // === 公开接口 ===
    public Transform GetDetectedPlayer() => playerTransform;
    public bool HasPlayerDetected() => playerTransform != null;
    public bool IsFacingRight() => isBossFacingRight;
    public float GetCurrentRange() => _currentDetectionRange;
    public float GetBaseRange() => _originalDetectionRange;

    void Update()
    {
        JudgeDir();
        UpdateColliderOffset();
    }

    private void UpdateColliderOffset()
    {
        if (at_collider == null) return;
        float offsetX = isBossFacingRight ? _currentDetectionRange : -_currentDetectionRange;
        at_collider.offset = new Vector2(offsetX, at_collider.offset.y);
    }
}