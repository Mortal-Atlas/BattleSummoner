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

        private static int _offsetCounter = 0;

        /// <summary>
        /// Generates a staggered golden-ratio fanning offset so rapid damage hits and heal ticks
        /// cascade beautifully around the target without overlapping.
        /// </summary>
        public static Vector3 GetStaggeredOffset()
        {
            _offsetCounter++;
            float angle = (_offsetCounter * 137.5f) * Mathf.Deg2Rad; // Golden angle distribution
            float radius = 0.28f + (_offsetCounter % 3) * 0.12f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = (_offsetCounter % 4) * 0.12f;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Spawns white floating text when an enemy takes damage.
        /// If it crits, the number is red and says "CRIT!".
        /// </summary>
        public void SpawnEnemyDamage(Vector3 worldPos, float damage, bool isCrit = false)
        {
            if (damage <= 0f) return;

            string formattedText;
            float scale;
            if (isCrit)
            {
                formattedText = $"<color=#FF2233><size=135%><b>CRIT! {damage:F0}</b></size></color>";
                scale = 1.35f;
            }
            else
            {
                formattedText = $"<color=#FFFFFF><b>{damage:F0}</b></color>";
                scale = 1.0f;
            }

            Vector3 spawnPos = worldPos + Vector3.up * 0.4f + GetStaggeredOffset();
            SpawnFloatingText(spawnPos, formattedText, scale);
        }

        /// <summary>
        /// Spawns ticking green numbers for how much a person heals for.
        /// Staggered with an offset so multiple heal ticks cascade cleanly.
        /// </summary>
        public void SpawnHealing(Vector3 worldPos, float healAmount)
        {
            if (healAmount <= 0f) return;

            string formattedText = $"<color=#22FF66><size=120%><b>+{healAmount:F0}</b></size></color>";
            Vector3 spawnPos = worldPos + Vector3.up * 0.6f + GetStaggeredOffset();
            SpawnFloatingText(spawnPos, formattedText, 1.15f);
        }

        public void SpawnDamagePopup(Vector3 worldPos, DamageResult result)
        {
            if (result == null) return;

            string formattedText;
            float scale = 1.0f;
            switch (result.outcome)
            {
                case HitOutcome.Miss:
                    formattedText = "<color=#999999><size=115%>MISS</size></color>";
                    scale = 0.9f;
                    break;
                case HitOutcome.Glancing:
                    formattedText = $"<color=#AAAAAA>GLANCING ({result.finalDamage:F0})</color>";
                    scale = 0.85f;
                    break;
                case HitOutcome.Critical:
                    formattedText = $"<color=#FF2233><size=135%><b>CRIT! {result.finalDamage:F0}</b></size></color>";
                    scale = 1.35f;
                    BattleSummoner.VFX.AnimeImpactFrameController.Trigger(BattleSummoner.VFX.AnimeImpactStyle.InvertMangaSketch, 0.07f);
                    BattleSummoner.VFX.EasyZoomBlurController.TriggerBlur(0.45f, 0.18f);
                    BattleSummoner.Combat.StylizedAnimeStarVFX.SpawnStarBurst(worldPos + Vector3.up * 0.5f, new Color(1f, 0.88f, 0.25f), 1.3f);
                    BattleSummoner.VFX.AnimeFlipbookVFXManager.PlayFlipbook(BattleSummoner.VFX.FlipbookEffectType.ImpactHitSparkle, worldPos + Vector3.up * 0.5f, null, 1.4f);
                    break;
                default:
                    // Default enemy damage is clean white floating text with hand-drawn anime slash
                    formattedText = $"<color=#FFFFFF><b>{result.finalDamage:F0}</b></color>";
                    scale = 1.0f;
                    BattleSummoner.VFX.AnimeFlipbookVFXManager.PlayFlipbook(BattleSummoner.VFX.FlipbookEffectType.AnimeSlashCrescent, worldPos + Vector3.up * 0.5f, null, 1.1f);
                    break;
            }

            Vector3 spawnPos = worldPos + Vector3.up * 0.4f + GetStaggeredOffset();
            SpawnFloatingText(spawnPos, formattedText, scale);

            // If a cross-elemental combo was triggered, spawn a secondary celebratory combo banner!
            if (result.comboTriggered != ElementalComboType.None)
            {
                string comboBanner = GetComboBannerText(result.comboTriggered, result.comboBonusDamage);
                SpawnFloatingText(worldPos + Vector3.up * 1.2f, comboBanner, 1.4f);
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
                SpawnFloatingText(worldPos + Vector3.up * 0.8f + GetStaggeredOffset(), statusText, 1.1f);
            }
        }

        /// <summary>
        /// Spawns floating golden/emerald loot drop feedback text in 3D world space.
        /// </summary>
        public void SpawnLootFeedback(Vector3 worldPos, string lootText, Color? color = null)
        {
            Color c = color ?? new Color(1.0f, 0.85f, 0.25f);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            string formatted = $"<color=#{hex}><size=125%><b>✦ {lootText}</b></size></color>";
            SpawnFloatingText(worldPos + Vector3.up * 1.0f + GetStaggeredOffset(), formatted, 1.25f);
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

            popupObj.transform.position = worldPos;
            popupObj.SetActive(true);

            var animator = popupObj.GetComponent<FloatingTextAnimator>();
            if (animator != null)
            {
                animator.Initialize(content, mainCamera, scale, () => {
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
                DamageType.PhysicalBlunt => $"<color=#FFFFFF>{damage:F0}</color>",
                DamageType.PhysicalPierce => $"<color=#FFFFFF>{damage:F0}</color>",
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
    /// FloatingTextAnimator controls upward floating arc, camera billboarding,
    /// fast initial drift that slows down (ease-out deceleration), and smooth alpha fade.
    /// </summary>
    public class FloatingTextAnimator : MonoBehaviour
    {
        public TextMeshProUGUI textMesh;
        private Camera targetCamera;
        private System.Action onComplete;
        private float elapsed = 0f;
        private const float LIFETIME = 1.15f;
        private Vector3 driftVelocity;
        private float baseScale = 1.0f;

        public void Initialize(string content, Camera cam, float scaleMultiplier, System.Action completeCallback)
        {
            if (textMesh != null) textMesh.text = content;
            targetCamera = cam;
            onComplete = completeCallback;
            elapsed = 0f;
            baseScale = scaleMultiplier;

            // Fast upward drift velocity + subtle horizontal drift
            float horizontalAngle = Random.Range(0f, Mathf.PI * 2f);
            float hSpeed = Random.Range(0.2f, 0.45f);
            driftVelocity = new Vector3(Mathf.Cos(horizontalAngle) * hSpeed, 3.5f, Mathf.Sin(horizontalAngle) * hSpeed);

            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = 1f;
                textMesh.color = c;
            }

            // Punchy scale pop on spawn
            transform.localScale = Vector3.one * (0.012f * baseScale * 1.35f);
        }

        private void Update()
        {
            if (targetCamera != null)
            {
                // Billboard: Face camera directly
                transform.rotation = targetCamera.transform.rotation;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / LIFETIME);

            // Fast drift initially, then slows down as it moves up (Ease-Out Deceleration)
            float speedDecay = Mathf.Pow(1.0f - progress, 2.0f);
            float currentUpwardSpeed = Mathf.Max(0.25f, driftVelocity.y * speedDecay);
            float currentHorizSpeed = speedDecay;

            transform.position += new Vector3(
                driftVelocity.x * currentHorizSpeed * Time.deltaTime,
                currentUpwardSpeed * Time.deltaTime,
                driftVelocity.z * currentHorizSpeed * Time.deltaTime
            );

            // Scale punch: quick punch-in settling within first 0.14 seconds
            if (elapsed < 0.14f)
            {
                float punchFactor = Mathf.Lerp(1.35f, 1.0f, elapsed / 0.14f);
                transform.localScale = Vector3.one * (0.012f * baseScale * punchFactor);
            }
            else
            {
                transform.localScale = Vector3.one * (0.012f * baseScale);
            }

            // Alpha fade: stays fully opaque during fast drift (progress < 0.35), then smoothly fades out
            if (textMesh != null)
            {
                Color c = textMesh.color;
                if (progress < 0.35f)
                {
                    c.a = 1.0f;
                }
                else
                {
                    float fadeT = (progress - 0.35f) / 0.65f;
                    c.a = Mathf.Clamp01(1.0f - (fadeT * fadeT)); // smooth quadratic fade
                }
                textMesh.color = c;
            }

            if (elapsed >= LIFETIME)
            {
                onComplete?.Invoke();
            }
        }
    }
}
