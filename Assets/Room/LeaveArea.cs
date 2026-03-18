using System;
using UnityEngine;

public class LeaveArea : MonoBehaviour
{
    [Header("连接目标")]
    public RoomNode targetRoom;
    public LeaveArea pairedExit;

    private RoomNode parentRoom;
    private Collider2D collider2D;

    public event Action<RoomNode> OnExitTriggered;

    private void Awake()
    {
        collider2D = GetComponent<Collider2D>();
        if (collider2D == null)
        {
            collider2D = gameObject.AddComponent<BoxCollider2D>();
        }
        collider2D.isTrigger = true;
    }

    public void Init(RoomNode room)
    {
        parentRoom = room;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (parentRoom == null || !parentRoom.IsActive) return;
        if (targetRoom == null) return;

        Debug.Log($"[LeaveArea] {gameObject.name} triggered! Target: {targetRoom.roomID}");

        OnExitTriggered?.Invoke(targetRoom);

        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.RequestTransition(parentRoom, targetRoom);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = targetRoom != null ? Color.cyan : Color.red;

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Vector3 center = transform.position;
            Vector3 size = new Vector3(
                box.size.x * transform.localScale.x,
                box.size.y * transform.localScale.y,
                0
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}