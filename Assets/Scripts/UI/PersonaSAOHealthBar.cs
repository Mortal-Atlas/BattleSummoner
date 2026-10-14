using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.UI
{
    /// <summary>
    /// PersonaSAOHealthBar implements a sleek, high-visibility health & mana bar gauge
    /// blending the aesthetics of Persona 5 and Sword Art Online (SAO).
    /// Features:
    /// - Angled geometric gunmetal chassis with neon status accents.
    /// - Segmented tick marks along emerald health and cyan mana tracks.
    /// - Smooth interpolation and ghost damage trail animation.
    /// - Critical HP warning pulse (<25% health).
    /// - Diamond Level indicator badge.
    /// </summary>
    public class PersonaSAOHealthBar : MonoBehaviour
    {
        [Header("UI Bar Components")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image healthGhostFill;
        [SerializeField] private Image manaBarFill;
        [SerializeField] private Image chassisFrame;
        [SerializeField] private Image levelDiamondBadge;

        [Header("Numeric Telemetry")]
        [SerializeField] private TextMeshProUGUI hpValueTMP;
        [SerializeField] private TextMeshProUGUI mpValueTMP;
        [SerializeField] private TextMeshProUGUI levelNumberTMP;

        [Header("Gauges Tuning")]
        [SerializeField] private float fillLerpSpeed = 8.0f;
        [SerializeField] private float ghostLagSpeed = 3.0f;
        [SerializeField] private Color normalHpColor = new Color(0.12f, 0.90f, 0.45f);
        [SerializeField] private Color criticalHpColor = new Color(1.0f, 0.22f, 0.28f);
        [SerializeField] private Color manaColor = new Color(0.15f, 0.78f, 1.0f);

        private float targetHpRatio = 1.0f;
        private float targetMpRatio = 1.0f;
        private float currentHpRatio = 1.0f;
        private float currentGhostRatio = 1.0f;
        private float currentMpRatio = 1.0f;

        private float currentHp = 1000f;
        private float maxHp = 1000f;
        private float currentMp = 600f;
        private float maxMp = 600f;
        private int currentLevel = 50;

        private PlayerProgression playerProgression;

        private void Start()
        {
            ResolveProgression();
            InitSprites();
        }

        private void OnEnable()
        {
            ResolveProgression();
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged += HandleHealthChanged;
                playerProgression.OnManaChanged += HandleManaChanged;
                playerProgression.OnLevelUpEvent += HandleLevelUp;
            }
        }

        private void OnDisable()
        {
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged -= HandleHealthChanged;
                playerProgression.OnManaChanged -= HandleManaChanged;
                playerProgression.OnLevelUpEvent -= HandleLevelUp;
            }
        }

        private void ResolveProgression()
        {
            if (playerProgression == null)
            {
                playerProgression = FindAnyObjectByType<PlayerProgression>();
                if (playerProgression != null)
                {
                    currentHp = playerProgression.currentHp;
                    maxHp = Mathf.Max(1f, playerProgression.maxHp);
                    currentMp = playerProgression.currentMp;
                    maxMp = Mathf.Max(1f, playerProgression.maxMp);
                    currentLevel = playerProgression.currentLevel;

                    targetHpRatio = Mathf.Clamp01(currentHp / maxHp);
                    targetMpRatio = Mathf.Clamp01(currentMp / maxMp);
                    currentHpRatio = targetHpRatio;
                    currentGhostRatio = targetHpRatio;
                    currentMpRatio = targetMpRatio;
                }
            }
        }

        private void InitSprites()
        {
            if (healthBarFill != null && healthBarFill.sprite == null)
            {
                var hpSprite = Resources.Load<Sprite>("UI/Bars/UI_HealthBar_Emerald") 
                    ?? Resources.Load<Sprite>("UI_HealthBar_Emerald");
                if (hpSprite != null) healthBarFill.sprite = hpSprite;
            }

            if (manaBarFill != null && manaBarFill.sprite == null)
            {
                var mpSprite = Resources.Load<Sprite>("UI/Bars/UI_ManaBar_Cyan")
                    ?? Resources.Load<Sprite>("UI_ManaBar_Cyan");
                if (mpSprite != null) manaBarFill.sprite = mpSprite;
            }
        }

        private void Update()
        {
            // Smoothly interpolate fills
            currentHpRatio = Mathf.Lerp(currentHpRatio, targetHpRatio, Time.deltaTime * fillLerpSpeed);
            currentGhostRatio = Mathf.Lerp(currentGhostRatio, targetHpRatio, Time.deltaTime * ghostLagSpeed);
            currentMpRatio = Mathf.Lerp(currentMpRatio, targetMpRatio, Time.deltaTime * fillLerpSpeed);

            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = currentHpRatio;
                // Critical pulse
                if (targetHpRatio <= 0.25f)
                {
                    float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                    healthBarFill.color = Color.Lerp(normalHpColor, criticalHpColor, pulse);
                }
                else
                {
                    healthBarFill.color = normalHpColor;
                }
            }

            if (healthGhostFill != null)
            {
                healthGhostFill.fillAmount = currentGhostRatio;
            }

            if (manaBarFill != null)
            {
                manaBarFill.fillAmount = currentMpRatio;
                manaBarFill.color = manaColor;
            }

            // Update numeric TMP readouts
            if (hpValueTMP != null)
            {
                hpValueTMP.text = $"HP {Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            }

            if (mpValueTMP != null)
            {
                mpValueTMP.text = $"MP {Mathf.CeilToInt(currentMp)}/{Mathf.CeilToInt(maxMp)}";
            }

            if (levelNumberTMP != null)
            {
                levelNumberTMP.text = currentLevel.ToString();
            }
        }

        public void SetHealth(float current, float max)
        {
            currentHp = current;
            maxHp = Mathf.Max(1f, max);
            targetHpRatio = Mathf.Clamp01(currentHp / maxHp);
        }

        public void SetMana(float current, float max)
        {
            currentMp = current;
            maxMp = Mathf.Max(1f, max);
            targetMpRatio = Mathf.Clamp01(currentMp / maxMp);
        }

        public void SetLevel(int level)
        {
            currentLevel = level;
            if (levelNumberTMP != null) levelNumberTMP.text = level.ToString();
        }

        private void HandleHealthChanged(float curr, float max) => SetHealth(curr, max);
        private void HandleManaChanged(float curr, float max) => SetMana(curr, max);
        private void HandleLevelUp(int level) => SetLevel(level);
    }
}
