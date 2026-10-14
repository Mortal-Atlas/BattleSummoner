using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class SpireAchievement
{
    public string achievementId;
    public string title;
    public string broadcastMessageTemplate;
    public bool isSecret;
    public bool isPublicBroadcast;
}

public class SystemBroadcastManager : MonoBehaviour
{
    public static SystemBroadcastManager Instance { get; private set; }

    [Header("UI Ticker Elements")]
    [SerializeField] private GameObject broadcastBannerContainer;
    [SerializeField] private TextMeshProUGUI broadcastText;
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private float bannerDisplayDuration = 4.5f;

    [Header("Database")]
    [SerializeField] private List<SpireAchievement> achievementRegistry = new List<SpireAchievement>();

    private readonly HashSet<string> unlockedAchievements = new HashSet<string>();
    private readonly Queue<string> broadcastQueue = new Queue<string>();
    private bool isDisplayingBroadcast = false;

    public static event Action<string> OnPublicBroadcastDispatched;
    public static event Action<string> OnSecretAchievementLocallyUnlocked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeDefaultAchievements();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (bannerCanvasGroup == null && broadcastBannerContainer != null)
        {
            bannerCanvasGroup = broadcastBannerContainer.GetComponent<CanvasGroup>();
        }
    }

    private void InitializeDefaultAchievements()
    {
        achievementRegistry.Clear();

        // Public milestones (Spire-wide broadcast)
        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_boss_floor50_slain",
            title = "Abyssal Arbiter's Bane",
            broadcastMessageTemplate = "[SOVEREIGN] [SPIRE BROADCAST] {0} has vanquished the Floor 50 Sovereign Arbiter!",
            isSecret = false,
            isPublicBroadcast = true
        });

        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_ascendant_lv100",
            title = "Ascendant Zenith",
            broadcastMessageTemplate = "[ZENITH] [SPIRE BROADCAST] {0} has achieved Player Level 100 and awakened their True Core!",
            isSecret = false,
            isPublicBroadcast = true
        });

        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_pinnacle_lv50",
            title = "Mid-Spire Vanguard",
            broadcastMessageTemplate = "[VANGUARD] [SPIRE BROADCAST] {0} has broken through Floor 50 Spire Barrier at Level 50!",
            isSecret = false,
            isPublicBroadcast = true
        });

        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_relic_3x_drop",
            title = "Hands of Midas",
            broadcastMessageTemplate = "[RELIC] [SPIRE BROADCAST] {0} unearthed an Ancestral 3.0x Multiplicative Strike Relic!",
            isSecret = false,
            isPublicBroadcast = true
        });

        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_gem_prismatic_unearth",
            title = "Prismatic Core Discovered",
            broadcastMessageTemplate = "[PRISMATIC] [SPIRE MINING] {0} struck and unearthed an Ancestral Prismatic Geode from the deep crystalline abyss!",
            isSecret = false,
            isPublicBroadcast = true
        });

        // Secret achievements (Never broadcasted)
        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_secret_forbidden_collapse",
            title = "Curiosity Vaporized the Caster",
            broadcastMessageTemplate = "",
            isSecret = true,
            isPublicBroadcast = false
        });

        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_secret_anvil_suicide",
            title = "Avarice's Detonation",
            broadcastMessageTemplate = "",
            isSecret = true,
            isPublicBroadcast = false
        });

        achievementRegistry.Add(new SpireAchievement
        {
            achievementId = "ach_secret_volatile_geode_explosion",
            title = "Geode Pipebomb",
            broadcastMessageTemplate = "",
            isSecret = true,
            isPublicBroadcast = false
        });
    }

    public void TriggerAchievement(string achievementId, string playerName)
    {
        if (unlockedAchievements.Contains(achievementId)) return;

        SpireAchievement ach = achievementRegistry.Find(a => a.achievementId == achievementId);
        if (ach == null)
        {
            Debug.LogWarning($"[ACHIEVEMENTS] Achievement ID '{achievementId}' not found in registry.");
            return;
        }

        unlockedAchievements.Add(achievementId);

        if (ach.isSecret)
        {
            Debug.Log($"[SECRET UNLOCKED - PRIVATE] {ach.title} rewarded to {playerName}. Suppressed from Spire broadcast.");
            OnSecretAchievementLocallyUnlocked?.Invoke(ach.title);
            return;
        }

        if (ach.isPublicBroadcast)
        {
            string formattedMessage = string.Format(ach.broadcastMessageTemplate, playerName);
            QueueBroadcast(formattedMessage);
            OnPublicBroadcastDispatched?.Invoke(formattedMessage);
        }
    }

    public void DispatchBroadcast(string message, bool isGlobal = true)
    {
        QueueBroadcast(message);
        if (isGlobal)
        {
            OnPublicBroadcastDispatched?.Invoke(message);
        }
    }

    public void QueueBroadcast(string broadcastMessage)
    {
        broadcastQueue.Enqueue(broadcastMessage);
        if (!isDisplayingBroadcast)
        {
            StartCoroutine(ProcessBroadcastQueueRoutine());
        }
    }

    private IEnumerator ProcessBroadcastQueueRoutine()
    {
        isDisplayingBroadcast = true;

        while (broadcastQueue.Count > 0)
        {
            string currentMessage = broadcastQueue.Dequeue();
            if (broadcastBannerContainer != null && broadcastText != null)
            {
                broadcastBannerContainer.SetActive(true);
                broadcastText.text = currentMessage;

                float timer = 0f;
                while (timer < 0.35f)
                {
                    timer += Time.deltaTime;
                    if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 0.35f);
                    yield return null;
                }

                yield return new WaitForSeconds(bannerDisplayDuration);

                timer = 0f;
                while (timer < 0.35f)
                {
                    timer += Time.deltaTime;
                    if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / 0.35f);
                    yield return null;
                }

                broadcastBannerContainer.SetActive(false);
            }
            yield return new WaitForSeconds(0.2f);
        }

        isDisplayingBroadcast = false;
    }
}
