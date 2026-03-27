using System;
using UnityEngine;

public class UIAnimationEventDispatcher : MonoBehaviour
{
    public event Action<string> OnAnimationEventTriggered;

    public event Action<string> OnAnimationComplete;

    public void TriggerAnimationEvent(string eventName)
    {
        DebugEx.LogModule("UIAnimEvt", $"TriggerAnimationEvent name={eventName} go={gameObject.name} t={Time.time:F3} f={Time.frameCount}");
        OnAnimationEventTriggered?.Invoke(eventName);
    }

    public void TriggerAnimationComplete(string animName)
    {
        DebugEx.LogModule("UIAnimEvt", $"TriggerAnimationComplete name={animName} go={gameObject.name} t={Time.time:F3} f={Time.frameCount}");
        OnAnimationComplete?.Invoke(animName);
    }
}
