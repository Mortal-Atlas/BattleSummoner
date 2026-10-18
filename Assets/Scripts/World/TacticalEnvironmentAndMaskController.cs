using System;
using UnityEngine;

namespace BattleSummoner.World
{
    public enum FloorWeather { Clear, Rainstorm, HeavyGaleWind, AshStorm }

    /// <summary>
    /// Tactical Environment, Weather-Reactive Magic, Cursed Possession Masks, Gamble Shrines & Guild Banners.
    /// Rainstorm chains Lightning spells and dampens Fire. Gale Wind enhances Aero/Wind velocity and damage.
    /// Cursed Boss Masks grant a 15-second "Let It Drive" berserk possession mode at 1 HP.
    /// Guild Trophy Banners create defensive rallying zones for +30% party damage and mana regen during boss raids.
    /// </summary>
    public class TacticalEnvironmentAndMaskController : MonoBehaviour
    {
        public static TacticalEnvironmentAndMaskController Instance { get; private set; }

        [Header("Floor Dynamic Weather")]
        public FloorWeather activeWeather = FloorWeather.Clear;

        [Header("Cursed Mask Possession (1-HP Clutch Mode)")]
        public bool isCursedMaskPossessionActive = false;
        public float cursedMaskDurationRemaining = 0f;
        public const float CURSED_MASK_MAX_DURATION = 15f;

        [Header("Guild Trophy Banner (Boss Raid Buff Zone)")]
        public bool isGuildBannerPlanted = false;
        public float guildBannerHp = 1500f;
        public Vector3 guildBannerPosition;
        public float guildBannerRadius = 25f;

        public event Action<FloorWeather> OnWeatherChanged;
        public event Action<bool> OnCursedMaskPossessionStateChanged;
        public event Action<Vector3> OnGuildBannerPlanted;
        public event Action OnGuildBannerDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (isCursedMaskPossessionActive)
            {
                cursedMaskDurationRemaining -= Time.deltaTime;
                if (cursedMaskDurationRemaining <= 0f)
                {
                    EndCursedMaskPossession();
                }
            }
        }

        public void SetWeather(FloorWeather newWeather)
        {
            activeWeather = newWeather;
            OnWeatherChanged?.Invoke(newWeather);
            Debug.Log($"🌦️ [FLOOR WEATHER] Shifted to {newWeather}!");
        }

        public float GetWeatherSpellMultiplier(SpellAffinity affinity, out bool chainsToWetTargets)
        {
            chainsToWetTargets = (activeWeather == FloorWeather.Rainstorm && affinity == SpellAffinity.Lightning);

            if (activeWeather == FloorWeather.Rainstorm)
            {
                if (affinity == SpellAffinity.Fire) return 0.65f;
                if (affinity == SpellAffinity.Lightning) return 1.50f;
            }
            else if (activeWeather == FloorWeather.HeavyGaleWind)
            {
                if (affinity == SpellAffinity.Wind || affinity == SpellAffinity.Aero) return 1.75f;
            }
            else if (activeWeather == FloorWeather.AshStorm)
            {
                if (affinity == SpellAffinity.Fire) return 1.30f;
            }

            return 1.0f;
        }

        public void TriggerCursedMaskPossessionPrompt(ref float currentHp, ref float currentMp)
        {
            if (currentHp <= 1f && !isCursedMaskPossessionActive)
            {
                AcceptCursedMaskPossession(ref currentHp, ref currentMp);
            }
        }

        public void AcceptCursedMaskPossession(ref float currentHp, ref float currentMp)
        {
            isCursedMaskPossessionActive = true;
            cursedMaskDurationRemaining = CURSED_MASK_MAX_DURATION;
            currentHp = 1f; // Clamped at 1 HP invulnerable

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    "💀 [CURSED MASK] 'LET ME DRIVE!' 15-Second Berserk Possession Activated! +200% Slash Power & Invulnerability!", true);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
            }

            OnCursedMaskPossessionStateChanged?.Invoke(true);
            Debug.Log("[CURSED MASK] 'Let Me Drive!' 15-second Berserk Possession Activated!");
        }

        private void EndCursedMaskPossession()
        {
            isCursedMaskPossessionActive = false;
            cursedMaskDurationRemaining = 0f;

            // Exhaustion after possession: Leaves player with 1 Mana
            Debug.Log("[CURSED MASK] Berserk possession ended. Exhaustion leaves user at 1 Mana!");
            OnCursedMaskPossessionStateChanged?.Invoke(false);
        }

        public void PlantGuildTrophyBanner(Vector3 arenaPosition)
        {
            isGuildBannerPlanted = true;
            guildBannerHp = 1500f;
            guildBannerPosition = arenaPosition;

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    "🚩 [GUILD BANNER] Rallying Banner Planted! +30% Party Damage & Mana Regen inside the zone!", true);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }

            OnGuildBannerPlanted?.Invoke(arenaPosition);
            Debug.Log($"[Guild Banner] Planted at {arenaPosition}! +30% Party Damage & Mana Regen while standing!");
        }

        public void DamageGuildBanner(float damage)
        {
            if (!isGuildBannerPlanted) return;
            guildBannerHp -= damage;
            if (guildBannerHp <= 0f)
            {
                isGuildBannerPlanted = false;
                guildBannerHp = 0f;
                Debug.Log("[Guild Banner] Destroyed by enemies! Party buffs collapsed.");
                OnGuildBannerDestroyed?.Invoke();
            }
        }

        public bool IsPositionInBannerZone(Vector3 position)
        {
            return isGuildBannerPlanted && Vector3.Distance(position, guildBannerPosition) <= guildBannerRadius;
        }

        public string PullDungeonGambleShrine(ref int playerManaCores, int cost = 50)
        {
            if (playerManaCores < cost)
            {
                return "Insufficient Mana Cores to offer the Gamble Shrine.";
            }

            playerManaCores -= cost;
            float roll = UnityEngine.Random.value;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            if (roll >= 0.70f)
            {
                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast("🎰 [GAMBLE SHRINE] Golden Blessing Granted! +50% Crit Rate for this floor!", true);
                }
                return "Granted: Golden Blessing (+50% Crit Rate)";
            }
            else if (roll >= 0.35f)
            {
                return "Granted: Alchemical Refill (Full Mana & Durability Restored)";
            }
            else if (roll >= 0.15f)
            {
                return "Granted: Common Rune Essence (+25 Bonus Mana Cores)";
            }
            else
            {
                Debug.LogWarning("⚠️ [GAMBLE SHRINE] Trap Sprung! Floor Spikes activated!");
                return "Cursed Outcome: Floor Trap Triggered!";
            }
        }
    }
}
