using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameFramework.Event;
using System.Collections.Generic;
using DG.Tweening;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class OutsiderFunctionUI : StateAwareUIForm
{
    #region �ֶ�

    private List<FunctionItem> m_FunctionItems = new List<FunctionItem>();

    // ���ܰ�ť����
    private readonly string[] m_FunctionNames = new string[]
    {
        "ͼ��",
        "�̵�",
        "����",
        "�ٻ���",
        "��ս"
    };

    #endregion

    #region �¼�����

    protected override void SubscribeEvents()
    {
        Log.Info("OutsiderFunctionUI: ���ľ���״̬�¼�");
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    protected override void UnsubscribeEvents()
    {
        Log.Info("OutsiderFunctionUI: ȡ�����ľ���״̬�¼�");
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    #endregion

    #region �¼�����

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        Log.Info("OutsiderFunctionUI: �յ���������¼�");
        ShowUI();
        RefreshFunctions();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        Log.Info("OutsiderFunctionUI: �յ������뿪�¼�");
        HideUI();
    }

    #endregion

    #region UI ˢ��

    /// <summary>
    /// ˢ�¹��ܰ�ť
    /// </summary>
    private void RefreshFunctions()
    {
        // �����ɵĹ�����
        ClearFunctionItems();

        // �������ܰ�ť
        for (int i = 0; i < m_FunctionNames.Length; i++)
        {
            CreateFunctionItem(m_FunctionNames[i], i);
        }

        Log.Info("OutsiderFunctionUI: ���ܰ�ť��ˢ��");
    }

    /// <summary>
    /// ����������
    /// </summary>
    private void CreateFunctionItem(string functionName, int index)
    {
        if (varFunctionItem == null || varOutsiderFunctionPanel == null)
        {
            Log.Warning("OutsiderFunctionUI: ������ģ������δ����");
            return;
        }

        // ʵ����������
        GameObject itemObj = Instantiate(varFunctionItem, varOutsiderFunctionPanel.transform);
        itemObj.SetActive(true);

        // ��ȡ FunctionItem ���
        FunctionItem functionItem = itemObj.GetComponent<FunctionItem>();
        if (functionItem != null)
        {
            functionItem.SetData(functionName, () => OnFunctionClicked(functionName));
            m_FunctionItems.Add(functionItem);
        }
        else
        {
            Log.Error("OutsiderFunctionUI: ��������δ�ҵ� FunctionItem ���");
            Destroy(itemObj);
        }
    }

    /// <summary>
    /// ����������
    /// </summary>
    private void ClearFunctionItems()
    {
        foreach (var item in m_FunctionItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        m_FunctionItems.Clear();
    }

    #endregion

    #region ���ܰ�ť�ص�

    /// <summary>
    /// ���ܰ�ť����ص�
    /// </summary>
    private void OnFunctionClicked(string functionName)
    {
        Log.Info($"OutsiderFunctionUI: ����˹��ܰ�ť - {functionName}");

        // TODO: ���ݹ������ƴ򿪶�Ӧ��UI
        switch (functionName)
        {
            case "ͼ��":
                // ��ͼ��UI
                break;
            case "�̵�":
                // ���̵�UI
                break;
            case "����":
                // �򿪿���UI
                break;
            case "�ٻ���":
                // ���ٻ���UI
                break;
            case "��ս":
                // ����սUI
                break;
        }
    }

    #endregion

    #region ��������

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearFunctionItems();
        base.OnClose(isShutdown, userData);
    }

    #endregion

    #region 动画

    protected new void ShowUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.ShowUI(); return; }
        DOTween.Kill(gameObject);
        cg.alpha = 0f; cg.blocksRaycasts = true; cg.interactable = true;
        var orig = rt.anchoredPosition;
        rt.anchoredPosition = orig + new Vector2(0, -50f);
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuart))
            .Join(rt.DOAnchorPos(orig, 0.3f).SetEase(Ease.OutQuart))
            .OnComplete(() =>
            {
                if (varOutsiderFunctionPanel != null)
                    UIAnimationHelper.StaggerChildren(varOutsiderFunctionPanel.transform, 0.05f, 0.2f);
            });
    }

    protected new void HideUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.HideUI(); return; }
        DOTween.Kill(gameObject);
        var orig = rt.anchoredPosition;
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(0f, 0.2f).SetEase(Ease.InQuart))
            .Join(rt.DOAnchorPos(orig + new Vector2(0, -50f), 0.2f).SetEase(Ease.InQuart))
            .OnComplete(() => { cg.interactable = false; cg.blocksRaycasts = false; });
    }

    #endregion
}
