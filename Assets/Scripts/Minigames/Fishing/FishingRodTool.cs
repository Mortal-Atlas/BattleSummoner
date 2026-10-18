using System.Collections;
using UnityEngine;
using BattleSummoner.Customization;

namespace BattleSummoner.Minigames.Fishing
{
    /// <summary>
    /// FishingRodTool controls the 3D fishing rod equipped by the player:
    /// - 3D rod geometry with line guides and reel
    /// - Procedural fishing line rendering via LineRenderer
    /// - Parabolic cast trajectory into nearby water bodies
    /// - Reel in / catch logic with facial reaction synergy
    /// </summary>
    public class FishingRodTool : MonoBehaviour
    {
        public static FishingBobber ActiveBobber { get; private set; }

        [Header("Rod Components")]
        [SerializeField] private Transform rodTip;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private GameObject rodVisual;

        [Header("State")]
        public bool isEquipped = true;
        public bool isCast = false;

        private AnimeFaceOverlayController faceCtrl;
        private FishingBobber currentBobber;

        private void Awake()
        {
            BuildRodVisual();
            faceCtrl = GetComponentInParent<AnimeFaceOverlayController>() 
                    ?? GetComponentInChildren<AnimeFaceOverlayController>();
        }

        private void BuildRodVisual()
        {
            if (rodVisual == null)
            {
                rodVisual = new GameObject("FishingRod_Visual");
                rodVisual.transform.SetParent(transform, false);

                // Rod shaft (tapered cylinder)
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(shaft.GetComponent<Collider>());
                shaft.transform.SetParent(rodVisual.transform, false);
                shaft.transform.localPosition = new Vector3(0, 1.0f, 0.4f);
                shaft.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                shaft.transform.localScale = new Vector3(0.04f, 1.2f, 0.04f);

                var mr = shaft.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.35f, 0.22f, 0.12f); // Wood/Bamboo finish
                mr.sharedMaterial = mat;

                // Reel
                var reel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(reel.GetComponent<Collider>());
                reel.transform.SetParent(rodVisual.transform, false);
                reel.transform.localPosition = new Vector3(0, 0.25f, 0.15f);
                reel.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                reel.transform.localScale = new Vector3(0.10f, 0.04f, 0.10f);
                var rmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rmat.color = new Color(0.85f, 0.75f, 0.35f); // Brass reel
                reel.GetComponent<MeshRenderer>().sharedMaterial = rmat;

                // Tip Anchor
                var tipGO = new GameObject("RodTip");
                tipGO.transform.SetParent(rodVisual.transform, false);
                tipGO.transform.localPosition = new Vector3(0, 1.85f, 1.25f);
                rodTip = tipGO.transform;

                // Line Renderer
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.015f;
                lineRenderer.endWidth = 0.015f;
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                var lmat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                lmat.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f, 0.85f));
                lineRenderer.sharedMaterial = lmat;
                lineRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (!isEquipped) return;

            // Connect fishing line from tip to bobber
            if (isCast && currentBobber != null && rodTip != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, rodTip.position);
                lineRenderer.SetPosition(1, currentBobber.transform.position + Vector3.up * 0.12f);
            }
            else if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }

            // Keyboard shortcut for testing (F key or Left Click / Action button)
            if (Input.GetKeyDown(KeyCode.F))
            {
                UseFishingRod();
            }
        }

        public void UseFishingRod()
        {
            if (FishingCatchCelebrationUI.Instance != null && FishingCatchCelebrationUI.Instance.IsCelebrating)
            {
                FishingCatchCelebrationUI.Instance.DismissCelebration();
                return;
            }

            if (!isCast)
            {
                CastLine();
            }
            else
            {
                ReelLine();
            }
        }

        public void CastLine()
        {
            if (isCast) return;

            if (faceCtrl == null)
            {
                faceCtrl = GetComponentInParent<AnimeFaceOverlayController>() 
                        ?? GetComponentInChildren<AnimeFaceOverlayController>();
            }
            if (faceCtrl != null) faceCtrl.SetExpression(AnimeExpression.Determined, autoResetDuration: 1.5f);

            // Cast trajectory forward into water
            Vector3 castOrigin = rodTip != null ? rodTip.position : transform.position;
            Vector3 targetWaterPos = transform.position + (transform.forward * 6.5f);
            targetWaterPos.y = 0.05f; // Water level in OverworldMap

            // Spawn Bobber
            var bobberGO = new GameObject("ActiveBobber");
            currentBobber = bobberGO.AddComponent<FishingBobber>();
            currentBobber.LandInWater(targetWaterPos);
            ActiveBobber = currentBobber;
            isCast = true;

            Debug.Log("[FISHING] Line cast! Waiting for fish shadows...");
        }

        public void ReelLine()
        {
            if (!isCast) return;

            if (currentBobber != null && currentBobber.AssignedFish != null)
            {
                var fish = currentBobber.AssignedFish;
                if (fish.State == FishAIState.Biting)
                {
                    // SUCCESSFUL CATCH!
                    fish.HookFish();
                    var species = FishDatabase.GetRandomFish(fish.shadowSize);
                    float length = species.GenerateRandomLength();

                    if (FishingCatchCelebrationUI.Instance != null)
                    {
                        FishingCatchCelebrationUI.Instance.ShowCatchCelebration(species, length, faceCtrl);
                    }
                    Debug.Log($"[FISHING] Caught a {species.speciesName}! ({length:F1} cm)");
                    Destroy(fish.gameObject);
                }
                else
                {
                    // Hooked too early!
                    fish.StartleAndFlee();
                    if (faceCtrl != null) faceCtrl.SetExpression(AnimeExpression.Surprised, autoResetDuration: 1.5f);
                    Debug.Log("[FISHING] Hooked too early! The fish fled!");
                }
            }
            else
            {
                Debug.Log("[FISHING] Reeled in empty line.");
            }

            // Cleanup bobber
            if (currentBobber != null)
            {
                Destroy(currentBobber.gameObject);
                currentBobber = null;
            }
            ActiveBobber = null;
            isCast = false;
        }

        public void SetEquipped(bool equipped)
        {
            isEquipped = equipped;
            if (rodVisual != null) rodVisual.SetActive(equipped);
            if (!equipped && isCast)
            {
                ReelLine();
            }
        }
    }
}
