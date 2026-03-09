public enum ActionState
{
    Unused = 0,     // 未使用 (默认值)
    Queued = 1,     // 进入预输入队列 (等待执行)
    Processing = 2, // 处理中 (正在执行)
    Completed = 3,  // 处理完成 (等待移除)
    Disabled = 4    // 禁用 (条件不满足，如冷却中)
}