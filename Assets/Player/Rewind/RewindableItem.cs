// Assets/Scripts/Rewind/RewindableItem.cs
using UnityEngine;

/// <summary>
/// 可回退物品组件 - 精简版
/// 只记录：Transform + Sprite + Animator + Health
/// </summary>
[RequireComponent(typeof(Transform))]
public class RewindableItem : MonoBehaviour, IRewindableObject
{
    [System.Serializable]
    public struct ItemSnapshot
    {
        public Vector2 position;
        public float rotationZ;
        public int spriteIndex;
        public int animatorStateHash;
        public float animatorTime;
        public float health;
        public bool isActive;
        public bool hasData;
    }

    [Header("设置")]
    [Tooltip("即使 GameObject 非激活也参与回退")]
    public bool includeWhenInactive = true;

    [Tooltip("追踪的 SpriteRenderer")]
    public SpriteRenderer targetSprite;

    [Tooltip("追踪的 Animator")]
    public Animator targetAnimator;

    [Tooltip("如果此物体有 Health 字段，记录其值")]
    public bool recordHealth = false;

    [Tooltip("调试日志")]
    public bool debugLog = false;

    // ========== 缓冲区 ==========
    private ItemSnapshot[] snapshots;
    private RewindSystem system;
    private Transform tf;
    private int bufferSize;
    private bool initialized;

    // 修正：Sprite[] 而不是 SpriteRenderer[]
    private Sprite[] allSprites;

    public bool IncludeWhenInactive => includeWhenInactive;

    void Awake()
    {
        tf = transform;
        system = RewindSystem.Instance;
        if (system == null) { enabled = false; return; }

        if (targetSprite == null) targetSprite = GetComponent<SpriteRenderer>();
        if (targetAnimator == null) targetAnimator = GetComponent<Animator>();

        // 修正：从 system 获取 Sprite 数组
        if (system.allSprites != null)
            allSprites = system.allSprites;
    }

    private bool isRegistered = false;  // 添加标记，避免重复注册

    void OnEnable()
    {
        if (system == null) system = RewindSystem.Instance;
        if (system != null && !isRegistered)
        {
            if (!system.rewindables.Contains(this))
            {
                system.rewindables.Add(this);
                isRegistered = true;
            }
            // 延迟初始化缓冲区（确保 system.BufferSize 已计算）
            if (!initialized && system.BufferSize > 0) InitBuffer();
        }
    }

    void OnDisable()
    {
    }

    void OnDestroy()
    {
        if (system != null && isRegistered)
        {
            system.rewindables.Remove(this);
            isRegistered = false;
        }
    }

    private void InitBuffer()
    {
        if (initialized) return;
        bufferSize = system.BufferSize;
        snapshots = new ItemSnapshot[bufferSize];
        initialized = true;
        if (debugLog) Debug.Log($"[RewindItem] {name} buffer={bufferSize}");
    }

    public void RecordState(int bufferIndex)
    {
        if (!initialized) InitBuffer();

        if (!includeWhenInactive && !gameObject.activeSelf)
        {
            snapshots[bufferIndex].hasData = false;
            return;
        }

        ref var snap = ref snapshots[bufferIndex];

        snap.position = tf.position;
        snap.rotationZ = tf.rotation.eulerAngles.z;
        snap.spriteIndex = GetSpriteIndex();

        if (targetAnimator != null && targetAnimator.isActiveAndEnabled)
        {
            var info = targetAnimator.GetCurrentAnimatorStateInfo(0);
            snap.animatorStateHash = info.shortNameHash;
            snap.animatorTime = info.normalizedTime;
        }
        else
        {
            snap.animatorStateHash = 0;
            snap.animatorTime = 0;
        }

        snap.health = recordHealth ? GetComponent<PlayerController>()?.health ?? 100f : 100f;
        snap.isActive = gameObject.activeSelf;
        snap.hasData = true;

        if (debugLog) Debug.Log($"[Record] {name} @ {snap.position} | idx={bufferIndex}");
    }

    // RewindableItem.cs - ApplyState 方法开头添加：
    public void ApplyState(int bufferIndex)
    {
        if (!initialized) InitBuffer();
        ref var snap = ref snapshots[bufferIndex];

        if (debugLog)
        {
            Debug.Log($"[Apply] {name} | idx={bufferIndex} | " +
                     $"hasData={snap.hasData} | isActive={snap.isActive} | " +
                     $"pos={snap.position} | currActive={gameObject.activeSelf}");
        }

        if (!snap.hasData) return;

        bool wasActive = gameObject.activeSelf;
        if (snap.isActive && !wasActive)
        {
            if (debugLog) Debug.Log($"[Apply] {name} → SetActive(true)");
            gameObject.SetActive(true);
        }

        // 应用其他属性
        tf.position = snap.position;
        tf.rotation = Quaternion.Euler(0, 0, snap.rotationZ);

        // ... 其他属性 ...

        // 如果快照要求禁用，且当前是激活，再禁用
        if (!snap.isActive && wasActive)
        {
            if (debugLog) Debug.Log($"[Apply] {name} → SetActive(false)");
            gameObject.SetActive(false);
        }
    }

    public void ClearAt(int bufferIndex)
    {
        if (!initialized) return;
        snapshots[bufferIndex].hasData = false;
    }

    public void ClearAll()
    {
        if (!initialized) return;
        for (int i = 0; i < snapshots.Length; i++) snapshots[i].hasData = false;
    }

    private int GetSpriteIndex()
    {
        if (targetSprite == null || targetSprite.sprite == null) return -1;
        if (allSprites == null || allSprites.Length == 0) return -1;
        for (int i = 0; i < allSprites.Length; i++)
            if (allSprites[i] == targetSprite.sprite) return i;
        return -1;
    }

    private Sprite GetSpriteByIndex(int index)
    {
        if (index < 0 || allSprites == null || index >= allSprites.Length) return null;
        return allSprites[index];
    }

    void OnDrawGizmosSelected()
    {
        if (!debugLog || !initialized) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(tf.position, 0.25f);
    }
}