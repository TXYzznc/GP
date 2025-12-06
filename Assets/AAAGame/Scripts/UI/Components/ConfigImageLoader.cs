using UnityEngine;
using UnityEngine.UI;
using GameExtension;
using Cysharp.Threading.Tasks;

/// <summary>
/// 通过配置表ID加载图片的组件（参考 LanguagesTable 的实现方式）
/// </summary>
[RequireComponent(typeof(Image))]
public class ConfigImageLoader : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private int resourceConfigId;
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool setNativeSize = false;
    
    [Header("加载状态")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private bool showLoadingIndicator = false;

    private Image image;
    private bool isLoading = false;

    private void Awake()
    {
        image = GetComponent<Image>();
        
        // 设置默认图片
        if (defaultSprite != null)
        {
            image.sprite = defaultSprite;
        }
    }

    private async void Start()
    {
        if (loadOnStart && resourceConfigId > 0)
        {
            await LoadSprite();
        }
    }

    /// <summary>
    /// 设置配置ID并加载
    /// </summary>
    public async UniTask SetConfigId(int configId)
    {
        resourceConfigId = configId;
        await LoadSprite();
    }

    /// <summary>
    /// 加载图片
    /// </summary>
    public async UniTask LoadSprite()
    {
        if (resourceConfigId <= 0)
        {
            Debug.LogWarning($"[ConfigImageLoader] 资源配置ID无效: {resourceConfigId}", this);
            return;
        }

        if (isLoading)
        {
            Debug.LogWarning($"[ConfigImageLoader] 正在加载中，请勿重复调用", this);
            return;
        }

        isLoading = true;

        try
        {
            // 显示加载指示器（可选）
            if (showLoadingIndicator)
            {
                // 可以在这里显示一个加载动画
            }

            // 异步加载图片
            Sprite sprite = await ResourceExtension.LoadSpriteAsync(resourceConfigId);
            
            if (sprite != null && image != null)
            {
                image.sprite = sprite;
                
                if (setNativeSize)
                {
                    image.SetNativeSize();
                }
                
                Debug.Log($"[ConfigImageLoader] 加载成功: ConfigId={resourceConfigId}, Sprite={sprite.name}", this);
            }
            else
            {
                Debug.LogError($"[ConfigImageLoader] 加载失败: ConfigId={resourceConfigId}", this);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ConfigImageLoader] 加载异常: ConfigId={resourceConfigId}, Error={ex.Message}", this);
        }
        finally
        {
            isLoading = false;
            
            // 隐藏加载指示器
            if (showLoadingIndicator)
            {
                // 隐藏加载动画
            }
        }
    }

    /// <summary>
    /// 清除图片
    /// </summary>
    public void ClearSprite()
    {
        if (image != null)
        {
            image.sprite = defaultSprite;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Load")]
    private void TestLoad()
    {
        LoadSprite().Forget();
    }
#endif
}
