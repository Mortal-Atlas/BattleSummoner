using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Core.DimensionalCompiler;
using BattleSummoner.Hobbies;

namespace BattleSummoner.Core
{
    [Serializable]
    public struct DevouredMonsterCatalyst
    {
        public string monsterPartId;
        public string monsterPartDisplayName;
        public string unlockedFormOrCounterTrait;
        public float bonusBaseDamage;
        public float bonus5DDensity;
        public string counterElementTag;
    }

    [Serializable]
    public struct ArtisanFusionRecipe
    {
        public string recipeName;
        public string primaryWeaponArchetype;
        public string secondaryMechanismId;
        public float fusedBaseDamage;
        public string compiled9DInjection;
    }

    public class BloodAndHearthMasterDirector : MonoBehaviour
    {
        public static BloodAndHearthMasterDirector Instance { get; private set; }

        [Header("1. 9-Ally Persona / Metaphor Raid HUD (-5.5 Deg Gothic Shear)")]
        [Tooltip("All 9 active squadmate portraits along the right vertical perimeter column.")]
        public RectTransform[] nineAllyPortraitSlots = new RectTransform[9];
        public float gothicPortraitShearZ = -5.5f;

        [Header("2. Visceral Gore, Armor Grime & Wizardry Screen-Glass Spatter")]
        [Range(0f, 1f)] public float armorBloodAndAshGrime = 0f;
        [Range(0f, 1f)] public float viewportGlassBloodAlpha = 0f;
        public float screenBloodDripSpeed = 0.06f;
        public Renderer[] playerArmorRenderers;
        public CanvasGroup viewportPerimeterBloodOverlay;
        public AudioSource visceralAudioSource;
        public AudioClip arterialSlashSplatterClip;
        public AudioClip whetstoneBloodWipeShingClip;

        [Header("3. DanMachi + Persona 'Guild Hearth' Excelsia Leveling")]
        public int playerLevel = 1;
        public float unboundExcelsiaPool = 0f;
        public int hearthBondRank = 1;
        public bool isCleanedAndRestedBuffActive = false;

        [Header("4. Fairy Tail / Ni no Kuni 'Tavern Chorus' Drinking Song Buff")]
        public bool isTavernChorusWardActive = false;
        public int activeChorusVoicesCount = 1;
        public float tavernChorusBuffRemainingSeconds = 0f;
        public AudioSource[] tavernChorusHarmonyLayers; // Solo Lute -> 5 Voices -> 20-Voice Norse Hall Chorus

        [Header("5. 'Anything Is A Weapon' & Improvised Prop Mastery")]
        private readonly Dictionary<string, float> _improvisedWeaponMastery = new Dictionary<string, float>();
        public const float WOK_HIT_COOKING_EXP_TRICKLE = 0.05f;

        [Header("6. Shield Hero Monster-Feed Evolution & Artisan Fusion")]
        public List<DevouredMonsterCatalyst> absorbedHeirloomCatalysts = new List<DevouredMonsterCatalyst>();
        public List<ArtisanFusionRecipe> unlockedArtisanFusions = new List<ArtisanFusionRecipe>()
        {
            new ArtisanFusionRecipe {
                recipeName = "Ignis Gunblade",
                primaryWeaponArchetype = "Broadsword",
                secondaryMechanismId = "SteamFlintlockCylinder",
                fusedBaseDamage = 145f,
                compiled9DInjection = "Industrial gunblade chambered for pressurized steam ignition and heavy inertia."
            },
            new ArtisanFusionRecipe {
                recipeName = "hell-Bore Shotgun-Axe",
                primaryWeaponArchetype = "GreatAxe",
                secondaryMechanismId = "ScatterblastBreech",
                fusedBaseDamage = 170f,
                compiled9DInjection = "Heavy executioner axe fused with a point-blank scatterblast furnace breech."
            },
            new ArtisanFusionRecipe {
                recipeName = "Repeating Halberd-Glaive",
                primaryWeaponArchetype = "Halberd",
                secondaryMechanismId = "ClockworkCrossbowDrum",
                fusedBaseDamage = 135f,
                compiled9DInjection = "Long-reach polearm housing a rapid-fire torsion bolt drum."
            },
            new ArtisanFusionRecipe {
                recipeName = "Piston Bear-Claws",
                primaryWeaponArchetype = "GauntletClaws",
                secondaryMechanismId = "PneumaticSteamPiston",
                fusedBaseDamage = 125f,
                compiled9DInjection = "Pressurized steam-piston gauntlets forged in spite to crush armor seams."
            }
        };

