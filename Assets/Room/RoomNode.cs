using UnityEngine;

public class RoomNode : MonoBehaviour
{
    [Header("房间设置")]
    public string roomID;

    [Header("相机区域 (必须带BoxCollider2D)")]
    public GameObject cameraArea;

    [Header("出口")]
    public LeaveArea[] exits;

    public bool IsActive { get; private set; }

    private Camera _camera;
    private Camera Cam => _camera ??= Camera.main;

    private void Awake()
    {
        if (string.IsNullOrEmpty(roomID))
            roomID = $"Room_{gameObject.name}";

        if (exits == null || exits.Length == 0)
        {
            exits = GetComponentsInChildren<LeaveArea>();
        }

        foreach (var exit in exits)
        {
            if (exit != null) exit.Init(this);
        }
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        gameObject.SetActive(true);
        FitCamera();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        gameObject.SetActive(false);
    }

    private void FitCamera()
    {
        if (cameraArea == null) return;

        var box = cameraArea.GetComponent<BoxCollider2D>();
        if (box == null) return;

        float worldHeight = box.size.y * cameraArea.transform.localScale.y;

        Cam.transform.position = new Vector3(
            cameraArea.transform.position.x,
            cameraArea.transform.position.y,
            Cam.transform.position.z
        );
        Cam.orthographicSize = worldHeight * 0.5f;
    }

    public bool ContainsPoint(Vector2 point)
    {
        if (cameraArea == null) return false;

        var box = cameraArea.GetComponent<BoxCollider2D>();
        if (box == null) return false;

        Vector2 center = cameraArea.transform.position;
        Vector2 size = new Vector2(
            box.size.x * cameraArea.transform.localScale.x,
            box.size.y * cameraArea.transform.localScale.y
        );

        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        return point.x >= center.x - halfW && point.x <= center.x + halfW &&
               point.y >= center.y - halfH && point.y <= center.y + halfH;
    }

    private void OnDrawGizmos()
    {
        if (cameraArea == null) return;

        var box = cameraArea.GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.color = IsActive ? Color.green : Color.gray;
        Vector3 center = cameraArea.transform.position;
        Vector3 size = new Vector3(
            box.size.x * cameraArea.transform.localScale.x,
            box.size.y * cameraArea.transform.localScale.y,
            0
        );
        Gizmos.DrawWireCube(center, size);
    }
}