using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.World;

public static class MetropolisLifeAndAestheticPopulator
{
    [MenuItem("BattleSummoner/Populate World & Beautify Metropolis", false, 20)]
    public static string PopulateAndBeautify()
    {
        var sc = EditorSceneManager.GetActiveScene();
        if (sc.name != "OverworldMap")
        {
            sc = EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
        }

        int npcsConfigured = ConfigureAllNPCs();
        int doorwaysConfigured = ConfigureBuildingDoorways();
        int aestheticsPlaced = PlaceAestheticNatureAndVFX();

        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveOpenScenes();

        return $"✦ METROPOLIS ENHANCED ✦\n- {npcsConfigured} Living NPCs equipped with [E] TALK and unique dialogue!\n- {doorwaysConfigured} Building Doorways connected to Interiors & Campfire Hub!\n- {aestheticsPlaced} Aesthetic Cherry Willows, Fluttering Butterflies & Motes placed!";
    }

    private static int ConfigureAllNPCs()
    {
        var npcsRoot = GameObject.Find("LIVELY_METROPOLIS_NPCS");
        if (npcsRoot == null) return 0;

        var animators = npcsRoot.GetComponentsInChildren<Animator>(true);
        int count = 0;

        foreach (var anim in animators)
        {
            var go = anim.gameObject;
            var inter = go.GetComponent<OverworldNPCInteractable>();
            if (inter == null) inter = go.AddComponent<OverworldNPCInteractable>();

            // Setup CapsuleCollider
            var col = go.GetComponent<CapsuleCollider>();
            if (col == null) col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.45f;
            col.isTrigger = true;

            // Configure identity based on name
            ConfigureNPCData(go.name, inter);

            count++;
        }

        return count;
    }

