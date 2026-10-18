using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Social
{
    /// <summary>
    /// Tavern Games, Shopkeeper Haggling, Secret Knock Doors, Monster Pit & Prison Wagons.
    /// Manages:
    /// 1. Two-Finger Arm Wrestling (rapid alternate taps to pin opponent).
    /// 2. Elemental Shot Roulette (Dragon-Fire chaos shot).
    /// 3. Secret Rhythm Knock doors to underground black markets.
    /// 4. Shopkeeper Haggling and culinary bribes with cooked dishes.
    /// 5. Underground Monster Pit wagering.
    /// 6. Campfire Multi-Instrument Band Jams (2+ players -> "Concert Hype" buff).
    /// 7. Bounty Prison Wagon escorts and hijacks.
    /// </summary>
    public class TavernSocialAndWorldEventsManager : MonoBehaviour
    {
        public static TavernSocialAndWorldEventsManager Instance { get; private set; }

        [Header("Two-Finger Arm Wrestling State")]
        [Range(0f, 1f)] public float armWrestleMeter = 0.5f; // 0.0 = Loss, 1.0 = Pin Victory
        public bool isArmWrestlingActive = false;

        [Header("Secret Knock Door Validator")]
        public List<float> requiredKnockIntervals = new List<float> { 0.25f, 0.25f, 0.60f, 0.25f };
        private List<float> playerTapTimestamps = new List<float>();
        public bool isBlackMarketDoorUnlocked = false;

        [Header("Campfire Multi-Instrument Jam")]
        public int activeJammingMusiciansCount = 0;
        public bool isConcertHypeBuffActive = false;

        [Header("Bounty Prison Wagon Event")]
        public bool isPrisonWagonActive = false;
        public float prisonWagonHp = 3500f;
        public bool isDefendingForGuild = true;

        public event Action<bool> OnArmWrestleFinished; // (didWin)
        public event Action OnDragonFireShotExplosion;
        public event Action OnSecretDoorUnlocked;
        public event Action<bool> OnConcertHypeBuffToggled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // --- 1. Two-Finger Arm Wrestling ---
        public void StartArmWrestlingMatch()
        {
            armWrestleMeter = 0.5f;
            isArmWrestlingActive = true;
            Debug.Log("💪 [ARM WRESTLING] Match started! Rapidly alternate tapping two fingers!");
        }

        public void RegisterArmWrestleAlternateTap(float opponentResistancePerSecond = 0.12f)
        {
            if (!isArmWrestlingActive) return;

            armWrestleMeter = Mathf.Clamp01(armWrestleMeter + 0.045f - (opponentResistancePerSecond * Time.deltaTime));

            if (armWrestleMeter >= 1.0f)
            {
                isArmWrestlingActive = false;
                Debug.Log("🏆 [ARM WRESTLING] PIN VICTORY! Opponent's arm slammed down!");
                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast("🏆 [TAVERN] Player won the Two-Finger Arm Wrestling match!", false);
                }
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
                }
                OnArmWrestleFinished?.Invoke(true);
            }
            else if (armWrestleMeter <= 0f)
            {
                isArmWrestlingActive = false;
                Debug.Log("💀 [ARM WRESTLING] Match lost! Your arm was pinned!");
                OnArmWrestleFinished?.Invoke(false);
            }
        }

        public void CounterArmWrestleSurge()
        {
            if (!isArmWrestlingActive) return;
            armWrestleMeter = Mathf.Clamp01(armWrestleMeter + 0.08f);
        }

        // --- 2. Elemental Shot Roulette ---
        public int TriggerElementalShotRoulette(int totalCupsRemaining)
        {
            bool hitDragonFireShot = UnityEngine.Random.Range(0, Mathf.Max(1, totalCupsRemaining)) == 0;
            if (hitDragonFireShot)
            {
                Debug.Log("🔥 [SHOT ROULETTE] DRAGON FIRE SHOT! Player breathes fire across the tavern!");

                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast(
                        "🔥 [TAVERN ROULETTE] DRAGON-FIRE SHOT TRIGGERED! Fiery tavern eruption!", true);
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
                }

                OnDragonFireShotExplosion?.Invoke();
                return -1; // Round ends in fiery chaos
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.7f);
            }

            return totalCupsRemaining - 1;
        }

        // --- 3. Secret Knock Door Validator ---
        public bool ValidateSecretDoorKnockTap()
        {
            playerTapTimestamps.Add(Time.time);

            if (playerTapTimestamps.Count <= requiredKnockIntervals.Count)
            {
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
                }
                return false;
            }

            // Verify intervals
            for (int i = 0; i < requiredKnockIntervals.Count; i++)
            {
                float actualInterval = playerTapTimestamps[i + 1] - playerTapTimestamps[i];
                if (Mathf.Abs(actualInterval - requiredKnockIntervals[i]) > 0.18f)
                {
                    playerTapTimestamps.Clear();
                    Debug.Log("🚪 [SECRET DOOR] Knock rhythm mismatched. Door remains sealed.");
                    return false;
                }
            }

            playerTapTimestamps.Clear();
            isBlackMarketDoorUnlocked = true;

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("🗝️ [BLACK MARKET] Secret Door Rhythm Knock Verified! Underground bazaar opened.", false);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            OnSecretDoorUnlocked?.Invoke();
            Debug.Log("[Secret Door] Rhythm Knock Verified! Black Market Unlocked.");
            return true;
        }

        // --- 4. Shopkeeper Haggling & Culinary Bribes ---
        public bool BribeMerchantWithCookedDish(string dishName, float dishHealingValue, out float discountPercentage, out string secretDoorPassword)
        {
            if (dishHealingValue >= 150f)
            {
                discountPercentage = 0.35f; // 35% discount
                secretDoorPassword = "Sable Serpent";
                Debug.Log($"🍷 [MERCHANT BRIBE] Merchant delighted by '{dishName}'! 35% discount granted and secret password revealed: '{secretDoorPassword}'!");
                return true;
            }

            discountPercentage = 0.15f;
            secretDoorPassword = "";
            return false;
        }

        // --- 5. Campfire Multi-Instrument Band Jam ---
        public bool EvaluateCampfireBandJamBuff(int simultaneousInstrumentPlayers)
        {
            activeJammingMusiciansCount = simultaneousInstrumentPlayers;
            bool grantBuff = activeJammingMusiciansCount >= 2;

            if (grantBuff != isConcertHypeBuffActive)
            {
                isConcertHypeBuffActive = grantBuff;
                OnConcertHypeBuffToggled?.Invoke(grantBuff);

                if (grantBuff)
                {
                    if (SystemBroadcastManager.Instance != null)
                    {
                        SystemBroadcastManager.Instance.DispatchBroadcast("🎶 [CONCERT HYPE] Band Jam synergy achieved! +25% Move Speed & +30% EXP Gain!", true);
                    }
                    if (BattleSummonerAudioMaster.Instance != null)
                    {
                        BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
                    }
                }
            }

            return grantBuff;
        }

        // --- 6. Underground Monster Pit Betting Arena ---
        public bool WagerOnMonsterPitBout(string monsterA, string monsterB, int wagerManaCores, int pickedMonsterIndex, out bool playerWon, out int payoutManaCores)
        {
            playerWon = UnityEngine.Random.value >= 0.45f;
            if (playerWon)
            {
                payoutManaCores = Mathf.RoundToInt(wagerManaCores * 2.2f);
                Debug.Log($"🐾 [MONSTER PIT] Victory! Wagered {wagerManaCores} and won {payoutManaCores} Mana Cores on {monsterA} vs {monsterB}!");
                return true;
            }

            payoutManaCores = 0;
            Debug.Log($"🐾 [MONSTER PIT] Defeat! Lost {wagerManaCores} Mana Cores on {monsterA} vs {monsterB}.");
            return false;
        }

        // --- 7. Bounty Prison Wagon Event ---
        public void StartPrisonWagonEvent(bool defendForGuildPay, int bountyManaCores)
        {
            isPrisonWagonActive = true;
            isDefendingForGuild = defendForGuildPay;
            prisonWagonHp = 3500f;

            string objective = defendForGuildPay ? "Guard the Armored Wagon against ambushers!" : "Attack the wheels to hijack the wagon and free the prisoners!";
            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast($"🛡️ [PRISON WAGON] Event Started: {objective} (Reward: {bountyManaCores} Cores)", true);
            }
        }
    }
}
