using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class SettingDialog : UIFormBase
{
    private const string KEY_SENSITIVITY = "mouse_sensitivity_x";
    private const float SENSITIVITY_MIN = 0.5f;
    private const float SENSITIVITY_MAX = 5.0f;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varSensitivitySlider.minValue = SENSITIVITY_MIN;
        varSensitivitySlider.maxValue = SENSITIVITY_MAX;
        varSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        float saved = GF.Setting.GetFloat(KEY_SENSITIVITY, 2.0f);
        varSensitivitySlider.SetValueWithoutNotify(saved);
        RefreshSensitivityText(saved);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Setting.Save();
        base.OnClose(isShutdown, userData);
    }

    private void OnSensitivityChanged(float value)
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.MouseSensitivityX = value;
            PlayerInputManager.Instance.MouseSensitivityY = value * 0.5f;
        }
        GF.Setting.SetFloat(KEY_SENSITIVITY, value);
        RefreshSensitivityText(value);
    }

    private void RefreshSensitivityText(float value)
    {
        if (varSensitivityValueText != null)
            varSensitivityValueText.text = value.ToString("F1");
    }

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf == varButton_Close)
        {
            GF.UI.CloseUIForm(Id);
        }
        else if (btSelf == varBtnBackToMenu)
        {
            SaveIfOutOfGame();
            GF.UI.CloseUIForm(Id);
            GameFlowManager.BackToMenu();
        }
        else if (btSelf == varBtnQuit)
        {
            SaveIfOutOfGame();
            GameFlowManager.QuitGame();
        }
    }

    private void SaveIfOutOfGame()
    {
        if (GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameStateType.OutOfGame)
        {
            PlayerAccountDataManager.Instance?.SaveCurrentSave();
        }
    }
}