    private static void ConfigureNPCData(string goName, OverworldNPCInteractable inter)
    {
        inter.interactionRadius = 3.8f;
        inter.overheadBadgeHeight = 2.15f;
        inter.interactKey = KeyCode.E;

        // 1. Story Companions
        if (goName.Contains("Margo"))
        {
            inter.npcName = "Margo";
            inter.npcRole = "Vanguard Cleric & Tactician";
            inter.nameplateColor = new Color(0.95f, 0.78f, 0.25f);
            inter.dialogueLines = new[]
            {
                "Summoner! Keep your focus sharpened—the Hundredfold Spire's dimensional pulses are destabilizing the outer perimeter.",
                "Whenever you feel exhausted from delving the rifts, meet me at the campfire hub outside the commercial district.",
                "Remember to temper your weapon at the blacksmith before challenging the Core Sentinel."
            };
        }
        else if (goName.Contains("Barnaby"))
        {
            inter.npcName = "Barnaby";
            inter.npcRole = "Heavy Juggernaut Vanguard";
            inter.nameplateColor = new Color(0.92f, 0.45f, 0.20f);
            inter.dialogueLines = new[]
            {
                "Hahaha! Ready to bash some dimensional void-beasts? My shield is polished and ready.",
                "The guards at the South Gate said minion patrols were spotted near the outer moat bridges.",
                "If you need pure defensive staying power, summon me during the next boss confrontation!"
            };
        }
        else if (goName.Contains("Zephyr"))
        {
            inter.npcName = "Zephyr";
            inter.npcRole = "Mistwood Ranger & Scout";
            inter.nameplateColor = new Color(0.35f, 0.85f, 0.55f);
            inter.dialogueLines = new[]
            {
                "The wind carries strange resonance from the northeast crags. The rifts are widening.",
                "I scouted ahead—Aethelgard guards the lower sanctum with kinetic ground shockwaves.",
                "Dash using [Left Control] the instant you see its slam telegraph charging magenta!"
            };
        }
        // 2. City Guards & Bastion Sentinels
        else if (goName.Contains("Guard_Cathedral"))
        {
            inter.npcName = goName.Contains("Left") ? "Sentinel Arthur" : "Sentinel Gareth";
            inter.npcRole = "High Acropolis Guard";
            inter.nameplateColor = new Color(0.25f, 0.75f, 0.98f);
            inter.dialogueLines = new[]
            {
                "Halt, traveler. You stand before the Grand Gothic Cathedral of Aethelburg.",
                "The sacred spire bell rings only when dimensional incursions breach the third tier.",
                "May the golden dawn illuminate your summoning glyphs."
            };
        }
        else if (goName.Contains("Guard_Citadel"))
        {
            inter.npcName = "Citadel Gatekeeper";
            inter.npcRole = "Inner Tier Sentinel";
            inter.nameplateColor = new Color(0.25f, 0.75f, 0.98f);
            inter.dialogueLines = new[]
            {
                "Ascending to Tier 4 requires authorization from the Archon Council.",
                "The grand hover-lanes overhead channel anti-gravity flux directly into the Spire core.",
                "Keep your weapons sheathed within the inner acropolis promenade."
            };
        }
        else if (goName.Contains("Guard_") && goName.Contains("Gate"))
        {
            string gate = goName.Contains("North") ? "North Bastion" : (goName.Contains("East") ? "East Port" : (goName.Contains("South") ? "South Promenade" : "West Wall"));
            inter.npcName = $"Sentinel of the {gate}";
            inter.npcRole = "Curtain Wall Watcher";
            inter.nameplateColor = new Color(0.25f, 0.75f, 0.98f);
            inter.dialogueLines = new[]
            {
                $"You are standing at the {gate} Bastion. Beyond lies the outer circular canal and continental wilds.",
                "The dimensional rift tear is marked by violet energy pylons in the field.",
                "Step through the outpost vortex when you are geared for dungeon trial."
            };
        }
        // 3. Merchants & Artisans
        else if (goName.Contains("Merchant_Baker_Helena"))
        {
            inter.npcName = "Helena";
            inter.npcRole = "Master Baker";
            inter.nameplateColor = new Color(0.98f, 0.72f, 0.22f);
            inter.dialogueLines = new[]
            {
                "Welcome to the Sun-Wheat Hearth! Fresh brioche, sweet buns, and golden loaves right out of the oven!",
                "Adventurers swear by my honey rolls—they restore stamina faster than any cheap potion.",
                "The agrarian farmers brought in a wagon of fresh grain from the south fields this morning."
            };
        }
        else if (goName.Contains("Artisan_Smith_Apprentice"))
        {
            inter.npcName = "Smith Torin";
            inter.npcRole = "Forge Apprentice";
            inter.nameplateColor = new Color(0.95f, 0.45f, 0.20f);
            inter.dialogueLines = new[]
            {
                "The master smith is working on high-temperature living gear tempering inside the forge.",
                "Keep your blades polished and check your armor dyes at the customizer station!",
                "Void-corrupted beasts drop raw ore fragments you can smelt into reinforcement ingots."
            };
        }
        else if (goName.Contains("Merchant_Apothecary_Linus"))
        {
            inter.npcName = "Linus";
            inter.npcRole = "Grand Herbalist & Alchemist";
            inter.nameplateColor = new Color(0.20f, 0.85f, 0.65f);
            inter.dialogueLines = new[]
            {
                "Ah, a summoner! Looking for concentrated mana drafts or restorative healing vials?",
                "The wild mountain lilies blooming along the cliffside have exceptionally pure etheric resonance.",
                "Drink your restorative potions with [R] whenever your vitals flash red in combat."
            };
        }
        else if (goName.Contains("Merchant_Spices_Amira"))
        {
            inter.npcName = "Amira";
            inter.npcRole = "Silk & Spice Importer";
            inter.nameplateColor = new Color(0.90f, 0.40f, 0.70f);
            inter.dialogueLines = new[]
            {
                "Aroma of saffron, cinnamon, and star-anise from the southern desert trade caravans!",
                "The harbor merchants just unloaded three crates of gossamer silks from the eastern archipelago.",
                "Fine fabrics make all the difference when tailoring enchanted robes."
            };
        }
        else if (goName.Contains("Merchant_Weapons_Goran"))
        {
            inter.npcName = "Goran";
            inter.npcRole = "Arsenal Quartermaster";
            inter.nameplateColor = new Color(0.85f, 0.35f, 0.35f);
            inter.dialogueLines = new[]
            {
                "Broadswords, heavy greataxes, runic staffs, and catalyst wands—we supply the finest arms in Aethelburg.",
                "A summoner who knows how to parry and roll will live twice as long as one who only relies on brute force.",
                "Take a look around the bazaar! There is no shortage of monsters needing slaying."
            };
        }
        // 4. Bards & Minstrels
        else if (goName.Contains("Bard_") || goName.Contains("Singer_") || goName.Contains("Lyrist_") || goName.Contains("Lutist_") || goName.Contains("Flutist_"))
        {
            string bName = goName.Replace("Bard_", "").Replace("Lyrist_", "").Replace("Singer_", "").Replace("Lutist_", "").Replace("Flutist_", "");
            inter.npcName = $"Minstrel {bName}";
            inter.npcRole = "Spire Troubadour";
            inter.nameplateColor = new Color(0.98f, 0.85f, 0.25f);
            inter.dialogueLines = new[]
            {
                "♪ O hear the song of the Hundredfold Spire, where crystal skies meet stellar fire... ♪",
                "Music has the power to soothe the corrupted spirits wandering the outer mistwood.",
                "If you listen closely to the breeze in the Acropolis Plaza, you can hear the resonant frequencies of the ancient architects."
            };
        }
        // 5. Scholars & Arcane Mages
        else if (goName.Contains("Mage_") || goName.Contains("Scholar_") || goName.Contains("Astrologer_"))
        {
            string mName = goName.Contains("Caelen") ? "Caelen" : (goName.Contains("Lyra") ? "Lyra" : "Vael");
            inter.npcName = $"Scholar {mName}";
            inter.npcRole = "Arcane Astrologer";
            inter.nameplateColor = new Color(0.65f, 0.35f, 0.95f);
            inter.dialogueLines = new[]
            {
                "The dimensional rift alignment occurs once every millennium. We are fortunate—and terrified—to witness it.",
                "By drawing runic ink patterns, summoners can materialize elemental manifestations into reality.",
                "Study the boss's attack rhythm carefully. Aethelgard's kinetic charge reveals itself through harmonic magenta emission."
            };
        }
        // 6. Harbor & Seaward Port
        else if (goName.Contains("Port_") || goName.Contains("Harbor_") || goName.Contains("Fisherman_") || goName.Contains("Caravan_"))
        {
            string hName = goName.Contains("Darius") ? "Darius" : (goName.Contains("Eamonn") ? "Eamonn" : (goName.Contains("Finn") ? "Finn" : "Port Voyager"));
            inter.npcName = hName;
            inter.npcRole = "Seaward Voyager";
            inter.nameplateColor = new Color(0.15f, 0.75f, 0.85f);
            inter.dialogueLines = new[]
            {
                "The tide is high today! The seaward channel carries crystalline debris from the sunken archipelagos.",
                "Keep an eye out along the docks—rare shimmering scales and pearls wash ashore after rainstorms.",
                "The grand lighthouse on the outer reef still burns with eternal blue soulfire."
            };
        }
        // 7. Citizens, Tavern Patrons & Town Youths
        else if (goName.Contains("Tavern_Innkeeper_Barkeep"))
        {
            inter.npcName = "Barkeep Cedric";
            inter.npcRole = "Tavern Innkeeper";
            inter.nameplateColor = new Color(0.92f, 0.65f, 0.25f);
            inter.dialogueLines = new[]
            {
                "Welcome to the Wandering Wyvern! Best spiced mead and roast boar in the province.",
                "Pull up a bench! The adventurers in the corner were just recounting their raid on Floor 14.",
                "Looking for a warm bed? The rooms upstairs are always open for heroes of the Spire."
            };
        }
        else if (goName.Contains("Youth_") || goName.Contains("Apprentice_Youth_"))
        {
            string yName = goName.Contains("Pip") ? "Pip" : (goName.Contains("Milo") ? "Milo" : "Nellie");
            inter.npcName = yName;
            inter.npcRole = "Town Youth";
            inter.nameplateColor = new Color(0.45f, 0.92f, 0.45f);
            inter.dialogueLines = new[]
            {
                "Whoa! Are those real enchanted gauntlets? You look like a real master summoner!",
                "When I grow up, I'm going to summon a giant celestial griffin and fly all the way to the top of the Spire!",
                "Did you see the butterflies in the Acropolis Plaza? They love the pink cherry blossom trees!"
            };
        }
        else
        {
            // General Citizens & Nobles
            bool isNoble = goName.Contains("Noble");
            inter.npcName = isNoble ? "Lady Vivienne" : "Town Citizen";
            inter.npcRole = isNoble ? "High Noble Resident" : "Metropolis Citizen";
            inter.nameplateColor = isNoble ? new Color(0.95f, 0.35f, 0.65f) : new Color(0.35f, 0.85f, 0.65f);
            inter.dialogueLines = new[]
            {
                "The city is bustling with energy today! Merchants have set up stalls all along the ring avenues.",
                "It's peaceful here inside the walls, but everyone knows the dimensional incursions are growing stronger.",
                "Thank you for defending our home, summoner. May your blade strike true."
            };
        }
    }

