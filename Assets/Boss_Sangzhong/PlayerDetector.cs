using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionOffset = 5f;
    [SerializeField] private float observeInterval = 1f;  // 每隔多少秒计数+1

    private Boss at_boss;
    private BoxCollider2D at_collider;
    private Rigidbody2D at_rigidbody;
    private bool isBossFacingRight;
    private Transform playerTransform;

    private float observeTimer;
    private bool isObserving;

    void Start()
    {
        at_boss = GetComponent<Boss>();
        at_collider = GetComponent<BoxCollider2D>();
        at_rigidbody = GetComponent<Rigidbody2D>();

        if (at_collider != null)
        {
            at_collider.size = new Vector2(10f, at_collider.size.y);
            at_collider.isTrigger = true;
        }
        JudgeDir();
    }

    private void JudgeDir()
    {
        if (at_rigidbody != null && at_rigidbody.velocity.x != 0f)
            isBossFacingRight = at_rigidbody.velocity.x >= 0f;
        else
            isBossFacingRight = transform.localScale.x >= 0f;
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

    public Transform GetDetectedPlayer() => playerTransform;
    public bool HasPlayerDetected() => playerTransform != null;
    public bool IsFacingRight() => isBossFacingRight;

    void Update()
    {
        JudgeDir();
        UpdateColliderOffset();
    }

    private void UpdateColliderOffset()
    {
        if (at_collider == null) return;
        float offsetX = isBossFacingRight ? detectionOffset : -detectionOffset;
        at_collider.offset = new Vector2(offsetX, at_collider.offset.y);
    }
}