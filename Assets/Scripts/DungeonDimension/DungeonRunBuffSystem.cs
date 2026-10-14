using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public enum DungeonBoonRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum DungeonBoonType
    {
        DamageBoost,       // % Melee & Spell damage
        MoveSpeed,         // % Movement & Dash speed
        VampiricLeech,     // % Life steal on hits
        StaticConduit,     // % Chance to chain lightning on hits
        TitansAegis,       // +Flat Max HP & +% Armor
        ManaTorrent,       // -% Cooldowns & +% Mana regen
        FamiliarFrenzy,    // % Pet familiar damage & attack rate
        ExecuteStrike,     // Instantly destroys foes below HP %
        CullTheWeak        // Slain foes detonate in a kinetic blast
    }

    [System.Serializable]
    public class DungeonBoon
    {
        public string id;
        public string name;
        public DungeonBoonRarity rarity;
        public DungeonBoonType boonType;
        public string description;
        public string iconSpriteName;
        public Color themeColor = Color.white;
        public float basePotency;
        public int stackCount = 1;

        public float TotalPotency => basePotency * stackCount;

        public DungeonBoon Clone()
        {
            return new DungeonBoon
            {
                id = this.id,
                name = this.name,
                rarity = this.rarity,
                boonType = this.boonType,
                description = this.description,
                iconSpriteName = this.iconSpriteName,
                themeColor = this.themeColor,
                basePotency = this.basePotency,
                stackCount = this.stackCount
            };
        }

        public string GetRarityTag()
        {
            return rarity switch
            {
                DungeonBoonRarity.Common => "<color=#C0C8D8>[COMMON]</color>",
                DungeonBoonRarity.Rare => "<color=#30D0FF>[RARE]</color>",
                DungeonBoonRarity.Epic => "<color=#B050FF>[EPIC]</color>",
                DungeonBoonRarity.Legendary => "<color=#FFD700>[LEGENDARY]</color>",
                _ => "[BOON]"
            };
        }

        public Color GetRarityColor()
        {
            return rarity switch
            {
                DungeonBoonRarity.Common => new Color(0.75f, 0.80f, 0.90f),
                DungeonBoonRarity.Rare => new Color(0.20f, 0.85f, 1.00f),
                DungeonBoonRarity.Epic => new Color(0.75f, 0.30f, 1.00f),
                DungeonBoonRarity.Legendary => new Color(1.00f, 0.85f, 0.20f),
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// DungeonRunBuffSystem manages temporary, run-specific blessings acquired upon clearing dungeon rooms:
    /// - Rewards 3 random Boons after each room clear (Hades / Slay the Spire style).
    /// - Temporary buffs persist strictly for the duration of the current dungeon dive.
    /// - Automatically resets when the player returns to the overworld or is defeated.
    /// </summary>
    public class DungeonRunBuffSystem : MonoBehaviour
    {
        private static DungeonRunBuffSystem _instance;
        public static DungeonRunBuffSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DungeonRunBuffSystem>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DungeonRunBuffSystem");
                        _instance = go.AddComponent<DungeonRunBuffSystem>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Active Run Boons")]
        [SerializeField] private List<DungeonBoon> activeBoons = new List<DungeonBoon>();

        public IReadOnlyList<DungeonBoon> ActiveBoons => activeBoons;

        public event Action<DungeonBoon> OnBoonAcquired;
        public event Action OnBuffsReset;

        private static readonly List<DungeonBoon> Catalog = new List<DungeonBoon>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeCatalog();
        }

        private static void InitializeCatalog()
        {
            if (Catalog.Count > 0) return;

            Catalog.Add(new DungeonBoon
            {
                id = "boon_void_edge",
                name = "Void Edge",
                rarity = DungeonBoonRarity.Common,
                boonType = DungeonBoonType.DamageBoost,
                description = "+25% Melee & Spell Damage for this dungeon run.",
                iconSpriteName = "Mockup_Boon_VoidEdge.png",
                themeColor = new Color(0.85f, 0.35f, 0.95f),
                basePotency = 0.25f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_chrono_stride",
                name = "Chrono Stride",
                rarity = DungeonBoonRarity.Common,
                boonType = DungeonBoonType.MoveSpeed,
                description = "+30% Movement & Dash Sprint speed for this dungeon run.",
                iconSpriteName = "Mockup_Boon_ChronoStride.png",
                themeColor = new Color(0.20f, 0.90f, 1.00f),
                basePotency = 0.30f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_vampiric_leech",
                name = "Vampiric Leech",
                rarity = DungeonBoonRarity.Rare,
                boonType = DungeonBoonType.VampiricLeech,
                description = "Attacks and spells restore 8% of damage dealt as Health.",
                iconSpriteName = "Mockup_Boon_VampiricLeech.png",
                themeColor = new Color(1.00f, 0.20f, 0.30f),
                basePotency = 0.08f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_static_conduit",
                name = "Static Conduit",
                rarity = DungeonBoonRarity.Rare,
                boonType = DungeonBoonType.StaticConduit,
                description = "Hits have a 35% chance to discharge chain lightning to 3 nearby foes.",
                iconSpriteName = "Mockup_Boon_StaticConduit.png",
                themeColor = new Color(0.40f, 0.85f, 1.00f),
                basePotency = 0.35f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_titans_aegis",
                name = "Titan's Aegis",
                rarity = DungeonBoonRarity.Rare,
                boonType = DungeonBoonType.TitansAegis,
                description = "+150 Max Health and +25% Armor / Damage Reduction.",
                iconSpriteName = "Mockup_Boon_TitansAegis.png",
                themeColor = new Color(1.00f, 0.75f, 0.20f),
                basePotency = 150f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_mana_torrent",
                name = "Mana Torrent",
                rarity = DungeonBoonRarity.Epic,
                boonType = DungeonBoonType.ManaTorrent,
                description = "-35% Spell Cooldowns & +50% Mana Recovery Rate.",
                iconSpriteName = "Mockup_Boon_ManaTorrent.png",
                themeColor = new Color(0.30f, 0.60f, 1.00f),
                basePotency = 0.35f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_familiar_frenzy",
                name = "Familiar Frenzy",
                rarity = DungeonBoonRarity.Epic,
                boonType = DungeonBoonType.FamiliarFrenzy,
                description = "All bonded familiars deal +50% damage and attack 45% faster.",
                iconSpriteName = "Mockup_Boon_FamiliarFrenzy.png",
                themeColor = new Color(0.40f, 1.00f, 0.50f),
                basePotency = 0.50f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_execute_strike",
                name = "Execute Strike",
                rarity = DungeonBoonRarity.Legendary,
                boonType = DungeonBoonType.ExecuteStrike,
                description = "Foes below 22% HP are instantly destroyed by spatial fractures.",
                iconSpriteName = "Mockup_Boon_ExecuteStrike.png",
                themeColor = new Color(1.00f, 0.85f, 0.20f),
                basePotency = 0.22f
            });

            Catalog.Add(new DungeonBoon
            {
                id = "boon_cull_weak",
                name = "Cull the Weak",
                rarity = DungeonBoonRarity.Legendary,
                boonType = DungeonBoonType.CullTheWeak,
                description = "Defeated enemies detonate in a kinetic shockwave dealing 40% of their Max HP.",
                iconSpriteName = "Mockup_Boon_CullWeak.png",
                themeColor = new Color(1.00f, 0.40f, 0.20f),
                basePotency = 0.40f
            });
        }

        public List<DungeonBoon> RollBoonChoices(int count = 3)
        {
            InitializeCatalog();
            List<DungeonBoon> pool = new List<DungeonBoon>();
            foreach (var b in Catalog)
            {
                pool.Add(b.Clone());
            }

            // Shuffle
            System.Random rnd = new System.Random();
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int k = rnd.Next(i + 1);
                var temp = pool[i];
                pool[i] = pool[k];
                pool[k] = temp;
            }

            int pickCount = Mathf.Min(count, pool.Count);
            return pool.GetRange(0, pickCount);
        }

        public void ApplyBoon(DungeonBoon boon)
        {
            if (boon == null) return;
            if (activeBoons == null) activeBoons = new List<DungeonBoon>();

            var existing = activeBoons.Find(b => b.id == boon.id);
            if (existing != null)
            {
                existing.stackCount++;
                Debug.Log($"✨ [DUNGEON BUFF] Stacked {existing.name} x{existing.stackCount} (+{existing.TotalPotency})");
                OnBoonAcquired?.Invoke(existing);
            }
            else
            {
                var newBoon = boon.Clone();
                activeBoons.Add(newBoon);
                Debug.Log($"✨ [DUNGEON BUFF] Acquired {newBoon.name} ({newBoon.GetRarityTag()})");
                OnBoonAcquired?.Invoke(newBoon);
            }

            // If Titan's Aegis, grant immediate health
            if (boon.boonType == DungeonBoonType.TitansAegis)
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null)
                {
                    prog.maxHp += boon.basePotency;
                    prog.currentHp = Mathf.Min(prog.currentHp + boon.basePotency, prog.maxHp);
                }
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("boon_card_select", 1.0f);
            }
        }

        public void ResetRunBuffs()
        {
            if (activeBoons == null) activeBoons = new List<DungeonBoon>();

            // Remove Titan's Aegis extra health if applicable
            var titan = activeBoons.Find(b => b.boonType == DungeonBoonType.TitansAegis);
            if (titan != null)
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null)
                {
                    prog.maxHp = Mathf.Max(100f, prog.maxHp - titan.TotalPotency);
                    prog.currentHp = Mathf.Min(prog.currentHp, prog.maxHp);
                }
            }

            activeBoons.Clear();
            Debug.Log("🔄 [DUNGEON BUFF] Temporary run boons cleared.");
            OnBuffsReset?.Invoke();
        }

        #region Combat Multipliers
        public float GetDamageMultiplier()
        {
            float mult = 1.0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.DamageBoost)
                {
                    mult += b.TotalPotency;
                }
            }
            return mult;
        }

        public float GetMoveSpeedMultiplier()
        {
            float mult = 1.0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.MoveSpeed)
                {
                    mult += b.TotalPotency;
                }
            }
            return mult;
        }

        public float GetCooldownReduction()
        {
            float cdr = 0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.ManaTorrent)
                {
                    cdr += b.TotalPotency;
                }
            }
            return Mathf.Clamp(cdr, 0f, 0.70f);
        }

        public float GetLifeStealPercent()
        {
            float leech = 0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.VampiricLeech)
                {
                    leech += b.TotalPotency;
                }
            }
            return leech;
        }

        public float GetChainLightningChance()
        {
            float chance = 0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.StaticConduit)
                {
                    chance += b.TotalPotency;
                }
            }
            return Mathf.Clamp01(chance);
        }

        public float GetFamiliarDamageMultiplier()
        {
            float mult = 1.0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.FamiliarFrenzy)
                {
                    mult += b.TotalPotency;
                }
            }
            return mult;
        }

        public float GetExecuteThreshold()
        {
            float thresh = 0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.ExecuteStrike)
                {
                    thresh = Mathf.Max(thresh, b.TotalPotency);
                }
            }
            return thresh;
        }

        public bool HasSlainExplosion(out float damagePercent)
        {
            damagePercent = 0f;
            foreach (var b in activeBoons)
            {
                if (b.boonType == DungeonBoonType.CullTheWeak)
                {
                    damagePercent += b.TotalPotency;
                }
            }
            return damagePercent > 0f;
        }
        #endregion
    }
}
