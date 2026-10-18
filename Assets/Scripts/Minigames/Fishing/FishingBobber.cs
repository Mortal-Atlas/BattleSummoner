using System.Collections;
using UnityEngine;

namespace BattleSummoner.Minigames.Fishing
{
    /// <summary>
    /// FishingBobber represents the floating fishing float:
    /// - Follows physics cast trajectory into water
    /// - Floats with natural sinus bobbing
    /// - Reacts to fish nibbles with subtle dips
    /// - Reacts to fish bites with dramatic underwater plunge and splashes
    /// </summary>
    public class FishingBobber : MonoBehaviour
    {
        public bool IsInWater { get; private set; }
        public FishShadowEntity AssignedFish { get; set; }

        private float waterLevelY = 0f;
        private float bobTimer = 0f;
        private Vector3 baseWaterPos;
        private bool isBiting = false;

        private GameObject visualBall;
        private ParticleSystem splashParticles;

        private void Awake()
        {
            BuildBobberVisual();
        }

        private void BuildBobberVisual()
        {
            if (visualBall == null)
            {
                visualBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visualBall.name = "BobberBall";
                var col = visualBall.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                visualBall.transform.SetParent(transform, false);
                visualBall.transform.localScale = new Vector3(0.22f, 0.26f, 0.22f);

                // Two-tone red and white classic Animal Crossing bobber material
                var mr = visualBall.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.95f, 0.25f, 0.20f); // Bright red
                mr.sharedMaterial = mat;

                // Little white stem on top
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var scol = stem.GetComponent<Collider>();
                if (scol != null) DestroyImmediate(scol);
                stem.transform.SetParent(visualBall.transform, false);
                stem.transform.localPosition = new Vector3(0, 0.6f, 0);
                stem.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
                var smr = stem.GetComponent<MeshRenderer>();
                var smat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                smat.color = Color.white;
                smr.sharedMaterial = smat;
            }
        }

        public void LandInWater(Vector3 landPoint)
        {
            waterLevelY = landPoint.y;
            baseWaterPos = landPoint;
            transform.position = landPoint;
            IsInWater = true;
            isBiting = false;

            CreateSplashEffect(0.5f);
        }

        private void Update()
        {
            if (!IsInWater) return;

            bobTimer += Time.deltaTime * 3.5f;

            if (!isBiting)
            {
                // Gentle floating bob
                float bobOffset = Mathf.Sin(bobTimer) * 0.035f;
                transform.position = new Vector3(baseWaterPos.x, waterLevelY + bobOffset, baseWaterPos.z);
            }
        }

        public void TriggerNibbleRipple()
        {
            StartCoroutine(NibbleDipRoutine());
        }

        private IEnumerator NibbleDipRoutine()
        {
            // Bobber dips 0.08m
            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                transform.position = new Vector3(baseWaterPos.x, waterLevelY - 0.08f, baseWaterPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            CreateSplashEffect(0.3f);
        }

        public void TriggerBiteSplash()
        {
            isBiting = true;
            transform.position = new Vector3(baseWaterPos.x, waterLevelY - 0.25f, baseWaterPos.z);
            CreateSplashEffect(1.2f);
        }

        public void TriggerFleeRipple()
        {
            isBiting = false;
            CreateSplashEffect(0.8f);
        }

        private void CreateSplashEffect(float scale)
        {
            // Create a quick visual water ring / splash puff
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(ring.GetComponent<Collider>());
            ring.name = "WaterRipple";
            ring.transform.position = new Vector3(transform.position.x, waterLevelY + 0.02f, transform.position.z);
            ring.transform.localScale = new Vector3(0.3f * scale, 0.01f, 0.3f * scale);

            var mr = ring.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            mat.SetColor("_BaseColor", new Color(0.8f, 0.95f, 1f, 0.7f));
            mat.SetFloat("_Surface", 1);
            mr.sharedMaterial = mat;

            StartCoroutine(ExpandAndFadeRipple(ring));
        }

        private IEnumerator ExpandAndFadeRipple(GameObject ring)
        {
            float elapsed = 0f;
            Vector3 startScale = ring.transform.localScale;
            var mr = ring.GetComponent<MeshRenderer>();
            Color col = mr.sharedMaterial.GetColor("_BaseColor");

            while (elapsed < 0.6f)
            {
                float t = elapsed / 0.6f;
                ring.transform.localScale = Vector3.Lerp(startScale, startScale * 4.0f, t);
                col.a = Mathf.Lerp(0.7f, 0f, t);
                mr.sharedMaterial.SetColor("_BaseColor", col);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(ring);
        }
    }
}