    private static int ConfigureBuildingDoorways()
    {
        int count = 0;

        // Ensure buildings have proper life-sized world scale (100x for 0.01-unit FBX models)
        string[] lifeSizedBuildings = new[]
        {
            "Building_Guildhall_Grand",
            "Building_Inn_Lodge",
            "Building_Bakery_Shop",
            "Building_House_Residential",
            "Building_House_LowDistrict"
        };
        foreach (var bName in lifeSizedBuildings)
        {
            var bObj = GameObject.Find(bName);
            if (bObj != null && bObj.transform.localScale.x < 10f)
            {
                bObj.transform.localScale = Vector3.one * 100f;
            }
        }

        // 1. Tavern Building -> BuildingInterior
        var tavernBuilding = GameObject.Find("Building_Tavern_Open");
        if (tavernBuilding != null)
        {
            EnsureDoorwayTrigger(tavernBuilding, "Doorway_Tavern_Interior", "BuildingInterior", "Interior_Doorway", "The Wandering Wyvern Tavern");
            count++;
        }

        // 2. Guildhall Building -> GuildHallInterior
        var guildBuilding = GameObject.Find("Building_Guildhall_Grand");
        if (guildBuilding != null)
        {
            EnsureDoorwayTrigger(guildBuilding, "Doorway_GuildHall_Interior", "GuildHallInterior", "Interior_Doorway", "The Grand Summoner's Guildhall");
            count++;
        }

        // 3. Inn Lodge Building -> CampfireHub
        var innBuilding = GameObject.Find("Building_Inn_Lodge");
        if (innBuilding != null)
        {
            EnsureDoorwayTrigger(innBuilding, "Doorway_CampfireHub", "CampfireHub", "Interior_Doorway", "The Adventurers' Campfire Hub");
            count++;
        }

        // 4. Shop Building Exterior -> ShopInterior
        var shopBuilding = GameObject.Find("Shop_Building_Exterior");
        if (shopBuilding != null)
        {
            EnsureDoorwayTrigger(shopBuilding, "Doorway_EntranceTrigger", "ShopInterior", "Interior_Doorway", "The Alchemist & Apothecary Shop");
            count++;
        }

        return count;
    }

