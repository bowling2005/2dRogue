using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [Header("引用设置")]
    public Camera mainCamera; // 改为直接引用 Camera 组件，方便修改 size
    private Collider2D _playerCollider;

    [Header("性能与平滑")]
    public float cameraMoveSpeed = 5f;
    public float cameraSizeSpeed = 2f; // 新增：相机大小变化的速度

    private List<RoomNode> _allRooms = new List<RoomNode>();
    private RoomNode _currentRoom;

    // 缓存目标相机大小，用于平滑过渡
    private float _targetOrthoSize;

    private void Awake()
    {
        // 自动获取主相机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("场景中未找到 Main Camera!");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerCollider = player.GetComponent<Collider2D>();
        }

        _allRooms = new List<RoomNode>(FindObjectsOfType<RoomNode>());

        // 初始化：关闭所有房间
        foreach (var room in _allRooms)
        {
            room.SetRoomActive(false);
        }

        // 初始化相机大小为默认值，避免第一帧错误
        _targetOrthoSize = mainCamera.orthographicSize;
    }

    private void Update()
    {
        if (_playerCollider == null || _allRooms.Count == 0 || mainCamera == null) return;

        RoomNode targetRoom = null;

        // 寻找完全包含玩家的房间
        foreach (var room in _allRooms)
        {
            if (room.IsPlayerFullyInside(_playerCollider))
            {
                targetRoom = room;
                break;
            }
        }

        // 房间切换逻辑
        if (targetRoom != null && targetRoom != _currentRoom)
        {
            SwitchRoom(targetRoom);
        }

        // 相机平滑移动与缩放逻辑
        if (_currentRoom != null)
        {
            // 1. 位置移动
            Vector3 targetPos = _currentRoom.transform.position;
            targetPos.z = mainCamera.transform.position.z;
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);

            _targetOrthoSize = _currentRoom.TargetOrthoSize;

            // 平滑过渡相机大小
            float currentSize = mainCamera.orthographicSize;
            if (Mathf.Abs(currentSize - _targetOrthoSize) > 0.01f)
            {
                mainCamera.orthographicSize = Mathf.Lerp(currentSize, _targetOrthoSize, Time.deltaTime * cameraSizeSpeed);
            }

        }
    }

    private void SwitchRoom(RoomNode newRoom)
    {
        if (_currentRoom != null)
        {
            _currentRoom.SetRoomActive(false);
        }

        _currentRoom = newRoom;

        if (_currentRoom != null)
        {
            _currentRoom.SetRoomActive(true);
            // 立即更新目标大小，避免平滑延迟导致的初始画面不对
            _targetOrthoSize = _currentRoom.TargetOrthoSize;
        }
    }
}