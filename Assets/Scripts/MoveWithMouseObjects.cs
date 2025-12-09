using UnityEngine;

public class MoveWithMouseObjects : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveFactor = 0.1f; // 鼠标移动反向系数
    [SerializeField] private float maxMoveDistance = 1.0f; // 最大移动距离

    [Header("返回设置")]
    [SerializeField] private float returnSpeed = 3f; // 返回原位的速度

    // 私有变量
    private Vector2 originalPosition; // 原始位置

    private Vector2 lastMouseWorldPosition; // 上一帧的鼠标世界位置
    private Vector2 currentMouseWorldPosition; // 当前鼠标世界位置

    private bool isReturningToOrigin = false; // 是否正在返回原位

    // 移动的累积量
    private Vector2 accumulatedMove;

    private void Start()
    {
        // 记录初始位置
        originalPosition = transform.position;

        // 初始化鼠标世界位置
        Vector3 mouseScreenPos = Input.mousePosition;
        currentMouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Camera.main.transform.position.z - transform.position.z
        ));
        lastMouseWorldPosition = currentMouseWorldPosition;
    }

    private void Update()
    {
        // 更新鼠标世界位置
        Vector3 mouseScreenPos = Input.mousePosition;
        lastMouseWorldPosition = currentMouseWorldPosition;
        currentMouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Camera.main.transform.position.z - transform.position.z
        ));

        if (isReturningToOrigin)
        {
            // 正在返回原位状态
            ReturnToOrigin();
        }
        else
        {
            // 正常受鼠标影响状态
            ProcessMouseInput();
        }
    }

    private void ProcessMouseInput()
    {
        // 计算鼠标在世界坐标系中的位移
        Vector2 mouseWorldDelta = currentMouseWorldPosition - lastMouseWorldPosition;

        // 将鼠标位移转换为反向的世界位移
        Vector2 objectMoveDelta = new Vector2(-mouseWorldDelta.x, -mouseWorldDelta.y) * moveFactor;

        // 累积移动量
        accumulatedMove += objectMoveDelta;

        // 计算目标位置
        Vector2 targetPosition = originalPosition + accumulatedMove;

        // 检查是否超出范围
        if (CheckBounds(targetPosition))
        {
            isReturningToOrigin = true;
            return;
        }

        // 如果没有超出范围，应用移动
        if (!isReturningToOrigin)
        {
            transform.position = targetPosition;
        }
    }

    private bool CheckBounds(Vector2 targetPosition)
    {
        // 检查移动距离是否超出范围
        float currentDistance = Vector2.Distance(targetPosition, originalPosition);
        return currentDistance > maxMoveDistance;
    }

    private void ReturnToOrigin()
    {
        // 平滑返回原始位置
        transform.position = Vector2.Lerp(
            transform.position,
            originalPosition,
            returnSpeed * Time.deltaTime
        );

        // 检查是否已经接近原点
        float positionDistance = Vector2.Distance(transform.position, originalPosition);

        // 如果已经足够接近原点，重置状态
        if (positionDistance < 0.01f)
        {
            // 完全重置到原点
            transform.position = originalPosition;

            // 重置累积量
            accumulatedMove = Vector2.zero;

            // 切换回正常状态
            isReturningToOrigin = false;
        }
    }

    // 调试辅助方法
    private void OnDrawGizmosSelected()
    {
        // 绘制移动范围
        Gizmos.color = Color.green;
        Vector2 origin = Application.isPlaying ? originalPosition : (Vector2)transform.position;
        Gizmos.DrawWireSphere(origin, maxMoveDistance);

        // 绘制当前鼠标位置
        if (Application.isPlaying && !isReturningToOrigin)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentMouseWorldPosition);
        }
    }

    // 重置到原始状态
    public void ResetToOrigin()
    {
        isReturningToOrigin = true;
    }

    // 获取当前是否正在返回原点
    public bool IsReturningToOrigin()
    {
        return isReturningToOrigin;
    }
}