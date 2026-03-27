using GameFramework;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;

public class GF : GFBuiltin
{
    public static DataModelComponent DataModel { get; private set; }
    //���DataNode, ʹ��Jobs�ĸ����ܱ����洢��,���ں������ݴ洢
    public static VariablePoolComponent VariablePool { get; private set; }
    //public static ADComponent AD { get; private set; }

    public static StaticUIComponent StaticUI { get; private set; }

    private void Start()
    {
        DataModel = GameEntry.GetComponent<DataModelComponent>();
        //AD = GameEntry.GetComponent<ADComponent>();
        StaticUI = GameEntry.GetComponent<StaticUIComponent>();
        VariablePool = GameEntry.GetComponent<VariablePoolComponent>();
    }

    private void OnApplicationQuit()
    {
        OnExitGame();
    }
    private void OnApplicationPause(bool pause)
    {
        //Log.Info("OnApplicationPause:{0}", pause);
        if (Application.isMobilePlatform && pause)
        {
            OnExitGame();
        }
    }
    public Vector2 GetCanvasSize()
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        return rect.sizeDelta;
    }
    public Vector2 World2ScreenPoint(Camera cam, Vector3 worldPoint)
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        Vector2 sPoint = cam.WorldToViewportPoint(worldPoint) * rect.sizeDelta;
        return sPoint - rect.sizeDelta * 0.5f;
    }
    private void OnExitGame()
    {
        GF.Event.FireNow(this, GFEventArgs.Create(GFEventType.ApplicationQuit));
        var exit_time = DateTime.UtcNow.ToString();
        GF.Setting.SetString(ConstBuiltin.Setting.QuitAppTime, exit_time);
        GF.Setting.Save();
        UnityGameFramework.Runtime.Log.Info("Application Quit:{0}", exit_time);
    }
}
