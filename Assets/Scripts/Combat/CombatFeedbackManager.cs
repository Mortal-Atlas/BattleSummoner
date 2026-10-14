using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// CombatFeedbackManager creates high-visibility, floating combat typography in 3D world space:
    /// - Miss, Glancing, Normal, and Critical strikes
    /// - Color-coded elemental damage (Fire, Ice, Electric, Poison, Acid, Void)
    /// - Cross-elemental combo banners (SHATTERED, COMBUSTION, MELT, SUPERCONDUCT, NIGHTMARE REAP)
    /// </summary>
    public class CombatFeedbackManager : MonoBehaviour
    {
        private static CombatFeedbackManager _instance;
        public static CombatFeedbackManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<CombatFeedbackManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var go = new GameObject("CombatFeedbackManager");
                        _instance = go.AddComponent<CombatFeedbackManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private readonly Queue<GameObject> textPool = new Queue<GameObject>();
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        public void SpawnDamagePopup(Vector3 worldPos, DamageResult result)
        {
            if (result == null) return;

            string formattedText;
            switch (result.outcome)
            {
                case HitOutcome.Miss:
                    formattedText = "<color=#999999><size=115%>MISS</size></color>";
                    break;
                case HitOutcome.Glancing:
                    formattedText = $"<color=#AAAAAA>GLANCING ({result.finalDamage:F0})</color>";
                    break;
                case HitOutcome.Critical:
                    formattedText = $"<color=#FFD700><size=130%><b>[CRIT! {result.finalDamage:F0}]</b></size></color>";
                    break;
                default:
                    formattedText = GetDamageTypeColorText(result.damageType, result.finalDamage);
                    break;
            }

            SpawnFloatingText(worldPos + Vector3.up * 0.4f, formattedText);

            // If a cross-elemental combo was triggered, spawn a secondary celebratory combo banner!
            if (result.comboTriggered != ElementalComboType.None)
            {
                string comboBanner = GetComboBannerText(result.comboTriggered, result.comboBonusDamage);
                SpawnFloatingText(worldPos + Vector3.up * 1.0f, comboBanner, 1.4f);
            }
        }

        public void SpawnStatusPopup(Vector3 worldPos, StatusEffectType status)
        {
            string statusText = status switch
            {
                StatusEffectType.Burn => "<color=#FF5511><b>[BURNED!]</b></color>",
                StatusEffectType.Freeze => "<color=#00E0FF><size=130%><b>[FROZEN SOLID!]</b></size></color>",
                StatusEffectType.Chilled => "<color=#80D0FF><b>[CHILLED]</b></color>",
                StatusEffectType.Shocked => "<color=#FFEE22><b>[SHOCKED!]</b></color>",
                StatusEffectType.Poisoned => "<color=#44EE44><b>[POISONED!]</b></color>",
                StatusEffectType.AcidCorroded => "<color=#AAFF00><b>[CORRODED -35% DEF]</b></color>",
                StatusEffectType.Slowed => "<color=#90B0FF><b>[SLOWED]</b></color>",
                StatusEffectType.Sleep => "<color=#D0A0FF><size=130%><b>[ASLEEP (Zzz)]</b></size></color>",
                StatusEffectType.Paralyzed => "<color=#FFCC00><size=130%><b>[PARALYZED!]</b></size></color>",
                _ => ""
            };

            if (!string.IsNullOrEmpty(statusText))
            {
                SpawnFloatingText(worldPos + Vector3.up * 0.8f, statusText, 1.1f);
            }
        }

        public void SpawnFloatingText(Vector3 worldPos, string content, float scale = 1.0f)
        {
            GameObject popupObj;
            if (textPool.Count > 0)
            {
                popupObj = textPool.Dequeue();
                if (popupObj == null) popupObj = CreateTextPopupObject();
            }
            else
            {
                popupObj = CreateTextPopupObject();
            }

            popupObj.transform.position = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.2f), Random.Range(-0.25f, 0.25f));
            popupObj.transform.localScale = Vector3.one * (0.012f * scale);
            popupObj.SetActive(true);

            var animator = popupObj.GetComponent<FloatingTextAnimator>();
            if (animator != null)
            {
                animator.Initialize(content, mainCamera, () => {
                    popupObj.SetActive(false);
                    textPool.Enqueue(popupObj);
                });
            }
        }

        private GameObject CreateTextPopupObject()
        {
            GameObject go = new GameObject("FloatingCombatText");
            go.transform.SetParent(transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 60f);
            go.transform.localScale = Vector3.one * 0.012f;

            var textObj = new GameObject("TextTMP");
            textObj.transform.SetParent(go.transform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;

            var anim = go.AddComponent<FloatingTextAnimator>();
            anim.textMesh = tmp;

            return go;
        }

        private string GetDamageTypeColorText(DamageType type, float damage)
        {
            return type switch
            {
                DamageType.PhysicalSlash => $"<color=#FFFFFF>{damage:F0}</color>",
                DamageType.PhysicalBlunt => $"<color=#E0E0E0>{damage:F0} [BLUNT]</color>",
                DamageType.PhysicalPierce => $"<color=#E0E8FF>{damage:F0} [PIERCE]</color>",
                DamageType.Fire => $"<color=#FF5511>{damage:F0} [FIRE]</color>",
                DamageType.Ice => $"<color=#44DDFF>{damage:F0} [FROST]</color>",
                DamageType.Electric => $"<color=#FFEE22>{damage:F0} [BOLT]</color>",
                DamageType.Poison => $"<color=#44EE44>{damage:F0} [POISON]</color>",
                DamageType.Acid => $"<color=#AAFF00>{damage:F0} [ACID]</color>",
                DamageType.ArcaneVoid => $"<color=#CC66FF>{damage:F0} [VOID]</color>",
                DamageType.RadiantHoly => $"<color=#FFE066>{damage:F0} [HOLY]</color>",
                _ => $"<color=#FFFFFF>{damage:F0}</color>"
            };
        }

        private string GetComboBannerText(ElementalComboType combo, float bonusDmg)
        {
            return combo switch
            {
                ElementalComboType.Shattered =>
                    $"<color=#00FFFF><size=130%><b>[SHATTERED! +{bonusDmg:F0} SHRAPNEL]</b></size></color>",
                ElementalComboType.Combustion =>
                    $"<color=#FF6600><size=130%><b>[COMBUSTION DETONATION! +{bonusDmg:F0}]</b></size></color>",
                ElementalComboType.MeltThermalShock =>
                    $"<color=#FFAA00><size=130%><b>[THERMAL SHOCK MELT! 2x BURST]</b></size></color>",
                ElementalComboType.Superconduct =>
                    $"<color=#88EEFF><size=125%><b>[SUPERCONDUCT DISCHARGE!]</b></size></color>",
                ElementalComboType.NightmareReap =>
                    $"<color=#FF0055><size=135%><b>[NIGHTMARE REAP CRIT! 2.5x]</b></size></color>",
                _ => ""
            };
        }
    }

    /// <summary>
    /// FloatingTextAnimator controls upward floating arc, camera billboarding, and smooth alpha fade.
    /// </summary>
    public class FloatingTextAnimator : MonoBehaviour
    {
        public TextMeshProUGUI textMesh;
        private Camera targetCamera;
        private System.Action onComplete;
        private float elapsed = 0f;
        private const float LIFETIME = 0.90f;
        private Vector3 startPos;
        private Vector3 driftVelocity;

        public void Initialize(string content, Camera cam, System.Action completeCallback)
        {
            if (textMesh != null) textMesh.text = content;
            targetCamera = cam;
            onComplete = completeCallback;
            elapsed = 0f;
            startPos = transform.position;
            driftVelocity = new Vector3(Random.Range(-0.4f, 0.4f), 1.8f, Random.Range(-0.4f, 0.4f));

            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = 1f;
                textMesh.color = c;
            }
        }

        private void Update()
        {
            if (targetCamera != null)
            {
                // Billboard: Face camera directly
                transform.rotation = targetCamera.transform.rotation;
            }

            elapsed += Time.deltaTime;
            float progress = elapsed / LIFETIME;

            // Float upwards with deceleration
            float currentSpeed = Mathf.Lerp(1.8f, 0.4f, progress);
            transform.position += new Vector3(driftVelocity.x * Time.deltaTime, currentSpeed * Time.deltaTime, driftVelocity.z * Time.deltaTime);

            // Fade out
            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = Mathf.Clamp01(1f - progress * progress);
                textMesh.color = c;
            }

            if (elapsed >= LIFETIME)
            {
                onComplete?.Invoke();
            }
        }
    }
}