    private static void EnsureDoorwayTrigger(GameObject parentBuilding, string triggerName, string targetScene, string targetMarker, string title)
    {
        var existing = parentBuilding.transform.Find(triggerName);
        GameObject triggerObj = existing != null ? existing.gameObject : null;

        if (triggerObj == null)
        {
            triggerObj = new GameObject(triggerName);
            triggerObj.transform.SetParent(parentBuilding.transform, false);
        }

        Vector3 pLossy = parentBuilding.transform.lossyScale;
        float pX = Mathf.Abs(pLossy.x) > 0.0001f ? Mathf.Abs(pLossy.x) : 1f;
        float pY = Mathf.Abs(pLossy.y) > 0.0001f ? Mathf.Abs(pLossy.y) : 1f;
        float pZ = Mathf.Abs(pLossy.z) > 0.0001f ? Mathf.Abs(pLossy.z) : 1f;

        // Position trigger right at doorway threshold (2.5m forward, 1.2m high) normalized for parent scale
        triggerObj.transform.localPosition = new Vector3(0f, 1.2f / pY, 3.2f / pZ);
        triggerObj.transform.localRotation = Quaternion.identity;
        triggerObj.transform.localScale = Vector3.one;

        var col = triggerObj.GetComponent<BoxCollider>();
        if (col == null) col = triggerObj.AddComponent<BoxCollider>();
        col.center = Vector3.zero;
        // 3.2m wide, 2.8m high, 2.5m deep in world space
        col.size = new Vector3(3.2f / pX, 2.8f / pY, 2.5f / pZ);
        col.isTrigger = true;

        var trigger = triggerObj.GetComponent<BuildingEntranceTrigger>();
        if (trigger == null) trigger = triggerObj.AddComponent<BuildingEntranceTrigger>();
        trigger.targetSceneName = targetScene;
        trigger.targetSpawnMarkerId = targetMarker;
        trigger.locationTitle = title;
        trigger.triggerOnTouch = false; // Manual [E] interact prompt
        trigger.interactKey = KeyCode.E;

        // Add a gentle warm doorway lantern light so player can see it easily
        var lightObj = triggerObj.transform.Find("Doorway_GlowLight");
        if (lightObj == null)
        {
            var lGo = new GameObject("Doorway_GlowLight");
            lGo.transform.SetParent(triggerObj.transform, false);
            lGo.transform.localPosition = new Vector3(0f, 1.0f / pY, 0f);
            var l = lGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 5.5f;
            l.intensity = 1.4f;
            l.color = new Color(1.0f, 0.85f, 0.55f);
            l.shadows = LightShadows.None;
        }
    }

