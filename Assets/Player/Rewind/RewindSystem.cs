using UnityEngine;
using System.Collections.Generic;

public class RewindSystem : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("记录间隔（秒）。0.1f=每秒10次，0.2f=每秒5次")]
    [Range(0.05f, 1f)]
    public float recordInterval = 0.1f;

    [Tooltip("最大倒带时长（秒）")]
    [Range(1f, 60f)]
    public float maxRewindTime = 10f;

    [Header("引用")]
    public GameObject playerObject;
    public Rigidbody2D playerRb;
    public SpriteRenderer playerSprite;
    public PlayerController playerController;

    [Header("精灵管理")]
    [Tooltip("玩家可能使用的所有精灵，按索引访问")]
    public Sprite[] allSprites;

    [Header("控制组件")]
    public List<Behaviour> componentsToDisable = new List<Behaviour>();

    // 环形缓冲区 - 固定大小，无 GC
    private RewindSnapshot[] historyBuffer;
    private int bufferSize = 0;
    private int writeIndex = 0;
    private int readIndex = 0;
    private int count = 0;  // 当前有效数据量

    private float recordTimer = 0f;
    private bool isRewinding = false;
    private float rewindTimer = 0f;

    // 组件状态缓存
    private Dictionary<Behaviour, bool> originalEnabledState = new Dictionary<Behaviour, bool>();

    void Awake()
    {
        // 计算缓冲区大小并预分配
        bufferSize = Mathf.CeilToInt(maxRewindTime / recordInterval) + 1;
        historyBuffer = new RewindSnapshot[bufferSize];

        Debug.Log($"[RewindSystem] 缓冲区大小：{bufferSize}，预计内存：{bufferSize * 40 / 1024f:F2}KB");

        AutoAddComponentsToDisable();
    }

    private void AutoAddComponentsToDisable()
    {
        if (playerObject == null) return;

        if (playerController != null && !componentsToDisable.Contains(playerController))
            componentsToDisable.Add(playerController);

        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null && !componentsToDisable.Contains(animator))
            componentsToDisable.Add(animator);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            RewindCall();
        }

        if (!isRewinding)
        {
            RecordState();
        }
        else
        {
            ProcessRewind();
        }
    }

    /// <summary>
    /// 记录状态 - 环形缓冲区写入
    /// </summary>
    private void RecordState()
    {
        if (playerObject == null || playerRb == null || !playerObject.activeSelf) return;

        recordTimer += Time.deltaTime;
        if (recordTimer >= recordInterval)
        {
            SaveSnapshot();
            recordTimer = 0f;
        }
    }

    /// <summary>
    /// 保存快照 - 无 GC，覆盖旧数据
    /// </summary>
    private void SaveSnapshot()
    {
        int spriteIndex = GetSpriteIndex();

        RewindSnapshot snapshot = new RewindSnapshot(
            Time.time,
            playerRb.position,
            playerRb.rotation,
            playerRb.velocity,
            playerController != null ? playerController.health : 100f,
            playerController != null ? playerController.isGrounded : false,
            spriteIndex
        );

        // 环形缓冲区写入
        historyBuffer[writeIndex] = snapshot;
        writeIndex = (writeIndex + 1) % bufferSize;

        if (count < bufferSize)
            count++;
    }

    /// <summary>
    /// 获取当前 Sprite 在数组中的索引
    /// </summary>
    private int GetSpriteIndex()
    {
        if (playerSprite == null || playerSprite.sprite == null) return -1;
        if (allSprites == null || allSprites.Length == 0) return -1;

        for (int i = 0; i < allSprites.Length; i++)
        {
            if (allSprites[i] == playerSprite.sprite)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 根据索引获取 Sprite
    /// </summary>
    private Sprite GetSpriteByIndex(int index)
    {
        if (index < 0 || index >= allSprites.Length) return null;
        return allSprites[index];
    }

    public void RewindCall()
    {
        if (count < 2)
        {
            Debug.LogWarning("历史记录不足，无法倒带！");
            return;
        }

        if (isRewinding)
            StopRewind();
        else
            StartRewind();
    }

    private void StartRewind()
    {
        isRewinding = true;
        DisableControlComponents();

        // 从最新数据开始倒带
        readIndex = (writeIndex - 1 + bufferSize) % bufferSize;
        rewindTimer = 0f;

        if (count > 0)
            ApplySnapshot(historyBuffer[readIndex]);

        Debug.Log($"[RewindSystem] 开始倒带，可用记录：{count}/{bufferSize}");
    }

    private void StopRewind()
    {
        isRewinding = false;
        RestoreControlComponents();

        Debug.Log($"[RewindSystem] 停止倒带");
    }

    private void DisableControlComponents()
    {
        originalEnabledState.Clear();
        foreach (var component in componentsToDisable)
        {
            if (component != null)
            {
                originalEnabledState[component] = component.enabled;
                component.enabled = false;
            }
        }
    }

    private void RestoreControlComponents()
    {
        foreach (var kvp in originalEnabledState)
        {
            if (kvp.Key != null)
                kvp.Key.enabled = kvp.Value;
        }
        originalEnabledState.Clear();
    }

    /// <summary>
    /// 倒带处理 - 环形缓冲区读取
    /// </summary>
    private void ProcessRewind()
    {
        if (count == 0) return;

        rewindTimer += Time.deltaTime;
        if (rewindTimer >= recordInterval)
        {
            rewindTimer = 0f;

            // 环形缓冲区向前读取
            readIndex = (readIndex - 1 + bufferSize) % bufferSize;

            // 检查是否回到起点
            int stepsTaken = (writeIndex - readIndex + bufferSize) % bufferSize;
            if (stepsTaken >= count)
            {
                readIndex = writeIndex;
                StopRewind();
                return;
            }

            ApplySnapshot(historyBuffer[readIndex]);
        }
    }

    private void ApplySnapshot(RewindSnapshot snapshot)
    {
        if (playerRb == null) return;

        playerRb.position = snapshot.position;
        playerRb.rotation = snapshot.rotation;
        playerRb.velocity = snapshot.velocity;

        if (playerSprite != null)
        {
            playerSprite.sprite = GetSpriteByIndex(snapshot.spriteIndex);
        }

        if (playerController != null)
        {
            playerController.health = snapshot.health;
            playerController.isGrounded = snapshot.isGrounded;
        }
    }

    /// <summary>
    /// 获取当前可倒带时长
    /// </summary>
    public float GetAvailableRewindTime()
    {
        return count * recordInterval;
    }

    /// <summary>
    /// 清空历史
    /// </summary>
    public void ClearHistory()
    {
        writeIndex = 0;
        readIndex = 0;
        count = 0;
        recordTimer = 0f;
    }

    /// <summary>
    /// 调试信息
    /// </summary>
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 150));
        GUILayout.Label($"倒带系统 (优化版)");
        GUILayout.Label($"缓冲区：{count}/{bufferSize}");
        GUILayout.Label($"可倒带：{GetAvailableRewindTime():F1}s / {maxRewindTime}s");
        GUILayout.Label($"倒带中：{isRewinding}");
        GUILayout.Label($"内存占用：~{bufferSize * 40 / 1024f:F2}KB");
        GUILayout.EndArea();
    }
}