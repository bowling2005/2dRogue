using UnityEngine;

/// <summary>
/// 倒带快照 - 使用 Sprite 索引而非引用，减少内存
/// </summary>
[System.Serializable]
public struct RewindSnapshot
{
    public float timeStamp;
    public Vector2 position;
    public float rotation;
    public Vector2 velocity;
    public float health;
    public bool isGrounded;
    public int spriteIndex; 

    public RewindSnapshot(float time, Vector2 pos, float rot, Vector2 vel,
                         float hp, bool grounded, int sprIndex)
    {
        timeStamp = time;
        position = pos;
        rotation = rot;
        velocity = vel;
        health = hp;
        isGrounded = grounded;
        spriteIndex = sprIndex;
    }
}