using UnityEngine;

/// <summary>
/// MapItemUI 的场景ID配置组件
/// 将此脚本挂在每个 MapItemUI 预制体上，通过 Inspector 配置对应的场景 ID
/// </summary>
public class MapItemSceneIdHolder : MonoBehaviour
{
    [SerializeField]
    [Tooltip("这个地图项对应的场景 ID（对应 SceneTable）")]
    private int sceneId;

    public int SceneId => sceneId;
}
