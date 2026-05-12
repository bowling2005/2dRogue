using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float baseDetectionRange = 5f;
    [SerializeField] private float observeInterval = 1f;

    [Header("References")]
    [SerializeField] private Boss boss;
    [SerializeField] private BoxCollider2D detectionCollider;
    [SerializeField] private Rigidbody2D bossRigidbody;

    private Transform _playerTransform;
    private float _currentDetectionRange;
    private float _originalDetectionRange;
    private float _observeTimer;
    private bool _isObserving;
    private bool _isBossFacingRight;

    private void Awake()
    {
        if (boss == null)
        {
            boss = GetComponent<Boss>();
        }

        if (bossRigidbody == null)
        {
            bossRigidbody = GetComponent<Rigidbody2D>();
        }

        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<BoxCollider2D>();
        }

        _originalDetectionRange = baseDetectionRange;
        _currentDetectionRange = baseDetectionRange;

        SetupCollider();
        UpdateFacingDirection();
    }

    private void Update()
    {
        UpdateFacingDirection();
        UpdateColliderOffset();
    }

    public void ExpandDetectionRange(float multiplier)
    {
        _currentDetectionRange = _originalDetectionRange * multiplier;
        UpdateColliderSize();
        Debug.Log($"PlayerDetector: Range expanded to {_currentDetectionRange:F1}.");
    }

    public void RestoreDetectionRange()
    {
        _currentDetectionRange = _originalDetectionRange;
        UpdateColliderSize();
        Debug.Log($"PlayerDetector: Range restored to {_currentDetectionRange:F1}.");
    }

    public void SetDetectionRange(float absoluteRange)
    {
        _currentDetectionRange = Mathf.Max(1f, absoluteRange);
        UpdateColliderSize();
    }

    public Transform GetDetectedPlayer() => _playerTransform;
    public bool HasPlayerDetected() => _playerTransform != null;
    public bool IsFacingRight() => _isBossFacingRight;
    public float GetCurrentRange() => _currentDetectionRange;
    public float GetBaseRange() => _originalDetectionRange;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
        {
            return;
        }

        _playerTransform = collision.transform;
        StartObserving();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag) || !_isObserving)
        {
            return;
        }

        _playerTransform = collision.transform;
        UpdateObserveCount();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
        {
            return;
        }

        StopObserving();
        _playerTransform = null;
        boss?.OnPlayerLost();
    }

    private void StartObserving()
    {
        _isObserving = true;
        _observeTimer = 0f;
    }

    private void StopObserving()
    {
        _isObserving = false;
        _observeTimer = 0f;
    }

    private void UpdateObserveCount()
    {
        _observeTimer += Time.deltaTime;
        if (_observeTimer < observeInterval)
        {
            return;
        }

        _observeTimer = 0f;
        boss?.OnPlayerSpotted();
    }

    private void SetupCollider()
    {
        if (detectionCollider == null)
        {
            return;
        }

        detectionCollider.isTrigger = true;
        UpdateColliderSize();
        UpdateColliderOffset();
    }

    private void UpdateColliderSize()
    {
        if (detectionCollider == null)
        {
            return;
        }

        detectionCollider.size = new Vector2(_currentDetectionRange * 2f, detectionCollider.size.y);
    }

    private void UpdateColliderOffset()
    {
        if (detectionCollider == null)
        {
            return;
        }

        float offsetX = _isBossFacingRight ? _currentDetectionRange : -_currentDetectionRange;
        detectionCollider.offset = new Vector2(offsetX, detectionCollider.offset.y);
    }

    private void UpdateFacingDirection()
    {
        if (boss != null)
        {
            _isBossFacingRight = boss.IsFacingRight;
            return;
        }

        if (bossRigidbody != null && Mathf.Abs(bossRigidbody.velocity.x) > 0.01f)
        {
            _isBossFacingRight = bossRigidbody.velocity.x >= 0f;
            return;
        }

        _isBossFacingRight = transform.localScale.x >= 0f;
    }
}