    private static int PlaceAestheticNatureAndVFX()
    {
        int placedCount = 0;
        var parentRoot = GameObject.Find("--- BEAUTIFIED SCENIC CITY PARKS ---");
        if (parentRoot == null)
        {
            parentRoot = new GameObject("--- BEAUTIFIED SCENIC CITY PARKS ---");
            parentRoot.transform.position = Vector3.zero;
        }

        var cherryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Pink.prefab");
        var willowGreenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Green.prefab");
        var butterfly1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/Code Related/Butterfly_01.prefab");
        var butterfly2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/Code Related/Butterfly_02.prefab");
        var motesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/Particles.prefab");

        // Positions for Cherry Blossom Trees around Citadel Acropolis & Fountains (Y ~ 80.0m on Tier 4)
        Vector3[] cherryPositions = new[]
        {
            new Vector3(-25f, 80f, -20f),
            new Vector3(25f, 80f, -20f),
            new Vector3(-25f, 80f, 20f),
            new Vector3(25f, 80f, 20f),
            new Vector3(0f, 80f, -45f),
            new Vector3(0f, 80f, 45f),
            new Vector3(-45f, 80f, 0f),
            new Vector3(45f, 80f, 0f)
        };

        if (cherryPrefab != null && parentRoot.transform.Find("Citadel_Cherry_0") == null)
        {
            for (int i = 0; i < cherryPositions.Length; i++)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(cherryPrefab, parentRoot.transform);
                tree.name = $"Citadel_Cherry_{i}";
                tree.transform.position = cherryPositions[i];
                tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.45f);
                placedCount++;
            }
        }

        // Green Willow Trees along Market Terrace Promenades (Y ~ 38.0m on Tier 2)
        Vector3[] willowPositions = new[]
        {
            new Vector3(-210f, 38f, 130f),
            new Vector3(-235f, 38f, 165f),
            new Vector3(-195f, 38f, 155f),
            new Vector3(210f, 38f, -130f),
            new Vector3(235f, 38f, -165f),
            new Vector3(195f, 38f, -155f)
        };

        if (willowGreenPrefab != null && parentRoot.transform.Find("Terrace_Willow_0") == null)
        {
            for (int i = 0; i < willowPositions.Length; i++)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(willowGreenPrefab, parentRoot.transform);
                tree.name = $"Terrace_Willow_{i}";
                tree.transform.position = willowPositions[i];
                tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 1.5f);
                placedCount++;
            }
        }

        // Fluttering Animated Butterflies over flowerbeds in Acropolis Plaza and Market
        Vector3[] butterflyPositions = new[]
        {
            new Vector3(-12f, 81.5f, -15f),
            new Vector3(12f, 81.5f, -15f),
            new Vector3(0f, 81.5f, -25f),
            new Vector3(-218f, 39.5f, 145f),
            new Vector3(-228f, 39.5f, 155f),
            new Vector3(220f, 39.5f, -145f)
        };

        if (parentRoot.transform.Find("Aesthetic_Butterfly_0") == null)
        {
            for (int i = 0; i < butterflyPositions.Length; i++)
            {
                var bPrefab = (i % 2 == 0 && butterfly1Prefab != null) ? butterfly1Prefab : butterfly2Prefab;
                if (bPrefab != null)
                {
                    var bFly = (GameObject)PrefabUtility.InstantiatePrefab(bPrefab, parentRoot.transform);
                    bFly.name = $"Aesthetic_Butterfly_{i}";
                    bFly.transform.position = butterflyPositions[i];
                    bFly.transform.localScale = Vector3.one * 1.5f;
                    placedCount++;
                }
            }
        }

        // Floating Sunbeam Pollen Motes in the Central Courtyard
        if (motesPrefab != null && parentRoot.transform.Find("Sunbeam_Pollen_Motes") == null)
        {
            var motes = (GameObject)PrefabUtility.InstantiatePrefab(motesPrefab, parentRoot.transform);
            motes.name = "Sunbeam_Pollen_Motes";
            motes.transform.position = new Vector3(0f, 81f, -10f);
            var ps = motes.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.65f, 0.6f));
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
                var shape = ps.shape;
                shape.radius = 45f;
            }
            placedCount++;
        }

        return placedCount;
    }
}
