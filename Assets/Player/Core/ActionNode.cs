using UnityEngine;

public enum ActionState
{
    Queued,
    Processing,
    Completed,
    Disabled
}

public class ActionNode
{
    public int actionID;
    public ActionState state;
    public float bufferDuration;
    public float elapsedBufferTime;
    public object context;

    public ActionNode(int id, float bufferSec = 0.2f, object ctx = null)
    {
        actionID = id;
        state = ActionState.Queued;
        bufferDuration = bufferSec;
        elapsedBufferTime = 0f;
        context = ctx;
    }

    public bool UpdateBuffer(float deltaTime)
    {
        elapsedBufferTime += deltaTime;

        // ¡¾µ÷ÊÔ¡¿´òÓ¡»º³å×´Ì¬
        if (elapsedBufferTime % 0.1f < deltaTime)
        {
            Debug.Log($"[Buffer] »º³åÖÐ... {elapsedBufferTime:F2}s / {bufferDuration:F2}s");
        }

        return elapsedBufferTime < bufferDuration;
    }
}