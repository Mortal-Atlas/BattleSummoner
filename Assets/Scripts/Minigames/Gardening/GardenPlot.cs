using System;
using System.Collections;
using UnityEngine;
using BattleSummoner.Customization;

namespace BattleSummoner.Minigames.Gardening
{
    public enum GrowthStage
    {
        EmptyTilled,
        PlantedSeed,
        Sprout,
        Growing,
        MatureHarvestable
    }

    /// <summary>
    /// GardenPlot represents an interactive farming and gardening bed:
    /// - 3D raised wooden garden bed with soil mound
    /// - Dry vs Wet hydrated soil state (watering accelerates growth)
    /// - 5 Visual Growth Stages with 3D sprouts, foliage, and blooming flowers/fruits
    /// - Harvest celebration and item collection
    /// </summary>
    public class GardenPlot : MonoBehaviour
    {
        [Header("Plant Information")]
        public string currentPlantId = "red_rose";
        public GrowthStage stage = GrowthStage.EmptyTilled;
        public bool isWatered = false;

        [Header("Growth Timers")]
        [SerializeField] private float currentGrowthTimer = 0f;
        [SerializeField] private float waterDurationTimer = 0f;

        [Header("3D Visuals")]
        [SerializeField] private MeshRenderer soilRenderer;
        [SerializeField] private Transform plantStageVisualRoot;
        [SerializeField] private GameObject harvestSparkles;

        [System.NonSerialized] private PlantSeedData currentData;
        public PlantSeedData PlantData => PlantDatabase.GetById(currentPlantId);

        private static readonly Color DrySoilColor = new Color(0.48f, 0.35f, 0.22f);
        private static readonly Color WetSoilColor = new Color(0.24f, 0.16f, 0.10f);

        private void Awake()
        {
            BuildPlotVisuals();
            UpdateVisualStage();
        }

        public void BuildPlotVisuals()
        {
            if (soilRenderer != null || transform.Find("SoilMound") != null) return;

            // Raised Wooden Border
            var bedBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(bedBorder.GetComponent<Collider>());
            bedBorder.name = "BedBorder";
            bedBorder.transform.SetParent(transform, false);
            bedBorder.transform.localPosition = new Vector3(0, 0.10f, 0);
            bedBorder.transform.localScale = new Vector3(1.6f, 0.22f, 1.6f);

            var bmr = bedBorder.GetComponent<MeshRenderer>();
            var bmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bmat.color = new Color(0.38f, 0.24f, 0.14f);
            bmat.SetColor("_BaseColor", new Color(0.38f, 0.24f, 0.14f));
            bmr.sharedMaterial = bmat;

            // Soil Mound
            var soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "SoilMound";
            soil.transform.SetParent(transform, false);
            soil.transform.localPosition = new Vector3(0, 0.15f, 0);
            soil.transform.localScale = new Vector3(1.4f, 0.18f, 1.4f);

            soilRenderer = soil.GetComponent<MeshRenderer>();
            var smat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            smat.color = DrySoilColor;
            smat.SetColor("_BaseColor", DrySoilColor);
            soilRenderer.sharedMaterial = smat;

            // Box Collider for Interaction
            var col = soil.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.2f, 2.0f, 1.2f);

            // Plant Stage Root
            var plantRoot = new GameObject("PlantStageVisuals");
            plantRoot.transform.SetParent(transform, false);
            plantRoot.transform.localPosition = new Vector3(0, 0.25f, 0);
            plantStageVisualRoot = plantRoot.transform;

            // Harvest Sparkle light
            var sparkleGO = new GameObject("HarvestSparkleGlow");
            sparkleGO.transform.SetParent(transform, false);
            sparkleGO.transform.localPosition = new Vector3(0, 0.6f, 0);
            var sl = sparkleGO.AddComponent<Light>();
            sl.type = LightType.Point;
            sl.color = new Color(1.0f, 0.9f, 0.4f);
            sl.range = 2.5f;
            sl.intensity = 1.5f;
            harvestSparkles = sparkleGO;
            harvestSparkles.SetActive(false);
        }

