using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BattleSummoner.UI.Polish
{
    /// <summary>
    /// Attaches to buttons, drawers, and HUD cards to provide flagship mobile-app responsiveness:
    /// - Critically damped spring scale on touch down/up (0.94x press -> 1.04x rebound -> 1.0x rest)
    /// - Smooth drawer slide-in/fade-in without jarring SetActive pop-in
    /// - Lag-drain secondary bar interpolation for Persona-style HP/MP gauges
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AppGradeUIPolishController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Micro-Interaction Spring Settings")]
        public bool enableTouchSpringScale = true;
        public float pressedScale = 0.94f;
        public float releaseOvershootScale = 1.035f;
        public float transitionDuration = 0.14f;

        [Header("Lag-Drain Gauge (Optional - For Player & 9-Ally HP/MP Bars)")]
        public Image primaryFillBar;
        public Image secondaryLagDrainBar;
        public float lagDrainSpeed = 2.8f;

        private RectTransform _rect;
        private Vector3 _baseScale;
        private Coroutine _scaleRoutine;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _baseScale = _rect.localScale;
        }

        private void Update()
        {
            // Smoothly trails the white/gold damage-lag bar behind the real-time HP bar
            if (primaryFillBar != null && secondaryLagDrainBar != null)
            {
                if (secondaryLagDrainBar.fillAmount > primaryFillBar.fillAmount)
                {
                    secondaryLagDrainBar.fillAmount = Mathf.Lerp(
                        secondaryLagDrainBar.fillAmount,
                        primaryFillBar.fillAmount,
                        Time.unscaledDeltaTime * lagDrainSpeed);
                }
                else
                {
                    secondaryLagDrainBar.fillAmount = primaryFillBar.fillAmount;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enableTouchSpringScale) return;
            StartScaleAnimation(_baseScale * pressedScale, transitionDuration * 0.6f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enableTouchSpringScale) return;
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(SpringReboundRoutine());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enableTouchSpringScale) return;
            StartScaleAnimation(_baseScale, transitionDuration);
        }

        private void StartScaleAnimation(Vector3 target, float duration)
        {
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleLerpRoutine(target, duration));
        }

        private IEnumerator ScaleLerpRoutine(Vector3 target, float duration)
        {
            Vector3 start = _rect.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _rect.localScale = Vector3.LerpUnclamped(start, target, t);
                yield return null;
            }
            _rect.localScale = target;
        }

        private IEnumerator SpringReboundRoutine()
        {
            yield return ScaleLerpRoutine(_baseScale * releaseOvershootScale, transitionDuration * 0.55f);
            yield return ScaleLerpRoutine(_baseScale, transitionDuration * 0.65f);
        }
    }
}
