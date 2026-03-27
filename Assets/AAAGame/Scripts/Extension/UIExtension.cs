using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using GameExtension;
using UnityEngine.U2D;
using DG.Tweening;
using System;
using UnityEngine.UI;

public static class UIExtension
{
    /// <summary>
    /// 异步加载并设置Sprite
    /// </summary>
    /// <param name="image"></param>
    /// <param name="spriteName"></param>
    public static void SetSprite(this Image image, string spriteName, bool resize = false)
    {
        spriteName = UtilityBuiltin.AssetsPath.GetSpritesPath(spriteName);
        GF.UI.LoadSprite(spriteName, sp =>
        {
            if (sp != null)
            {
                image.sprite = sp;
                if (resize) image.SetNativeSize();
            }
        });
    }

    /// <summary>
    /// 通过配置表ID异步加载并设置Sprite
    /// </summary>
    /// <param name="image">目标Image组件</param>
    /// <param name="configId">资源配置表ID（使用ResourceIds中的常量）</param>
    /// <param name="alpha">不透明度（0-1，默认1为完全不透明）</param>
    /// <param name="size">缩放大小（默认为(1,1,1)），设置Image.transform.localScale</param>
    /// <param name="resize">是否自动调整大小</param>
    public static void SetSpriteById(this Image image, int configId, float alpha = 1f, Vector3? size = null, bool resize = false)
    {
        ResourceExtension.LoadSpriteAsync(configId, image, errorMsg =>
        {
            DebugEx.ErrorModule("UIExtension", $"SetSpriteById failed: {errorMsg}");
        }, alpha, size);

        if (resize)
        {
            // ✅ resize 延迟执行，确保sprite已赋值
            image.SetNativeSize();
        }
    }

    /// <summary>
    /// 异步加载并设置Texture
    /// </summary>
    /// <param name="rawImage"></param>
    /// <param name="spriteName"></param>
    public static void SetTexture(this RawImage rawImage, string spriteName, bool resize = false)
    {
        spriteName = UtilityBuiltin.AssetsPath.GetTexturePath(spriteName);
        GF.UI.LoadTexture(spriteName, tex =>
        {
            if (tex != null)
            {
                rawImage.texture = tex;
                if (resize) rawImage.SetNativeSize();
            }
        });
    }

    /// <summary>
    /// 通过配置表ID异步加载并设置Texture
    /// </summary>
    /// <param name="rawImage">目标RawImage组件</param>
    /// <param name="configId">资源配置表ID（使用ResourceIds中的常量）</param>
    /// <param name="resize">是否自动调整大小</param>
    public static void SetTextureById(this RawImage rawImage, int configId, bool resize = false)
    {
        ResourceExtension.LoadAssetByConfigAsync<Texture2D>(configId, texture =>
        {
            if (texture != null)
            {
                rawImage.texture = texture;
                if (resize) rawImage.SetNativeSize();
            }
        });
    }

    /// <summary>
    /// 判断是否点击在UI上
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="mousePosition"></param>
    /// <returns></returns>
    public static bool IsPointerOverUIObject(this UIComponent uiCom, Vector3 mousePosition)
    {
        return UtilityEx.IsPointerOverUIObject(mousePosition);
    }