        private MaterialPropertyBlock _grimePropBlock;
        private float _dripUvOffset = 0f;

        private static readonly int BloodGrimeMaskID = Shader.PropertyToID("_BloodAndAshGrimeMask");
        private static readonly int ViewportDripOffsetID = Shader.PropertyToID("_ViewportDripOffset");

        public event Action<int, float> OnHearthLevelUpCrystallized;
        public event Action<string, int> OnImprovisedSkillLevelUp;
        public event Action<DevouredMonsterCatalyst> OnHeirloomMonsterPartDevoured;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _grimePropBlock = new MaterialPropertyBlock();
            EnforceNinePortraitGothicShear();
        }

        private void Update()
        {
            TickViewportBloodDripAndFade();
            TickTavernChorusDuration();
        }

        // ====================================================================
        // 1. 9-ALLY PERSONA / METAPHOR RAID UI ENFORCEMENT
        // ====================================================================
        public void EnforceNinePortraitGothicShear()
        {
            if (nineAllyPortraitSlots == null) return;
            for (int i = 0; i < nineAllyPortraitSlots.Length; i++)
            {
                if (nineAllyPortraitSlots[i] != null)
                {
                    nineAllyPortraitSlots[i].localEulerAngles = new Vector3(0f, 0f, gothicPortraitShearZ);
                }
            }
        }

        // ====================================================================
        // 2. VISCERAL BLOOD SPLATTER, ARMOR GRIME & WHETSTONE BLOOD-WIPE
        // ====================================================================
        private void TickViewportBloodDripAndFade()
        {
            if (viewportGlassBloodAlpha > 0f)
            {
                _dripUvOffset += Time.deltaTime * 0.04f;
                viewportGlassBloodAlpha = Mathf.MoveTowards(viewportGlassBloodAlpha, 0f, Time.deltaTime * screenBloodDripSpeed);
                if (viewportPerimeterBloodOverlay != null)
                {
                    viewportPerimeterBloodOverlay.alpha = viewportGlassBloodAlpha;
                }
            }
        }

        /// <summary>
        /// Invoked by FruitNinjaBladeCutter.cs and SeverableBossLimb.cs on every live melee slash or ambush hit.
        /// Accumulates armor blood/ash grime, spatters the 9:16 screen perimeter on criticals/severs,
        /// and banks DanMachi Unbound Excelsia.
        /// </summary>
        public void RegisterVisceralCombatHit(
            Vector3 worldHitPos,
            Vector3 slashDirection,
            float rawDamageDealt,
            bool isCriticalOrLimbSever,
            bool isPlayerTakingPointBlankHit = false)
        {
            // 1. Accumulate arterial blood & dungeon ash on player armor
            float grimeDelta = isCriticalOrLimbSever ? 0.14f : 0.035f;
            armorBloodAndAshGrime = Mathf.Clamp01(armorBloodAndAshGrime + grimeDelta);
            ApplyArmorGrimePropertyBlock();

            // 2. Wizardry Variants Daphne 9:16 Screen-Glass Blood Spatter
            if (isCriticalOrLimbSever || isPlayerTakingPointBlankHit)
            {
                viewportGlassBloodAlpha = Mathf.Clamp01(viewportGlassBloodAlpha + (isCriticalOrLimbSever ? 0.50f : 0.35f));
                if (viewportPerimeterBloodOverlay != null)
                {
                    viewportPerimeterBloodOverlay.alpha = viewportGlassBloodAlpha;
                }
                if (visceralAudioSource != null && arterialSlashSplatterClip != null)
                {
                    visceralAudioSource.PlayOneShot(arterialSlashSplatterClip, 0.85f);
                }
            }

            // 3. Bank Unbound Excelsia (DanMachi lived experience)
            float excelsiaGain = rawDamageDealt * (isCriticalOrLimbSever ? 0.30f : 0.12f);
            unboundExcelsiaPool += excelsiaGain;
        }

