using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;

/// <summary>
/// ������ʾ�� - ������ʾ����������������ʾ��Ϣ
/// </summary>
public partial class FloatingBoxTip : UIFormBase
{
    private RectTransform m_RectTransform;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        m_RectTransform = GetComponent<RectTransform>();
        
        Log.Info($"FloatingBoxTip OnInit: GameObject={gameObject.name}, RectTransform={m_RectTransform != null}");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Log.Info($"FloatingBoxTip OnOpen: GameObject active={gameObject.activeSelf}, position={transform.position}");
        PlayOpenAnimation();
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
        UIAnimationHelper.PopOut(m_RectTransform, GetComponent<CanvasGroup>(), 0.15f)
            .OnComplete(() => GF.UI.Close(this.UIForm));
    }

    private void PlayOpenAnimation()
    {
        DOTween.Kill(gameObject);
        Interactable = false;
        UIAnimationHelper.PopIn(m_RectTransform, GetComponent<CanvasGroup>(), 0.2f)
            .OnComplete(() => Interactable = true);
    }

    /// <summary>
    /// ������ʾ������
    /// </summary>
    /// <param name="text">��ʾ���ı�</param>
    public void SetData(string text)
    {
        Log.Info($"FloatingBoxTip SetData: text={text}, varText={varText != null}");
        
        if (varText != null)
        {
            varText.text = text;
            Log.Info($"varText.text ������: {varText.text}");
        }
        else
        {
            Log.Error("varText Ϊ null������ Unity Inspector ���Ƿ���ȷ��ֵ");
        }
    }

    /// <summary>
    /// ������ʾ��λ�ã���Ļ���꣩
    /// </summary>
    /// <param name="screenPosition">��Ļ����λ��</param>
    public void SetPosition(Vector2 screenPosition)
    {
        Log.Info($"FloatingBoxTip SetPosition: screenPosition={screenPosition}");
        
        if (m_RectTransform != null)
        {
            // ת����Ļ���굽UI����
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_RectTransform.parent as RectTransform,
                screenPosition,
                GF.UICamera,
                out Vector2 localPoint
            );

            m_RectTransform.anchoredPosition = localPoint;
            Log.Info($"anchoredPosition ������: {m_RectTransform.anchoredPosition}");
        }
        else
        {
            Log.Error("m_RectTransform Ϊ null��");
        }
    }

    /// <summary>
    /// ������ʾ��λ�ã������Ŀ��RectTransform��
    /// </summary>
    /// <param name="targetRect">Ŀ��RectTransform</param>
    /// <param name="offset">ƫ����</param>
    public void SetPositionRelativeTo(RectTransform targetRect, Vector2 offset)
    {
        Log.Info($"FloatingBoxTip SetPositionRelativeTo: targetRect={targetRect != null}, offset={offset}");
        
        if (m_RectTransform != null && targetRect != null)
        {
            // ��ȡĿ�����Ļ����
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(GF.UICamera, corners[1]); // ���Ͻ�

            Log.Info($"Ŀ����Ļ����: {screenPos}");

            // Ӧ��ƫ��
            screenPos += offset;

            SetPosition(screenPos);
        }
        else
        {
            Log.Error($"������Ч: m_RectTransform={m_RectTransform != null}, targetRect={targetRect != null}");
        }
    }
}