        private void Update()
        {
            if (stage != GrowthStage.EmptyTilled && stage != GrowthStage.MatureHarvestable)
            {
                // Wet soil grows 2.2x faster!
                float growthRate = isWatered ? 2.2f : 1.0f;
                currentGrowthTimer += Time.deltaTime * growthRate;

                float maxSeconds = PlantData.baseGrowthSeconds;
                float progress = currentGrowthTimer / maxSeconds;

                if (progress >= 1.0f)
                {
                    AdvanceToStage(GrowthStage.MatureHarvestable);
                }
                else if (progress >= 0.60f && stage < GrowthStage.Growing)
                {
                    AdvanceToStage(GrowthStage.Growing);
                }
                else if (progress >= 0.25f && stage < GrowthStage.Sprout)
                {
                    AdvanceToStage(GrowthStage.Sprout);
                }
            }

            // Hydration drying timer
            if (isWatered)
            {
                waterDurationTimer -= Time.deltaTime;
                if (waterDurationTimer <= 0f)
                {
                    isWatered = false;
                    soilRenderer.sharedMaterial.color = DrySoilColor;
                }
            }

            // Player Proximity Interaction (E key to harvest)
            if (Input.GetKeyDown(KeyCode.E) && stage == GrowthStage.MatureHarvestable)
            {
                var hero = GameObject.Find("Player_Hero");
                if (hero != null && Vector3.Distance(hero.transform.position, transform.position) < 2.4f)
                {
                    var faceCtrl = hero.GetComponentInChildren<AnimeFaceOverlayController>();
                    Harvest(faceCtrl);
                }
            }
        }

        public void PlantSeed(string plantId)
        {
            if (stage != GrowthStage.EmptyTilled) return;

            currentPlantId = plantId;
            currentData = PlantDatabase.GetById(plantId);
            currentGrowthTimer = 0f;
            AdvanceToStage(GrowthStage.PlantedSeed);
            Debug.Log($"[GARDENING] Planted {currentData.plantName} in garden bed!");
        }

        public void WaterSoil()
        {
            isWatered = true;
            waterDurationTimer = 45.0f; // stays watered for 45s
            if (soilRenderer != null)
            {
                soilRenderer.sharedMaterial.color = WetSoilColor;
            }
            Debug.Log($"[GARDENING] Watered garden plot! Growth speed boosted.");
        }

        public void Harvest(AnimeFaceOverlayController faceCtrl)
        {
            if (stage != GrowthStage.MatureHarvestable) return;

            var data = PlantData;
            Debug.Log($"[GARDENING] ★ Harvested {data.harvestYieldCount}x {data.harvestItemName}! (+{data.marketValue} G)");

            if (faceCtrl != null)
            {
                faceCtrl.SetExpression(AnimeExpression.Joy, autoResetDuration: 2.5f);
            }

            // Return to empty or regrowth
            if (data.category == PlantCategory.Flower)
            {
                // Regrow stems
                AdvanceToStage(GrowthStage.Growing);
                currentGrowthTimer = data.baseGrowthSeconds * 0.5f;
            }
            else
            {
                // Harvest produce completely
                AdvanceToStage(GrowthStage.EmptyTilled);
                currentGrowthTimer = 0f;
            }
        }

        private void AdvanceToStage(GrowthStage nextStage)
        {
            stage = nextStage;
            UpdateVisualStage();
        }

