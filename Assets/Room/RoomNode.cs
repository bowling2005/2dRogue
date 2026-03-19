using UnityEngine;

public class RoomNode : MonoBehaviour
{
    private BoxCollider2D _roomCollider;
    private GameObject[] _roomContents;

    public float TargetOrthoSize => _roomCollider != null ? _roomCollider.size.y * 0.5f : 5f;

    // 新增：暴露房间的宽高比，防止相机拉伸变形
    public float AspectRatio => _roomCollider != null ? _roomCollider.size.x / _roomCollider.size.y : 16f / 9f;

    private void Awake()
    {
        _roomCollider = GetComponent<BoxCollider2D>();
        if (_roomCollider == null)
        {
            Debug.LogError($"房间 {gameObject.name} 缺少 BoxCollider2D!");
            enabled = false;
            return;
        }

        _roomContents = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            _roomContents[i] = transform.GetChild(i).gameObject;
        }
    }

    public bool IsPlayerFullyInside(Collider2D targetCollider)
    {
        if (targetCollider == null) return false;

        Bounds roomBounds = _roomCollider.bounds;
        Bounds playerBounds = targetCollider.bounds;

        if (playerBounds.min.x < roomBounds.min.x || playerBounds.max.x > roomBounds.max.x)
            return false;
        if (playerBounds.min.y < roomBounds.min.y || playerBounds.max.y > roomBounds.max.y)
            return false;

        return true;
    }

    public void SetRoomActive(bool isActive)
    {
        foreach (var obj in _roomContents)
        {
            if (obj != null) obj.SetActive(isActive);
        }
    }
}