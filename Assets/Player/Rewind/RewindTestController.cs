// Assets/Scripts/Test/RewindTestController.cs
using UnityEngine;

/// <summary>
/// 测试脚本：控制玩家和测试物体同步移动，验证回退一致性
/// </summary>
public class RewindTestController : MonoBehaviour
{
    [Header("测试对象")]
    public Transform testObject;

    private Rigidbody2D testRb;
    private bool isGrounded;
    private float moveSpeed = 3f;

    void Awake()
    {
        testRb = testObject?.GetComponent<Rigidbody2D>();
    }
    void Update()
    {
       
        if (testObject != null && !RewindSystem.Instance.IsRewinding)
        {
            // 轻微独立运动：按方向键时测试物体会偏移
            float v = Input.GetAxisRaw("Horizontal");
            testObject.position += Vector3.right * v * moveSpeed * 0.5f * Time.deltaTime;

            if (Input.GetKey(KeyCode.K))
            {
                gameObject.SetActive(false);
            }

        }
    }
}