        public void UpdateVisualStage()
        {
            if (plantStageVisualRoot == null)
            {
                var pRoot = transform.Find("PlantStageVisuals");
                if (pRoot != null) plantStageVisualRoot = pRoot;
                else return;
            }

            // Clear old visuals
            foreach (Transform child in plantStageVisualRoot)
            {
                Destroy(child.gameObject);
            }

            harvestSparkles.SetActive(stage == GrowthStage.MatureHarvestable);
            var data = PlantData;

            switch (stage)
            {
                case GrowthStage.EmptyTilled:
                    break;

                case GrowthStage.PlantedSeed:
                    // Small dirt mound + marker
                    var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroyImmediate(mound.GetComponent<Collider>());
                    mound.transform.SetParent(plantStageVisualRoot, false);
                    mound.transform.localScale = new Vector3(0.2f, 0.08f, 0.2f);
                    var mmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mmat.color = new Color(0.40f, 0.28f, 0.16f);
                    mmat.SetColor("_BaseColor", new Color(0.40f, 0.28f, 0.16f));
                    mound.GetComponent<MeshRenderer>().sharedMaterial = mmat;
                    break;

                case GrowthStage.Sprout:
                    // Two tiny green cotyledons
                    CreateLeaf(plantStageVisualRoot, new Vector3(-0.08f, 0.05f, 0), Quaternion.Euler(25, 0, -35), 0.10f, data.foliageColor);
                    CreateLeaf(plantStageVisualRoot, new Vector3(0.08f, 0.05f, 0), Quaternion.Euler(25, 0, 35), 0.10f, data.foliageColor);
                    break;

                case GrowthStage.Growing:
                    // Stem + fuller foliage
                    var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    DestroyImmediate(stem.GetComponent<Collider>());
                    stem.transform.SetParent(plantStageVisualRoot, false);
                    stem.transform.localPosition = new Vector3(0, 0.18f, 0);
                    stem.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
                    var smat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    smat.color = data.foliageColor;
                    smat.SetColor("_BaseColor", data.foliageColor);
                    stem.GetComponent<MeshRenderer>().sharedMaterial = smat;

                    CreateLeaf(plantStageVisualRoot, new Vector3(-0.16f, 0.22f, 0), Quaternion.Euler(15, 45, -30), 0.18f, data.foliageColor);
                    CreateLeaf(plantStageVisualRoot, new Vector3(0.16f, 0.22f, 0), Quaternion.Euler(15, -45, 30), 0.18f, data.foliageColor);
                    CreateLeaf(plantStageVisualRoot, new Vector3(0, 0.32f, 0.16f), Quaternion.Euler(30, 0, 0), 0.18f, data.foliageColor);
                    break;

                case GrowthStage.MatureHarvestable:
                    // Full mature lush bush with colorful blooms/fruits!
                    var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroyImmediate(bush.GetComponent<Collider>());
                    bush.transform.SetParent(plantStageVisualRoot, false);
                    bush.transform.localPosition = new Vector3(0, 0.28f, 0);
                    bush.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);
                    var bmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    bmat.color = data.foliageColor;
                    bmat.SetColor("_BaseColor", data.foliageColor);
                    bush.GetComponent<MeshRenderer>().sharedMaterial = bmat;

                    // 3 Blooms or Ripe Fruits on top
                    CreateBloomOrFruit(plantStageVisualRoot, new Vector3(0, 0.52f, 0), data);
                    CreateBloomOrFruit(plantStageVisualRoot, new Vector3(-0.20f, 0.44f, 0.15f), data);
                    CreateBloomOrFruit(plantStageVisualRoot, new Vector3(0.20f, 0.44f, -0.15f), data);
                    break;
            }
        }

        private void CreateLeaf(Transform parent, Vector3 localPos, Quaternion localRot, float scale, Color color)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(leaf.GetComponent<Collider>());
            leaf.transform.SetParent(parent, false);
            leaf.transform.localPosition = localPos;
            leaf.transform.localRotation = localRot;
            leaf.transform.localScale = new Vector3(scale * 0.4f, scale * 0.1f, scale);

            var mr = leaf.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetColor("_BaseColor", color);
            mr.sharedMaterial = mat;
        }

        private void CreateBloomOrFruit(Transform parent, Vector3 localPos, PlantSeedData data)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;

            if (data.category == PlantCategory.ProduceCrop)
            {
                // Round ripe fruit
                obj.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            }
            else
            {
                // Flared flower petal crown
                obj.transform.localScale = new Vector3(0.26f, 0.14f, 0.26f);
            }

            var mr = obj.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = data.bloomColor;
            mat.SetColor("_BaseColor", data.bloomColor);
            mr.sharedMaterial = mat;
        }
    }
}
