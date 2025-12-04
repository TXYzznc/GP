using UnityEngine;

public class LayerTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"物体: {gameObject.name}");
        Debug.Log($"Layer 编号: {gameObject.layer}");
        Debug.Log($"Layer 名称: {LayerMask.LayerToName(gameObject.layer)}");
        
        // 测试 LayerMask
        int enemyMask = LayerMask.GetMask("Enemy");
        Debug.Log($"Enemy LayerMask 值: {enemyMask}");
        
        int objectLayerMask = 1 << gameObject.layer;
        bool isInEnemyLayer = (objectLayerMask & enemyMask) != 0;
        Debug.Log($"是否在 Enemy 层: {isInEnemyLayer}");
    }
}