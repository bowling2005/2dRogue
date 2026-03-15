using System.Collections.Generic;
using UnityEngine;

public class RoomNode : MonoBehaviour
{
    [Header("房间配置")]
    // 【核心修改点 2】：在编辑器中手动安置这个物体，用它的大小定义房间范围
    public GameObject cameraArea;

    public Camera mainCamera; // 统一使用主相机引用
    public List<GameObject> activeObjects = new List<GameObject>();
    public LeaveArea lvArea;
    public string roomID = "Room_00";

    private bool isInitialized = false;

    // 缓存相机参数，用于恢复
    private float originalOrthoSize;
    private Rect originalRect;

    private void Awake()
    {
        // 确保 cameraArea 存在
        if (cameraArea == null)
        {
            Debug.LogError($"RoomNode {roomID}: cameraArea is not assigned!");
        }
    }

    public void Init()
    {
        if (isInitialized) return;

        if (lvArea != null)
        {
            lvArea.OnPlayerLeft += HandlePlayerLeft;
        }

        isInitialized = true;
    }

    // 当玩家离开当前房间
    private void HandlePlayerLeft(LeaveArea.ExitDirection direction, RoomNode nextRoom)
    {
        RoomManager.Instance.OnPlayerTransition(this, nextRoom);
    }

    // 进入房间：激活物体，调整相机
    public void EnterRoom(Camera cam)
    {
        if (cam == null) return;
        mainCamera = cam;

        // 1. 激活物体
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null) obj.SetActive(true);
        }
        if (lvArea != null) lvArea.enabled = true;

        // 2. 根据 cameraArea 调整相机
        UpdateCameraView(cam);
    }

    // 离开房间：隐藏物体
    public void LeaveRoom()
    {
        if (lvArea != null) lvArea.enabled = false;

        foreach (GameObject obj in activeObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    // 核心逻辑：根据 cameraArea 的 Collider 大小设置相机
    private void UpdateCameraView(Camera cam)
    {
        if (cameraArea == null) return;

        BoxCollider2D box = cameraArea.GetComponent<BoxCollider2D>();
        if (box == null)
        {
            Debug.LogWarning($"RoomNode {roomID}: cameraArea missing BoxCollider2D");
            //  fallback: 移动相机到中心即可
            cam.transform.position = new Vector3(transform.position.x, transform.position.y, cam.transform.position.z);
            return;
        }

        // 获取实际世界尺寸
        Vector2 size = new Vector2(box.size.x * cameraArea.transform.localScale.x,
                                   box.size.y * cameraArea.transform.localScale.y);

        // 移动相机到 cameraArea 的中心
        Vector3 targetPos = cameraArea.transform.position;
        targetPos.z = cam.transform.position.z;
        cam.transform.position = targetPos;

        // 计算 Orthographic Size (高度的一半)
        // 注意：如果游戏强制固定宽高比，可能需要调整这里的逻辑
        // 这里我们假设相机应该完全容纳这个 box 的高度，宽度由屏幕比例决定
        float targetHeight = size.y;
        cam.orthographicSize = targetHeight * 0.5f;

        // 如果希望相机严格匹配 box 的宽高比（可能导致黑边或裁剪，视 Screen 比例而定）
        // 通常 2D 游戏固定 orthographicSize，让宽度自适应，或者固定 Aspect Ratio
        // 这里只设置位置和高度，宽度由 Unity 相机自动根据 Screen.Aspect 计算
    }
}