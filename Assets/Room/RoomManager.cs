using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("引用")]
    public GameObject player;
    public Image blackScreen;

    [Header("过渡时间")]
    public float fadeTime = 0.3f;

    private RoomNode currentRoom;
    private RoomNode[] allRooms;
    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p;
        }

        allRooms = FindObjectsOfType<RoomNode>();
    }

    private void Start()
    {
        currentRoom = FindRoomForPlayer();

        if (currentRoom != null)
        {
            foreach (var room in allRooms)
            {
                if (room == currentRoom) room.Activate();
                else room.Deactivate();
            }
            Debug.Log($"[RoomManager] Start Room: {currentRoom.roomID}");
        }
    }

    private RoomNode FindRoomForPlayer()
    {
        if (player == null) return null;

        Vector2 playerPos = player.transform.position;

        foreach (var room in allRooms)
        {
            if (room.ContainsPoint(playerPos))
                return room;
        }

        return allRooms.Length > 0 ? allRooms[0] : null;
    }

    public void RequestTransition(RoomNode from, RoomNode to)
    {
        if (isTransitioning || to == null || to == currentRoom) return;
        if (from != currentRoom) return;

        Debug.Log($"[RoomManager] Transition: {from.roomID} -> {to.roomID}");
        StartCoroutine(Transition(to));
    }

    private IEnumerator Transition(RoomNode nextRoom)
    {
        isTransitioning = true;

        yield return StartCoroutine(Fade(1f));

        currentRoom.Deactivate();
        nextRoom.Activate();

        TeleportPlayer(nextRoom);

        currentRoom = nextRoom;

        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (blackScreen == null) yield break;

        Color start = blackScreen.color;
        Color end = start;
        end.a = targetAlpha;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / fadeTime;
            blackScreen.color = Color.Lerp(start, end, t);
            yield return null;
        }

        blackScreen.color = end;
    }

    private void TeleportPlayer(RoomNode nextRoom)
    {
        if (player == null) return;
        player.transform.position = nextRoom.cameraArea.transform.position;
        Debug.Log($"[RoomManager] Player teleported to {nextRoom.roomID}");
    }

    public RoomNode GetCurrentRoom() => currentRoom;
}