        /// <summary>
        /// Invoked mid-dungeon when the player uses a Whetstone & Oil Cloth to restore WeaponTemper.cs.
        /// Physically wipes steaming monster blood off the blade and partially clears armor grime.
        /// </summary>
        public void ExecuteMidCombatWhetstoneBloodWipe(WeaponTemper activeWeaponTemper)
        {
            armorBloodAndAshGrime = Mathf.Max(0f, armorBloodAndAshGrime - 0.35f);
            ApplyArmorGrimePropertyBlock();

            if (activeWeaponTemper != null)
            {
                activeWeaponTemper.currentDurability = activeWeaponTemper.maxDurability;
            }

            if (visceralAudioSource != null && whetstoneBloodWipeShingClip != null)
            {
                visceralAudioSource.PlayOneShot(whetstoneBloodWipeShingClip, 1.0f);
            }

            Debug.Log("[Whetstone Blood-Wipe] Wiped steaming arterial blood from steel! Blade Temper restored to 100%.");
        }

        private void ApplyArmorGrimePropertyBlock()
        {
            if (playerArmorRenderers == null) return;
            for (int i = 0; i < playerArmorRenderers.Length; i++)
            {
                Renderer r = playerArmorRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_grimePropBlock);
                _grimePropBlock.SetFloat(BloodGrimeMaskID, armorBloodAndAshGrime);
                _grimePropBlock.SetFloat(ViewportDripOffsetID, _dripUvOffset);
                r.SetPropertyBlock(_grimePropBlock);
            }
        }

        // ====================================================================
        // 3. DANMACHI + PERSONA "EARNED HEARTH" LEVELING & TAVERN CHORUS BUFF
        // ====================================================================
        public float GetRequiredExcelsiaForNextLevel() => 120f * Mathf.Pow(playerLevel, 1.85f);

        /// <summary>
        /// Triggered when stepping into Solvang's Grand Norse Guild Hall Hearth, Bathhouse, or Dungeon Campfire.
        /// Scrubs all dungeon blood/grime, crystallizes Unbound Excelsia into permanent levels,
        /// and triggers the multi-voice Tavern Drinking Song Chorus ward.
        /// </summary>
        public void ExecuteTavernHearthRitual(int participatingSquadAndShadowVoices = 10)
        {
            // 1. Scrub all dungeon blood & ash grime back to 0.0f
            armorBloodAndAshGrime = 0f;
            viewportGlassBloodAlpha = 0f;
            ApplyArmorGrimePropertyBlock();
            if (viewportPerimeterBloodOverlay != null) viewportPerimeterBloodOverlay.alpha = 0f;
            isCleanedAndRestedBuffActive = true;

            // 2. Crystallize DanMachi Unbound Excelsia into permanent Level-Ups
            int levelsGained = 0;
            while (unboundExcelsiaPool >= GetRequiredExcelsiaForNextLevel())
            {
                unboundExcelsiaPool -= GetRequiredExcelsiaForNextLevel();
                playerLevel++;
                levelsGained++;
            }

            if (levelsGained > 0)
            {
                hearthBondRank++;
                OnHearthLevelUpCrystallized?.Invoke(playerLevel, unboundExcelsiaPool);
                Debug.Log($"[Guild Hearth Ritual] Crystallized dungeon sweat & scars by the fire! Gained +{levelsGained} Level(s) -> Now Lv.{playerLevel} (Bond Rank {hearthBondRank})!");
            }

            // 3. Activate Tavern Chorus Drinking Song Buff
            StartTavernDrinkingSongChorus(participatingSquadAndShadowVoices);
        }

        public void StartTavernDrinkingSongChorus(int voiceCount)
        {
            activeChorusVoicesCount = Mathf.Clamp(voiceCount, 1, 20);
            isTavernChorusWardActive = true;
            tavernChorusBuffRemainingSeconds = 900f; // 15-minute Hearth-Warmth ward for the next Spire delve

            // Dynamically unmute acoustic harmony stems based on how many squadmates/shadows raise tankards
            if (tavernChorusHarmonyLayers != null)
            {
                for (int i = 0; i < tavernChorusHarmonyLayers.Length; i++)
                {
                    if (tavernChorusHarmonyLayers[i] == null) continue;
                    bool shouldPlayLayer = (i == 0) || (activeChorusVoicesCount >= i * 4);
                    tavernChorusHarmonyLayers[i].mute = !shouldPlayLayer;
                    if (shouldPlayLayer && !tavernChorusHarmonyLayers[i].isPlaying)
                    {
                        tavernChorusHarmonyLayers[i].Play();
                    }
                }
            }

            Debug.Log($"[Tavern Chorus] {activeChorusVoicesCount}-Voice Norse Drinking Song echoing! Granted 'Hearth-Warmth' (+25% 11D Wavefunction Stability & Cold-Grey Sanity Immunity).");
        }

        private void TickTavernChorusDuration()
        {
            if (!isTavernChorusWardActive) return;
            tavernChorusBuffRemainingSeconds -= Time.deltaTime;
            if (tavernChorusBuffRemainingSeconds <= 0f)
            {
                isTavernChorusWardActive = false;
            }
        }

        // ====================================================================
        // 4. "ANYTHING IS A WEAPON" & +0.05 WOK COOKING EXP TRICKLE
        // ====================================================================
        public float ExecuteImprovisedPropStrike(
            string propArchetypeName,
            bool isCulinaryWokOrSkillet,
            float swipeVelocityMagnitude,
            DimensionalArtifact prop12DArtifact)
        {
            string skillName = $"{propArchetypeName} Combat";
            if (!_improvisedWeaponMastery.TryGetValue(skillName, out float currentSkillLv))
            {
                currentSkillLv = 1f;
                _improvisedWeaponMastery[skillName] = currentSkillLv;
                Debug.Log($"[Anything Is A Weapon] Dynamically generated new proficiency: '{skillName}' (Lv.1)!");
            }
            else
            {
                float prevFloor = Mathf.Floor(currentSkillLv);
                currentSkillLv += 0.12f;
                _improvisedWeaponMastery[skillName] = currentSkillLv;
                if (Mathf.Floor(currentSkillLv) > prevFloor)
                {
                    OnImprovisedSkillLevelUp?.Invoke(skillName, Mathf.FloorToInt(currentSkillLv));
                }
            }

            // Bashing a monster with a Wok or Cast-Iron Skillet trickles +0.05 Cooking EXP per hit!
            if (isCulinaryWokOrSkillet)
            {
                if (HobbyMasteryManager.Instance != null)
                {
                    HobbyMasteryManager.Instance.AddCookingExp(WOK_HIT_COOKING_EXP_TRICKLE);
                }
                if (HobbyMasteryEngine.Instance != null)
                {
                    HobbyMasteryEngine.Instance.AwardHobbyExp(HobbyDiscipline.Cooking, WOK_HIT_COOKING_EXP_TRICKLE, 1.0f);
                }
            }

            // Calculate kinetic impact using 4D Mass & 5D Density from the 0D-11D Compiler
            float densityScalar = 1.0f;
            if (prop12DArtifact != null)
            {
                prop12DArtifact.Compile9DMetadata(isTavernChorusWardActive);
                densityScalar = Mathf.Max(1.0f, prop12DArtifact.densityMatrix_5D * 0.1f);
            }

            float masteryScalar = 1.0f + (currentSkillLv * 0.04f);
            return swipeVelocityMagnitude * densityScalar * masteryScalar;
        }

        // ====================================================================
        // 5. SHIELD HERO / EXILED KNIGHT MONSTER-FEEDING GEAR EVOLUTION
        // ====================================================================
        public void FeedSeveredMonsterPartToHeirloom(
            DevouredMonsterCatalyst catalyst,
            WeaponTemper heirloomTemper,
            DimensionalArtifact heirloom12DArtifact)
        {
            if (!absorbedHeirloomCatalysts.Exists(c => c.monsterPartId == catalyst.monsterPartId))
            {
                absorbedHeirloomCatalysts.Add(catalyst);
            }

            if (heirloomTemper != null)
            {
                heirloomTemper.baseDamage += catalyst.bonusBaseDamage;
                heirloomTemper.maxDurability += 25f;
                heirloomTemper.currentDurability = heirloomTemper.maxDurability;
            }

            if (heirloom12DArtifact != null)
            {
                heirloom12DArtifact.semanticDescription_9D +=
                    $" Devoured {catalyst.monsterPartDisplayName} [{catalyst.unlockedFormOrCounterTrait}].";
                heirloom12DArtifact.Compile9DMetadata(isTavernChorusWardActive);
            }

            OnHeirloomMonsterPartDevoured?.Invoke(catalyst);
            Debug.Log($"[Heirloom Monster-Feed] Devoured '{catalyst.monsterPartDisplayName}'! Permanently unlocked form/trait: {catalyst.unlockedFormOrCounterTrait}!");
        }
    }
}