    /// <summary>
    /// 世界坐标转换到UI屏幕坐标
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="worldPos">世界坐标点</param>
    /// <returns></returns>
    public static Vector3 PositionWorldToUI(this UIComponent uiCom, Vector3 worldPos, RectTransform targetRect)
    {
        var viewPos = GF.Scene.MainCamera.WorldToViewportPoint(worldPos);
        var uiPos = GF.UICamera.ViewportToScreenPoint(viewPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, uiPos, GF.UICamera, out var localPoint);
        return localPoint;
    }
    /// <summary>
    /// 加载Sprite图集
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="atlasName"></param>
    /// <param name="onSpriteAtlasLoaded"></param>
    public static void LoadSpriteAtlas(this UIComponent uiCom, string atlasName, GameFrameworkAction<SpriteAtlas> onSpriteAtlasLoaded)
    {
        if (GF.Resource.HasAsset(atlasName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("LoadSpriteAtlas失败, 资源不存在:{0}", atlasName);
            return;
        }

        GF.Resource.LoadAsset(atlasName, typeof(SpriteAtlas), new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            var spAtlas = asset as SpriteAtlas;
            onSpriteAtlasLoaded.Invoke(spAtlas);
        }));
    }
    /// <summary>
    /// 异步加载Sprite
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="spriteName"></param>
    /// <param name="onSpriteLoaded"></param>
    public static void LoadSprite(this UIComponent uiCom, string spriteName, GameFrameworkAction<Sprite> onSpriteLoaded)
    {
        if (GF.Resource.HasAsset(spriteName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("UIExtension.SetSprite()失败, 资源不存在:{0}", spriteName);
            return;
        }
        GF.Resource.LoadAsset(spriteName, typeof(Sprite), new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            Sprite resultSp = asset as Sprite;
            onSpriteLoaded.Invoke(resultSp);
        }));
    }
    /// <summary>
    /// 异步加载Texture
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="spriteName"></param>
    /// <param name="onSpriteLoaded"></param>
    public static void LoadTexture(this UIComponent uiCom, string spriteName, GameFrameworkAction<Texture2D> onSpriteLoaded)
    {
        if (GF.Resource.HasAsset(spriteName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("UIExtension.LoadTexture()失败, 资源不存在:{0}", spriteName);
            return;
        }
        GF.Resource.LoadAsset(spriteName, typeof(Texture2D), new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            Texture2D resultSp = asset as Texture2D;
            onSpriteLoaded.Invoke(resultSp);
        }));
    }
    /// <summary>
    /// Destory指定根节点下的所有子节点
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="parent"></param>
    public static void RemoveAllChildren(this UIComponent ui, Transform parent)
    {
        foreach (Transform child in parent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    /// <summary>
    /// 显示Toast提示
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="text"></param>
    /// <param name="duration"></param>
    public static void ShowToast(this UIComponent ui, string text, ToastStyle style = ToastStyle.Blue, float duration = 2)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarString>(ToastTips.P_Text, text);
        uiParams.Set<VarFloat>(ToastTips.P_Duration, duration);
        uiParams.Set<VarUInt32>(ToastTips.P_Style, (uint)style);
        var tipsGroup = ui.GetUIGroup(Const.UIGroup.Tips.ToString());
        if (tipsGroup.CurrentUIForm != null)
        {
            uiParams.SortOrder = ((tipsGroup.CurrentUIForm as UIForm).Logic as UIFormBase).Params.SortOrder + 1;
        }
        ui.OpenUIForm(UIViews.ToastTips, uiParams);
    }

    /// <summary>
    /// 显示悬浮提示框（跟随鼠标位置）
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="text">提示文本</param>
    /// <param name="offset">相对鼠标的偏移量</param>
    /// <returns>返回打开的UI表单ID</returns>
    public static int ShowFloatingTip(this UIComponent ui, string text, Vector2? offset = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return -1;
        }

        // 默认偏移量（鼠标右上方）
        Vector2 tipOffset = offset ?? new Vector2(20f, 20f);

        var uiParams = UIParams.Create();
        var tipsGroup = ui.GetUIGroup(Const.UIGroup.Tips.ToString());
        if (tipsGroup.CurrentUIForm != null)
        {
            uiParams.SortOrder = ((tipsGroup.CurrentUIForm as UIForm).Logic as UIFormBase).Params.SortOrder + 1;
        }

        int formId = ui.OpenUIForm(UIViews.FloatingBoxTip, uiParams);

        // 等待UI打开后设置数据和位置
        ui.StartCoroutine(SetFloatingTipDataCoroutine(ui, formId, text, Input.mousePosition + (Vector3)tipOffset));

        return formId;
    }

    /// <summary>
    /// 显示悬浮提示框（相对于指定UI元素）
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="text">提示文本</param>
    /// <param name="targetRect">目标UI元素</param>
    /// <param name="offset">相对目标的偏移量</param>
    /// <returns>返回打开的UI表单ID</returns>
    public static int ShowFloatingTipAt(this UIComponent ui, string text, RectTransform targetRect, Vector2? offset = null)
    {
        if (string.IsNullOrEmpty(text) || targetRect == null)
        {
            Log.Warning($"ShowFloatingTipAt 参数无效: text={text}, targetRect={targetRect}");
            return -1;
        }

        // 默认偏移量（目标右上方）
        Vector2 tipOffset = offset ?? new Vector2(10f, 10f);

        // 检查是否已经有 FloatingBoxTip 打开
        var tipsGroup = ui.GetUIGroup(Const.UIGroup.Tips.ToString());
        if (tipsGroup != null)
        {
            var existingForms = tipsGroup.GetUIForms(ui.GetUIFormAssetName(UIViews.FloatingBoxTip));
            if (existingForms != null && existingForms.Length > 0)
            {
                // 复用已存在的提示框
                var existingForm = existingForms[0];
                var floatingTip = (existingForm as UIForm).Logic as FloatingBoxTip;
                if (floatingTip != null)
                {
                    floatingTip.SetData(text);
                    floatingTip.SetPositionRelativeTo(targetRect, tipOffset);
                    
                    // 确保显示（使用 OnResume 恢复显示）
                    existingForm.OnResume();
                    
                    Log.Info($"复用悬浮提示框: {text}");
                    return existingForm.SerialId;
                }
            }
        }

        // 如果没有现有的,创建新的
        var uiParams = UIParams.Create();
        if (tipsGroup != null && tipsGroup.CurrentUIForm != null)
        {
            uiParams.SortOrder = ((tipsGroup.CurrentUIForm as UIForm).Logic as UIFormBase).Params.SortOrder + 1;
        }

        Log.Info($"准备打开 FloatingBoxTip: UIViews.FloatingBoxTip={(int)UIViews.FloatingBoxTip}");
        
        int formId = ui.OpenUIForm(UIViews.FloatingBoxTip, uiParams);
        
        if (formId == -1)
        {
            Log.Error("OpenUIForm 返回 -1，FloatingBoxTip 打开失败！可能原因：");
            Log.Error("1. UITable 中没有 ID=14 的配置");
            Log.Error("2. UIGroupTable 中没有对应的 UIGroup 配置");
            Log.Error("3. 预制体路径错误或资源不存在");
            return -1;
        }
        
        Log.Info($"OpenUIForm 返回 formId={formId}");
        
        // 立即检查是否正在加载
        if (ui.IsLoadingUIForm(formId))
        {
            Log.Info($"FloatingBoxTip 正在加载中: formId={formId}");
        }
        else
        {
            Log.Warning($"FloatingBoxTip 不在加载队列中: formId={formId}");
        }

        // 等待UI打开后设置数据和位置
        ui.StartCoroutine(SetFloatingTipDataAtCoroutine(ui, formId, text, targetRect, tipOffset));

        return formId;
    }

    /// <summary>
    /// 协程：设置悬浮提示框数据（屏幕坐标）
    /// </summary>
    private static System.Collections.IEnumerator SetFloatingTipDataCoroutine(UIComponent ui, int formId, string text, Vector3 screenPosition)
    {
        // 等待UI打开
        while (!ui.HasUIForm(formId))
        {
            yield return null;
        }

        var uiForm = ui.GetUIForm(formId);
        if (uiForm != null)
        {
            var floatingTip = (uiForm as UIForm).Logic as FloatingBoxTip;
            if (floatingTip != null)
            {
                floatingTip.SetData(text);
                floatingTip.SetPosition(screenPosition);
            }
        }
    }

    /// <summary>
    /// 协程：设置悬浮提示框数据（相对于目标）
    /// </summary>
    private static System.Collections.IEnumerator SetFloatingTipDataAtCoroutine(UIComponent ui, int formId, string text, RectTransform targetRect, Vector2 offset)
    {
        Log.Info($"SetFloatingTipDataAtCoroutine 开始: formId={formId}");
        
        int waitFrames = 0;
        // 等待UI打开（最多等待60帧，约1秒）
        while (!ui.HasUIForm(formId) && waitFrames < 60)
        {
            waitFrames++;
            yield return null;
        }

        if (!ui.HasUIForm(formId))
        {
            Log.Error($"等待超时！FloatingBoxTip 未能打开: formId={formId}, 等待了 {waitFrames} 帧");
            yield break;
        }

        Log.Info($"FloatingBoxTip 已打开，等待了 {waitFrames} 帧");

        var uiForm = ui.GetUIForm(formId);
        if (uiForm == null)
        {
            Log.Error($"GetUIForm 返回 null: formId={formId}");
            yield break;
        }

        Log.Info($"获取到 UIForm: {uiForm.GetType().Name}");

        var floatingTip = (uiForm as UIForm).Logic as FloatingBoxTip;
        if (floatingTip == null)
        {
            Log.Error($"Logic 转换为 FloatingBoxTip 失败: Logic={((uiForm as UIForm).Logic?.GetType().Name ?? "null")}");
            yield break;
        }

        Log.Info($"成功获取 FloatingBoxTip 组件");

        floatingTip.SetData(text);
        Log.Info($"SetData 完成: text={text}");

        floatingTip.SetPositionRelativeTo(targetRect, offset);
        Log.Info($"SetPositionRelativeTo 完成: offset={offset}");

        // 检查 GameObject 是否激活
        if (floatingTip.gameObject != null)
        {
            Log.Info($"FloatingBoxTip GameObject 状态: active={floatingTip.gameObject.activeSelf}, activeInHierarchy={floatingTip.gameObject.activeInHierarchy}");
            Log.Info($"FloatingBoxTip 位置: {floatingTip.transform.position}");
        }
    }

    /// <summary>
    /// 关闭所有悬浮提示框
    /// </summary>
    public static void CloseAllFloatingTips(this UIComponent ui)
    {
        ui.CloseUIForms(UIViews.FloatingBoxTip, Const.UIGroup.Tips.ToString());
    }

    /// <summary>
    /// 打开UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="viewId">UI界面id(传入自动生成的UIViews枚举值)</param>
    /// <param name="parms"></param>
    /// <returns></returns>
    public static int OpenUIForm(this UIComponent uiCom, UIViews viewId, UIParams parms = null)
    {
        var uiTb = GF.DataTable.GetDataTable<UITable>();
        var uiGroupTb = GF.DataTable.GetDataTable<UIGroupTable>();
        int uiId = (int)viewId;
        if (!uiTb.HasDataRow(uiId))
        {
            Log.Error("UITable表不存在id:{0}", uiId);
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return -1;
        }
        var uiRow = uiTb.GetDataRow(uiId);
        if (!uiGroupTb.HasDataRow(uiRow.UIGroupId))
        {
            Log.Error("UIGroupTable表不存在id:{0}", uiId);
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return -1;
        }
        var uiGroupRow = uiGroupTb.GetDataRow(uiRow.UIGroupId);
        string uiName = UtilityBuiltin.AssetsPath.GetUIFormPath(uiRow.UIPrefab);
        if (uiCom.IsLoadingUIForm(uiName))
        {
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return -1;
        }
        parms ??= UIParams.Create();
        parms.AllowEscapeClose ??= uiRow.EscapeClose;
        parms.SortOrder ??= uiRow.SortOrder + uiGroupRow.Depth;
        return uiCom.OpenUIForm(uiName, uiGroupRow.Name, uiRow.PauseCoveredUI, parms);
    }

    /// <summary>
    /// 关闭UI界面(关闭前播放UI界面关闭动画)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="ui"></param>
    public static void Close(this UIComponent uiCom, UIForm ui)
    {
        Close(uiCom, ui.SerialId);
    }
    /// <summary>
    /// 关闭UI界面(关闭前播放UI界面关闭动画)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="uiFormId"></param>
    public static void Close(this UIComponent uiCom, int uiFormId)
    {
        if (uiCom.IsLoadingUIForm(uiFormId))
        {
            GF.UI.CloseUIForm(uiFormId);
            return;
        }
        if (!uiCom.HasUIForm(uiFormId))
        {
            return;
        }
        var uiForm = uiCom.GetUIForm(uiFormId);
        UIFormBase logic = uiForm.Logic as UIFormBase;
        logic.CloseWithAnimation();
    }
    /// <summary>
    /// 关闭整个UI组的所有UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="groupName"></param>
    public static void CloseUIForms(this UIComponent uiCom, string groupName)
    {
        var group = uiCom.GetUIGroup(groupName);
        var all = group.GetAllUIForms();
        foreach (var item in all)
        {
            uiCom.CloseUIForm(item.SerialId);
        }
    }
    /// <summary>
    /// 判断UI界面是否正在加载队列(还没有实体化)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool IsLoadingUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        return uiCom.IsLoadingUIForm(assetName);
    }
    /// <summary>
    /// 是否已经打开UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool HasUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        if (string.IsNullOrEmpty(assetName))
        {
            return false;
        }

        return uiCom.HasUIForm(assetName);
    }
    /// <summary>
    /// 获取UI界面的prefab资源名
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static string GetUIFormAssetName(this UIComponent uiCom, UIViews view)
    {
        if (GF.DataTable == null || !GF.DataTable.HasDataTable<UITable>())
        {
            Log.Warning("GetUIFormAssetName is empty.");
            return string.Empty;
        }

        var uiTb = GF.DataTable.GetDataTable<UITable>();
        if (!uiTb.HasDataRow((int)view))
        {
            return string.Empty;
        }
        string uiName = UtilityBuiltin.AssetsPath.GetUIFormPath(uiTb.GetDataRow((int)view).UIPrefab);
        return uiName;
    }
    /// <summary>
    /// 关闭所有打开的某个界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <param name="uiGroup"></param>
    public static void CloseUIForms(this UIComponent uiCom, UIViews view, string uiGroup = null)
    {
        string uiAssetName = uiCom.GetUIFormAssetName(view);
        GameFramework.UI.IUIForm[] uIForms;
        if (string.IsNullOrEmpty(uiGroup))
        {
            uIForms = uiCom.GetUIForms(uiAssetName);
        }
        else
        {
            if (!uiCom.HasUIGroup(uiGroup))
            {
                return;
            }
            uIForms = uiCom.GetUIGroup(uiGroup).GetUIForms(uiAssetName);
        }

        foreach (var item in uIForms)
        {
            uiCom.Close(item.SerialId);
        }
    }
    /// <summary>
    /// 刷新所有UI的多语言文本(当语言切换时需调用),用于即时改变多语言文本
    /// </summary>
    /// <param name="uiCom"></param>
    public static void UpdateLocalizationTexts(this UIComponent uiCom)
    {
        //foreach (var item in Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>())
        //{
        //    item.ClearFontAssetData();
        //}
        foreach (UIForm uiForm in uiCom.GetAllLoadedUIForms())
        {
            (uiForm.Logic as UIFormBase).InitLocalization();
        }
        var uiObjectPool = GF.ObjectPool.GetObjectPool(pool => pool.FullName == "GameFramework.UI.UIManager+UIFormInstanceObject.UI Instance Pool");
        if (uiObjectPool != null)
        {
            uiObjectPool.ReleaseAllUnused();
        }
    }
    /// <summary>
    /// 获取当前顶层的UI界面id(排除子界面)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <returns></returns>
    public static int GetTopUIFormId(this UIComponent uiCom)
    {
        var dialogGp = uiCom.GetUIGroup(Const.UIGroup.Dialog.ToString());
        var allUIForms = dialogGp.GetAllUIForms();
        int maxSortOrder = -1;
        int maxOrderIndex = -1;
        for (int i = 0; i < allUIForms.Length; i++)
        {
            var uiBase = (allUIForms[i] as UIForm).Logic as UIFormBase;
            if (uiBase == null || uiBase.Params.IsSubUIForm) continue;

            int curOrder = uiBase.SortOrder;
            if (curOrder >= maxSortOrder)
            {
                maxSortOrder = curOrder;
                maxOrderIndex = i;
            }
        }
        if (maxOrderIndex != -1) return allUIForms[maxOrderIndex].SerialId;

        maxSortOrder = -1;
        maxOrderIndex = -1;
        var uiFormGp = uiCom.GetUIGroup(Const.UIGroup.UIForm.ToString());
        allUIForms = uiFormGp.GetAllUIForms();
        for (int i = 0; i < allUIForms.Length; i++)
        {
            var uiBase = (allUIForms[i] as UIForm).Logic as UIFormBase;
            if (uiBase == null || uiBase.Params.IsSubUIForm) continue;

            int curOrder = uiBase.SortOrder;
            if (curOrder >= maxSortOrder)
            {
                maxSortOrder = curOrder;
                maxOrderIndex = i;
            }
        }
        if (maxOrderIndex != -1) return allUIForms[maxOrderIndex].SerialId;
        return -1;
    }
    public static void ShowRewardEffect(this UIComponent uiCom, Vector3 centerPos, Vector3 fly2Pos, float flyDelay = 0.5f, GameFrameworkAction onAnimComplete = null, int num = 30)
    {
        int coinNum = num;
        DOVirtual.DelayedCall(flyDelay, () =>
        {
            // TODO: 请在 ResourceConfigTable 中配置 add_money.wav 并替换此处的 ID
            int soundId = 0; 
            if (soundId > 0) GF.Sound.PlayEffect(soundId);
            else Log.Warning("UIExtension: add_money.wav 未配置 ID");
        });
        var richText = "<sprite name=USD_0>";
        for (int i = 0; i < num; i++)
        {
            var animPrams = EntityParams.Create(centerPos, Vector3.zero, Vector3.one);
            animPrams.OnShowCallback = moneyEntity =>
            {
                moneyEntity.GetComponent<TMPro.TextMeshPro>().text = richText;
                var spawnPos = UnityEngine.Random.insideUnitCircle * 3;
                var expPos = centerPos;
                expPos.x += spawnPos.x;
                expPos.y += spawnPos.y;
                var targetPos = fly2Pos;
                int moneyEntityId = moneyEntity.Entity.Id;
                var expDuration = Vector2.Distance(moneyEntity.transform.position, expPos) * 0.05f;// Mathf.Clamp(Vector3.Distance(moneyEntity.transform.position, expPos)*0.01f, 0.1f, 0.4f);
                var animSeq = DOTween.Sequence();
                animSeq.Append(moneyEntity.transform.DOMove(expPos, expDuration));
                animSeq.AppendInterval(0.25f);
                var moveDuration = Vector2.Distance(expPos, targetPos) * 0.05f;// Mathf.Clamp(Vector3.Distance(expPos, targetPos)*0.01f, 0.1f, 0.8f);
                animSeq.Append(moneyEntity.transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear));
                animSeq.onComplete = () =>
                {
                    GF.Entity.HideEntitySafe(moneyEntityId);
                    coinNum--;
                    if (coinNum <= 0)
                    {
                        onAnimComplete?.Invoke();
                    }
                    // TODO: 请在 ResourceConfigTable 中配置 Collect_Gem_2.wav 并替换此处的 ID
                    int soundId = 0;
                    if (soundId > 0) GF.Sound.PlayEffect(soundId);
                    GF.Sound.PlayVibrate();
                };
            };
            GF.Entity.ShowEntity<EntityBase>("Effect/EffectMoney", Const.EntityGroup.Effect, animPrams);
        }
    }


    #region Unity UI Extension
    public static void SetAnchoredPositionX(this RectTransform rectTransform, float anchoredPositionX)
    {
        var value = rectTransform.anchoredPosition;
        value.x = anchoredPositionX;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPositionY(this RectTransform rectTransform, float anchoredPositionY)
    {
        var value = rectTransform.anchoredPosition;
        value.y = anchoredPositionY;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPosition3DZ(this RectTransform rectTransform, float anchoredPositionZ)
    {
        var value = rectTransform.anchoredPosition3D;
        value.z = anchoredPositionZ;
        rectTransform.anchoredPosition3D = value;
    }
    public static void SetColorAlpha(this UnityEngine.UI.Graphic graphic, float alpha)
    {
        var value = graphic.color;
        value.a = alpha;
        graphic.color = value;
    }
    public static void SetFlexibleSize(this LayoutElement layoutElement, Vector2 flexibleSize)
    {
        layoutElement.flexibleWidth = flexibleSize.x;
        layoutElement.flexibleHeight = flexibleSize.y;
    }
    public static Vector2 GetFlexibleSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.flexibleWidth, layoutElement.flexibleHeight);
    }
    public static void SetMinSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
    }
    public static Vector2 GetMinSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.minWidth, layoutElement.minHeight);
    }
    public static void SetPreferredSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
    }
    public static Vector2 GetPreferredSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
    }
    #endregion
    public enum ToastStyle : uint
    {
        Blue = 0,
        Yellow = 1,
        Green = 2,
        Red = 3,
        White = 4
    }
}
