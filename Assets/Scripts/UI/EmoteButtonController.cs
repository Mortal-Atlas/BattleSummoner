using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BattleSummoner.Combat;
using BattleSummoner.Core;

namespace BattleSummoner.UI
{
    /// <summary>
    /// EmoteButtonController handles the interactive Emote Drama Masks button placed directly beneath the AI button.
    /// Provides one-tap instant emote cycling and an optional radial/popup selection menu for player expressions:
    /// - Royal Waltz Dance
    /// - Energetic Folk Jig
    /// - Campfire Sway
    /// - Martial Arts Kata
    /// - Archer Flourish
    /// - Victory Cheer
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class EmoteButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public static EmoteButtonController Instance { get; private set; }

        [Header("UI Linkages")]
        [SerializeField] private Button emoteButton;
        [SerializeField] private Image buttonIcon;
        [SerializeField] private RectTransform radialEmoteMenu;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = new Color(1f, 0.85f, 0.5f, 1f);
        [SerializeField] private float pressScalePunch = 0.88f;

        private int currentEmoteIndex = 0;
        private readonly string[] emoteNames = new string[]
        {
            "💃 Chicken Wing Dance",
            "🕺 Groovy Step Loop",
            "💧 Sprinkler Dance",
            "🤸 Freestyle Groove",
            "🔥 Arms Pump & Step",
            "⚡ Kick & Pump Arms",
            "🥋 Martial Arts Taunt",
            "🎉 Victorious Cheer"
        };

        private RectTransform rectTransform;
        private Vector3 baseScale;
        private Coroutine punchCoroutine;

        private void Awake()
        {
            Instance = this;
            rectTransform = GetComponent<RectTransform>();
            baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

            if (emoteButton == null) emoteButton = GetComponent<Button>();
            if (buttonIcon == null) buttonIcon = GetComponent<Image>();

            if (emoteButton != null)
            {
                emoteButton.onClick.RemoveAllListeners();
                emoteButton.onClick.AddListener(OnEmoteButtonClicked);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(AnimateScale(baseScale * pressScalePunch, 0.08f));
            if (buttonIcon != null) buttonIcon.color = pressedColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(AnimateScale(baseScale, 0.12f));
            if (buttonIcon != null) buttonIcon.color = normalColor;
        }

        public void OnEmoteButtonClicked()
        {
            TriggerCurrentEmote();
            // Advance to next emote for next quick-tap
            currentEmoteIndex = (currentEmoteIndex + 1) % emoteNames.Length;
        }

        public void TriggerCurrentEmote()
        {
            TriggerEmoteByIndex(currentEmoteIndex);
        }

        public void TriggerEmoteByIndex(int index)
        {
            currentEmoteIndex = Mathf.Clamp(index, 0, emoteNames.Length - 1);
            string chosenEmote = emoteNames[currentEmoteIndex];

            // 1. Trigger Animation on Hero
            var animCtrl = HumanoidAnimationController.Instance 
                           ?? UnityEngine.Object.FindFirstObjectByType<HumanoidAnimationController>();
            if (animCtrl != null)
            {
                switch (currentEmoteIndex)
                {
                    case 0:
                        animCtrl.PlayDanceAnimation(1);
                        break;
                    case 1:
                        animCtrl.PlayDanceAnimation(2);
                        break;
                    case 2:
                        animCtrl.PlayDanceAnimation(3);
                        break;
                    case 3:
                        animCtrl.PlayDanceAnimation(4);
                        break;
                    case 4:
                        animCtrl.PlayDanceAnimation(5);
                        break;
                    case 5:
                        animCtrl.PlayDanceAnimation(6);
                        break;
                    case 6:
                        animCtrl.PlayMartialArtsStrike(1);
                        break;
                    case 7:
                        animCtrl.PlayVictoryAnimation();
                        break;
                }
            }

            // 2. Broadcast banner notification
            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast($"🎭 [EMOTE] Aurelia expresses: {chosenEmote}!", false);
            }

            Debug.Log($"🎭 [EMOTE TRIGGERED] Index: {currentEmoteIndex} - {chosenEmote}");
        }

        private IEnumerator AnimateScale(Vector3 targetScale, float duration)
        {
            Vector3 start = rectTransform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rectTransform.localScale = Vector3.Lerp(start, targetScale, t);
                yield return null;
            }
            rectTransform.localScale = targetScale;
        }
    }
}
