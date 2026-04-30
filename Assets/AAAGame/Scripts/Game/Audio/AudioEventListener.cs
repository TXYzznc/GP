using GameFramework;
using System;
using UnityEngine;

namespace AAAGame.Audio
{
    /// <summary>
    /// 音效系统的事件监听器（各流程/系统直接调用对应方法）
    /// </summary>
    public class AudioEventListener : MonoBehaviour
    {
        public static AudioEventListener Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayBGMForProcedure(string procedureName)
        {
            int bgmId = procedureName switch
            {
                "StartGameProcedure" => AudioClipIds.BGM_StartGame,
                "GameProcedure" => AudioClipIds.BGM_BaseRoom,
                "TutorialProcedure" => AudioClipIds.BGM_TutorialScene,
                _ => -1
            };

            if (bgmId > 0)
            {
                AudioManager.Instance?.PlayBGM(bgmId, fadeInTime: 1f);
                DebugEx.Log("AudioEventListener", $"✓ 播放流程 BGM: {procedureName} (ID={bgmId})");
            }
        }

        public void OnCombatStart()
        {
            AudioManager.Instance?.PlayBGM(AudioClipIds.BGM_CombatNormal);
            AudioManager.Instance?.PlaySFX(AudioClipIds.SFX_CombatStart);
        }

        public void OnCombatVictory()
        {
            AudioManager.Instance?.PlaySFX(AudioClipIds.SFX_CombatVictory);
        }

        public void OnCombatDefeat()
        {
            AudioManager.Instance?.PlaySFX(AudioClipIds.SFX_CombatDefeat);
        }
    }
}
