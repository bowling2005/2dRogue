using UnityEngine;
using System;

public class LeaveArea : MonoBehaviour
{
    [Header("连接设置 (留空即为墙壁)")]
    public RoomNode topRoom;
    public RoomNode bottomRoom;
    public RoomNode leftRoom;
    public RoomNode rightRoom;

    public enum ExitDirection { None, Top, Bottom, Left, Right }

    // 事件：方向, 目标房间
    public event Action<ExitDirection, RoomNode> OnPlayerLeft;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        Vector2 playerPos = collision.transform.position;
        Vector2 areaPos = transform.position;
        BoxCollider2D box = GetComponent<BoxCollider2D>();

        if (box == null)
        {
            Debug.LogError($"LeaveArea on {gameObject.name} missing BoxCollider2D!");
            return;
        }

        Vector2 size = new Vector2(box.size.x * transform.localScale.x, box.size.y * transform.localScale.y);
        float offsetX = playerPos.x - areaPos.x;
        float offsetY = playerPos.y - areaPos.y;

        ExitDirection direction = ExitDirection.None;
        RoomNode nextRoom = null;

        // 根据触发器形状判断方向
        if (size.x > size.y)
        {
            // 扁平 -> 上下
            if (offsetY > 0) { direction = ExitDirection.Top; nextRoom = topRoom; }
            else { direction = ExitDirection.Bottom; nextRoom = bottomRoom; }
        }
        else
        {
            // 瘦高 -> 左右
            if (offsetX > 0) { direction = ExitDirection.Right; nextRoom = rightRoom; }
            else { direction = ExitDirection.Left; nextRoom = leftRoom; }
        }

        // 【核心修改点 1】：如果 nextRoom 为 null，说明是墙壁，直接返回，不触发任何事件
        if (nextRoom == null)
        {
            // 可选：如果你希望玩家碰到墙壁有反馈（如播放声音），可以在这里加
            // Debug.Log("Hit a wall.");
            return;
        }

        OnPlayerLeft?.Invoke(direction, nextRoom);
    }
}