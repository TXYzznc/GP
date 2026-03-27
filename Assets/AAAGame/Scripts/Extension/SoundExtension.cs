using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using GameExtension;

public static class SoundExtension
{
    private static Dictionary<string, float> lastPlayEffectTags = new Dictionary<string, float>();
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="soundCom"></param>
    /// <param name="soundId">背景音乐资源ID</param>
    /// <returns></returns>
    public static int PlayBGM(this SoundComponent soundCom, int soundId)
    {
        return soundCom.PlaySound(soundId, Const.SoundGroup.Music.ToString(), Vector3.zero, true);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="soundCom"></param>
    /// <param name="soundId">音效资源ID</param>
    /// <returns></returns>
    public static int PlaySound(this SoundComponent soundCom, int soundId, string group, Vector3 worldPos, bool isLoop = false)
    {
        string assetName = ResourceExtension.GetResourceConfigPath(soundId);
        if (string.IsNullOrEmpty(assetName))
        {
            Log.Warning("SoundExtension: 播放声音失败，无效的资源ID: {0}", soundId);
            return 0;
        }

        //TODO 临时资源存在判定
        if (GFBuiltin.Resource.HasAsset(assetName) == GameFramework.Resource.HasAssetResult.NotExist) return 0;
        var parms = ReferencePool.Acquire<GameFramework.Sound.PlaySoundParams>();
        parms.Clear();
        parms.Loop = isLoop;
        return soundCom.PlaySound(assetName, group, 0, parms, worldPos);
    }
    public static int PlayEffect(this SoundComponent soundCom, int soundId, bool isLoop = false)
    {
        return soundCom.PlaySound(soundId, Const.SoundGroup.Sound.ToString(), Vector3.zero, isLoop);
    }
    public static void PlayEffect(this SoundComponent soundCom, int soundId, float interval)
    {
        // 使用 ID 作为 Key
        string key = soundId.ToString();
        bool hasKey = lastPlayEffectTags.ContainsKey(key);
        if (hasKey && Time.time - lastPlayEffectTags[key] < interval)
        {
            return;
        }
        soundCom.PlaySound(soundId, Const.SoundGroup.Sound.ToString(), Vector3.zero, false);
        if (hasKey) lastPlayEffectTags[key] = Time.time;
        else lastPlayEffectTags.Add(key, Time.time);
    }

    public static void PlayVibrate(this SoundComponent soundCom, long time = Const.DefaultVibrateDuration)
    {
        if (soundCom.GetSoundGroup(Const.SoundGroup.Vibrate.ToString()).Mute)
        {
            return;
        }
#if UNITY_ANDROID || UNITY_IOS
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass act = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityAct = act.GetStatic<AndroidJavaObject>("currentActivity");
            var vibr = unityAct.Call<AndroidJavaObject>("getSystemService", "vibrator");
            vibr.Call("vibrate", time);
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            Handheld.Vibrate();
        }
#endif
    }
}
