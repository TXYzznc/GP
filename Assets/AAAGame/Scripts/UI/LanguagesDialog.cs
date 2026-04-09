using DG.Tweening;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class LanguagesDialog : UIFormBase
{
    public const string P_LangChangedCb = "LangChangedCb";
    VarAction m_VarAction;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_VarAction = Params.Get<VarAction>(P_LangChangedCb);
        RefreshList();
        DOTween.Kill(gameObject);
        Interactable = false;
        UIAnimationHelper.PopIn(GetComponent<UnityEngine.RectTransform>(), GetComponent<UnityEngine.CanvasGroup>(), 0.3f)
            .OnComplete(() => Interactable = true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(gameObject, true);
        base.OnClose(isShutdown, userData);
    }

    public override void OnClickClose()
    {
        Interactable = false;
        DOTween.Kill(gameObject);
        UIAnimationHelper.PopOut(GetComponent<UnityEngine.RectTransform>(), GetComponent<UnityEngine.CanvasGroup>(), 0.2f)
            .OnComplete(() => GF.UI.Close(this.UIForm));
    }
    void RefreshList()
    {
        var langTb = GF.DataTable.GetDataTable<LanguagesTable>();
        foreach (var lang in langTb)
        {
            var item = this.SpawnItem<UIItemObject>(varLanguageToggle, varToggleGroup.transform);
            (item.itemLogic as LanguageItem).SetData(lang, varToggleGroup, m_VarAction);
        }
    }
}
