using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 需要引用 UI

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("引用")]
    public GameObject player;
    public Camera mainCamera;

    [Header("黑屏遮罩 (UI Image)")]
    // 找一个全屏黑色 Image 组件拖在这里
    public Image blackScreenMask;

    [Header("过渡设置")]
    public float transitionDuration = 0.5f; // 相机移动耗时

    private RoomNode currentRoom;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p;
        }
        if (mainCamera == null) mainCamera = Camera.main;

        // 确保黑屏初始是隐藏的
        if (blackScreenMask != null)
        {
            Color c = blackScreenMask.color;
            c.a = 0f;
            blackScreenMask.color = c;
            blackScreenMask.raycastTarget = false; // 防止阻挡输入
        }
    }

    private void Start()
    {
        RoomNode[] nodes = FindObjectsOfType<RoomNode>();
        foreach (var node in nodes) node.Init();

        if (nodes.Length > 0)
        {
            // 简单策略：默认激活第一个，或者找离玩家最近的
            // 这里为了演示，假设第一个就是出生点
            ForceEnterRoom(nodes[0]);
        }
    }

    public void ForceEnterRoom(RoomNode room)
    {
        if (currentRoom != null && currentRoom != room)
            currentRoom.LeaveRoom();

        currentRoom = room;
        isTransitioning = false;

        // 瞬间设置，无黑屏（用于游戏开始）
        currentRoom.EnterRoom(mainCamera);
    }

    public void OnPlayerTransition(RoomNode fromRoom, RoomNode toRoom)
    {
        if (isTransitioning || toRoom == null || fromRoom != currentRoom) return;

        StartCoroutine(TransitionCoroutine(fromRoom, toRoom));
    }

    private IEnumerator TransitionCoroutine(RoomNode fromRoom, RoomNode toRoom)
    {
        isTransitioning = true;

        // --- 步骤 1: 瞬间黑屏 ---
        if (blackScreenMask != null)
        {
            Color c = blackScreenMask.color;
            c.a = 1f; // 完全不透明
            blackScreenMask.color = c;
        }

        // 等待一帧确保黑屏渲染出来，防止玩家看到切换瞬间
        yield return null;

        // --- 步骤 2: 处理房间逻辑 (旧房间隐藏，新房间激活) ---
        fromRoom.LeaveRoom();
        toRoom.EnterRoom(mainCamera);
        // 注意：此时相机还没动，但新房间的物体已经激活在远处了，反正玩家看不见（黑屏）

        // --- 步骤 3: 移动相机 ---
        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = toRoom.transform.position; // 或者 toRoom.cameraArea.transform.position
        // 确保 Z 轴不变
        endPos.z = startPos.z;

        // 如果 RoomNode 的 EnterRoom 已经修正了相机位置，这里可能只需要做插值动画
        // 但为了平滑，我们手动插值位置

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            // 简单的线性插值，也可以用 SmoothStep
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // 确保最终位置精确匹配 RoomNode 定义的位置
        // 再次调用 UpdateCameraView 确保 OrthoSize 等参数绝对正确（以防插值过程中有偏差）
        // 其实 ToRoom.EnterRoom 里已经设过了，这里主要是修正 Position
        mainCamera.transform.position = endPos;

        // --- 步骤 4: 移除黑屏 ---
        if (blackScreenMask != null)
        {
            Color c = blackScreenMask.color;
            c.a = 0f;
            blackScreenMask.color = c;
        }

        currentRoom = toRoom;
        isTransitioning = false;

        Debug.Log($"Switched to {toRoom.roomID}");
    }
}