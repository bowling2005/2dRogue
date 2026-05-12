// Assets/Scripts/Rewind/RewindSystem.cs
using UnityEngine;
using System.Collections.Generic;

public class RewindSystem : MonoBehaviour
{
    // ========== 单例（方便外部调用） ==========
    public static RewindSystem Instance { get; private set; }

    [Header("设置")]
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
    public Sprite[] allSprites;

    [Header("控制组件")]
    public List<Behaviour> componentsToDisable = new List<Behaviour>();
    public List<IRewindableObject> rewindables = new List<IRewindableObject>();

    // ========== 环形缓冲区 ==========
    private RewindSnapshot[] historyBuffer;
    public int BufferSize { get; private set; } 
    private int writeIndex = 0;
    private int readIndex = 0;
    private int count = 0;

    private float recordTimer = 0f;

   
    public bool IsRewinding { get; private set; }

    private float rewindTimer = 0f;
    private Dictionary<Behaviour, bool> originalEnabledState = new Dictionary<Behaviour, bool>();

    // ========== 可回退物体管理（简化） ==========


    // ========== Unity生命周期 ==========
    public void AddRewindable(IRewindableObject r) { if (!rewindables.Contains(r)) rewindables.Add(r); }
    public void RemoveRewindable(IRewindableObject r) { rewindables.Remove(r); }
    void Awake()
    {
        Instance = this;

        // 计算缓冲区大小并预分配
        BufferSize = Mathf.CeilToInt(maxRewindTime / recordInterval) + 1;
        historyBuffer = new RewindSnapshot[BufferSize];

        AutoAddComponentsToDisable();
        RefreshRewindables();  // 初始化时扫描一次
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            RewindCall();

        if (!IsRewinding)
            RecordState();
        else
            ProcessRewind();
    }

    // ========== 原有功能方法 ==========

    private void AutoAddComponentsToDisable()
    {
        if (playerObject == null) return;
        if (playerController != null && !componentsToDisable.Contains(playerController))
            componentsToDisable.Add(playerController);
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null && !componentsToDisable.Contains(animator))
            componentsToDisable.Add(animator);
    }

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

    private void SaveSnapshot()
    {
        int spriteIndex = GetSpriteIndex();
        RewindSnapshot snapshot = new RewindSnapshot(
            Time.time, playerRb.position, playerRb.rotation, playerRb.velocity,
            playerController != null ? playerController.health : 100f,
            playerController != null ? playerController.isGrounded : false,
            spriteIndex
        );

        // 缓冲区满时清理旧数据
        if (count == BufferSize)
            BroadcastClear(writeIndex);

        historyBuffer[writeIndex] = snapshot;

        BroadcastRecord(writeIndex);

        writeIndex = (writeIndex + 1) % BufferSize;
        if (count < BufferSize) count++;
    }

    private int GetSpriteIndex()
    {
        if (playerSprite == null || playerSprite.sprite == null) return -1;
        if (allSprites == null || allSprites.Length == 0) return -1;
        for (int i = 0; i < allSprites.Length; i++)
            if (allSprites[i] == playerSprite.sprite) return i;
        return -1;
    }

    private Sprite GetSpriteByIndex(int index)
    {
        if (index < 0 || index >= allSprites.Length) return null;
        return allSprites[index];
    }


    /// <summary>开始/停止倒带</summary>
    public void RewindCall()
    {
        if (count < 2) { Debug.LogWarning("历史记录不足，无法倒带！"); return; }
        if (IsRewinding) StopRewind(); else StartRewind();
    }

    /// <summary>获取当前可倒带时长</summary>
    public float GetAvailableRewindTime() => count * recordInterval;

    /// <summary>清空所有历史记录</summary>
    public void ClearHistory()
    {
        writeIndex = 0; readIndex = 0; count = 0; recordTimer = 0f;
        foreach (var r in rewindables) r?.ClearAll();
    }

    /// <summary>刷新可回退物体列表（动态生成物体后调用）</summary>
    public void RefreshRewindables()
    {
        rewindables.Clear();
        var all = FindObjectsOfType<MonoBehaviour>();
        foreach (var mb in all)
            if (mb is IRewindableObject r) rewindables.Add(r);
    }


    private void StartRewind()
    {
        IsRewinding = true;
        DisableControlComponents();
        readIndex = (writeIndex - 1 + BufferSize) % BufferSize;
        rewindTimer = 0f;
        if (count > 0) ApplySnapshot(historyBuffer[readIndex]);
    }

    private void StopRewind()
    {
        IsRewinding = false;
        RestoreControlComponents();
    }

    private void DisableControlComponents()
    {
        originalEnabledState.Clear();
        foreach (var c in componentsToDisable)
        {
            if (c != null) { originalEnabledState[c] = c.enabled; c.enabled = false; }
        }
    }

    private void RestoreControlComponents()
    {
        foreach (var kvp in originalEnabledState)
            if (kvp.Key != null) kvp.Key.enabled = kvp.Value;
        originalEnabledState.Clear();
    }

    private void ProcessRewind()
    {
        if (count == 0) return;
        rewindTimer += Time.deltaTime;
        if (rewindTimer >= recordInterval)
        {
            rewindTimer = 0f;
            readIndex = (readIndex - 1 + BufferSize) % BufferSize;
            int steps = (writeIndex - readIndex + BufferSize) % BufferSize;
            if (steps >= count) { readIndex = writeIndex; StopRewind(); return; }
            ApplySnapshot(historyBuffer[readIndex]);
        }
    }

    private void ApplySnapshot(RewindSnapshot snapshot)
    {
        BroadcastApply(readIndex);  

        if (playerRb == null) return;
        playerRb.position = snapshot.position;
        playerRb.rotation = snapshot.rotation;
        playerRb.velocity = snapshot.velocity;
        if (playerSprite != null) playerSprite.sprite = GetSpriteByIndex(snapshot.spriteIndex);
        if (playerController != null)
        {
            playerController.health = snapshot.health;
            playerController.isGrounded = snapshot.isGrounded;
        }
    }

    private void BroadcastRecord(int idx)
    {
        var rewindablesSnapshot = new List<IRewindableObject>(rewindables);

        foreach (var r in rewindablesSnapshot)
        {
            if (r == null) continue;
            var mb = (MonoBehaviour)r;
            if (r.IncludeWhenInactive || (mb != null && mb.gameObject.activeSelf))
                r.RecordState(idx);
        }
    }

    private void BroadcastApply(int idx)
    {
        var rewindablesSnapshot = new List<IRewindableObject>(rewindables);

        foreach (var r in rewindablesSnapshot)
        {
            if (r == null) continue;
            var mb = (MonoBehaviour)r;
            if (r.IncludeWhenInactive || (mb != null && mb.gameObject.activeSelf))
                r.ApplyState(idx);
        }
    }

    private void BroadcastClear(int idx)
    {
        var rewindablesSnapshot = new List<IRewindableObject>(rewindables);

        foreach (var r in rewindablesSnapshot)
        {
            r?.ClearAt(idx);
        }
    }

    // ========== GUI调试 ==========
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 260, 140));
        GUILayout.Label($"<b>RewindSystem</b>");
        GUILayout.Label($"Buffer: {count}/{BufferSize}");
        GUILayout.Label($"Time: {GetAvailableRewindTime():F1}s / {maxRewindTime}s");
        GUILayout.Label($"Rewinding: {IsRewinding}"); 
        GUILayout.Label($"Objects: {rewindables.Count}");
        GUILayout.EndArea();
    }
}