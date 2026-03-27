using UnityEngine;

/// <summary>
/// 棋子移动接口
/// 定义移动行为，后续可替换为NavMesh实现
/// </summary>
public interface IChessMovement
{
    /// <summary>是否正在移动</summary>
    bool IsMoving { get; }

    /// <summary>移动速度</summary>
    float MoveSpeed { get; set; }

    /// <summary>
    /// 移动到目标位置
    /// </summary>
    /// <param name="targetPosition">目标位置</param>
    void MoveTo(Vector3 targetPosition);

    /// <summary>
    /// 停止移动
    /// </summary>
    void Stop();

    /// <summary>
    /// 每帧更新（由ChessEntity调用）
    /// </summary>
    void Tick(float deltaTime);
}
