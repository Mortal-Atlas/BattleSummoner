using System.Collections;
using UnityEngine;
using BattleSummoner.Customization;

namespace BattleSummoner.Minigames.Gardening
{
    /// <summary>
    /// WateringCanTool allows the player to hydrate garden plots:
    /// - 3D watering can model with curved handle, spout, and brass sprinkler
    /// - Tilt pour animation with droplet splash effects
    /// - Waters adjacent GardenPlots to accelerate flower and crop growth
    /// </summary>
    public class WateringCanTool : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private GameObject canVisual;
        [SerializeField] private Transform spoutTip;

        [Header("State")]
        public bool isEquipped = true;
        public bool isPouring = false;

        private AnimeFaceOverlayController faceCtrl;
        private Coroutine pourCoroutine;

        private void Awake()
        {
            BuildCanVisual();
            faceCtrl = GetComponentInParent<AnimeFaceOverlayController>() 
                    ?? GetComponentInChildren<AnimeFaceOverlayController>();
        }

        private void BuildCanVisual()
        {
            if (canVisual == null)
            {
                canVisual = new GameObject("WateringCan_Visual");
                canVisual.transform.SetParent(transform, false);

                // Body (Cylinder)
                var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(canVisual.transform, false);
                body.transform.localPosition = new Vector3(0, 0.25f, 0.35f);
                body.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f);

                var mr = body.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.28f, 0.52f, 0.40f); // Sage green metal
                mr.sharedMaterial = mat;

                // Spout (Angled cylinder)
                var spout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(spout.GetComponent<Collider>());
                spout.transform.SetParent(canVisual.transform, false);
                spout.transform.localPosition = new Vector3(0, 0.35f, 0.58f);
                spout.transform.localRotation = Quaternion.Euler(45f, 0, 0);
                spout.transform.localScale = new Vector3(0.06f, 0.24f, 0.06f);
                spout.GetComponent<MeshRenderer>().sharedMaterial = mat;

                // Rosette Sprinkler Tip
                var rosette = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                DestroyImmediate(rosette.GetComponent<Collider>());
                rosette.name = "SpoutTip";
                rosette.transform.SetParent(spout.transform, false);
                rosette.transform.localPosition = new Vector3(0, 1.0f, 0);
                rosette.transform.localScale = new Vector3(2.0f, 0.6f, 2.0f);

                var rmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rmat.color = new Color(0.85f, 0.75f, 0.35f); // Brass
                rosette.GetComponent<MeshRenderer>().sharedMaterial = rmat;
                spoutTip = rosette.transform;

                // Handle (Torus or arched bar)
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(handle.GetComponent<Collider>());
                handle.transform.SetParent(canVisual.transform, false);
                handle.transform.localPosition = new Vector3(0, 0.42f, 0.28f);
                handle.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                handle.transform.localScale = new Vector3(0.035f, 0.18f, 0.035f);
                handle.GetComponent<MeshRenderer>().sharedMaterial = rmat;
            }
        }

        private void Update()
        {
            if (!isEquipped) return;

            // Keyboard shortcut for testing (H key)
            if (Input.GetKeyDown(KeyCode.H))
            {
                UseWateringCan();
            }
        }

        public void UseWateringCan()
        {
            if (isPouring) return;
            if (pourCoroutine != null) StopCoroutine(pourCoroutine);
            pourCoroutine = StartCoroutine(PourRoutine());
        }

        private IEnumerator PourRoutine()
        {
            isPouring = true;

            if (faceCtrl == null)
            {
                faceCtrl = GetComponentInParent<AnimeFaceOverlayController>() 
                        ?? GetComponentInChildren<AnimeFaceOverlayController>();
            }
            if (faceCtrl != null) faceCtrl.SetExpression(AnimeExpression.Smile, autoResetDuration: 1.2f);

            // Tilt forward animation
            Quaternion baseRot = canVisual.transform.localRotation;
            Vector3 basePos = canVisual.transform.localPosition;
            Quaternion tiltRot = Quaternion.Euler(45f, 0, 0);
            Vector3 tiltPos = basePos + Vector3.forward * 0.15f;

            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                float t = elapsed / 0.25f;
                canVisual.transform.localRotation = Quaternion.Slerp(baseRot, tiltRot, t);
                canVisual.transform.localPosition = Vector3.Lerp(basePos, tiltPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Water adjacent garden plots
            Vector3 checkPoint = transform.position + transform.forward * 1.5f;
            var hits = Physics.OverlapSphere(checkPoint, 2.0f);
            int wateredCount = 0;
            foreach (var h in hits)
            {
                var plot = h.GetComponent<GardenPlot>() ?? h.GetComponentInParent<GardenPlot>();
                if (plot != null)
                {
                    plot.WaterSoil();
                    wateredCount++;
                }
            }
            Debug.Log($"[WATERING CAN] Poured water! Watered {wateredCount} garden plot(s).");

            yield return new WaitForSeconds(0.6f);

            // Return to upright
            elapsed = 0f;
            while (elapsed < 0.25f)
            {
                float t = elapsed / 0.25f;
                canVisual.transform.localRotation = Quaternion.Slerp(tiltRot, baseRot, t);
                canVisual.transform.localPosition = Vector3.Lerp(tiltPos, basePos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            canVisual.transform.localRotation = baseRot;
            canVisual.transform.localPosition = basePos;
            isPouring = false;
        }

        public void SetEquipped(bool equipped)
        {
            isEquipped = equipped;
            if (canVisual != null) canVisual.SetActive(equipped);
        }
    }
}
