using System.Collections;
using UnityEngine;
using BattleSummoner.Customization;

namespace BattleSummoner.Minigames.Insects
{
    /// <summary>
    /// BugNetTool manages the 3D bug net equipped by the player:
    /// - 3D wooden pole, metal hoop, and mesh cloth net bag
    /// - Normal swoop swing or hold-to-stalk stealth stance
    /// - Overlap sweep detection to catch butterflies, beetles, and other critters
    /// </summary>
    public class BugNetTool : MonoBehaviour
    {
        [Header("Net Components")]
        [SerializeField] private GameObject netVisual;
        [SerializeField] private Transform netHoop;

        [Header("State")]
        public bool isEquipped = true;
        public bool isStalking = false;
        public bool isSwinging = false;

        private AnimeFaceOverlayController faceCtrl;
        private Coroutine swingCoroutine;

        private void Awake()
        {
            BuildNetVisual();
            faceCtrl = GetComponentInParent<AnimeFaceOverlayController>() 
                    ?? GetComponentInChildren<AnimeFaceOverlayController>();
        }

        private void BuildNetVisual()
        {
            if (netVisual == null)
            {
                netVisual = new GameObject("BugNet_Visual");
                netVisual.transform.SetParent(transform, false);

                // Handle (Cylinder)
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(handle.GetComponent<Collider>());
                handle.transform.SetParent(netVisual.transform, false);
                handle.transform.localPosition = new Vector3(0, 0.6f, 0.3f);
                handle.transform.localRotation = Quaternion.Euler(55f, 0, 0);
                handle.transform.localScale = new Vector3(0.035f, 0.8f, 0.035f);

                var hmr = handle.GetComponent<MeshRenderer>();
                var hmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                hmat.color = new Color(0.6f, 0.42f, 0.25f);
                hmr.sharedMaterial = hmat;

                // Hoop Rim (Ring)
                var hoop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(hoop.GetComponent<Collider>());
                hoop.name = "NetHoop";
                hoop.transform.SetParent(netVisual.transform, false);
                hoop.transform.localPosition = new Vector3(0, 1.35f, 0.95f);
                hoop.transform.localRotation = Quaternion.Euler(55f, 0, 0);
                hoop.transform.localScale = new Vector3(0.55f, 0.02f, 0.55f);

                var rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rimMat.color = new Color(0.85f, 0.85f, 0.88f);
                hoop.GetComponent<MeshRenderer>().sharedMaterial = rimMat;
                netHoop = hoop.transform;

                // Net Cloth Bag
                var bag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                DestroyImmediate(bag.GetComponent<Collider>());
                bag.transform.SetParent(hoop.transform, false);
                bag.transform.localPosition = new Vector3(0, -0.4f, 0);
                bag.transform.localScale = new Vector3(0.85f, 1.2f, 0.85f);

                var bmat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                bmat.SetColor("_BaseColor", new Color(0.92f, 0.95f, 1f, 0.55f));
                bmat.SetFloat("_Surface", 1);
                bag.GetComponent<MeshRenderer>().sharedMaterial = bmat;
            }
        }

        private void Update()
        {
            if (!isEquipped) return;

            // Keyboard shortcut for testing (G key)
            if (Input.GetKeyDown(KeyCode.G))
            {
                StartStalking();
            }
            if (Input.GetKeyUp(KeyCode.G))
            {
                if (isStalking)
                {
                    StopStalkingAndSwoop();
                }
                else
                {
                    SwingNet();
                }
            }
        }

        public void StartStalking()
        {
            if (isSwinging) return;
            isStalking = true;

            if (faceCtrl == null)
            {
                faceCtrl = GetComponentInParent<AnimeFaceOverlayController>() 
                        ?? GetComponentInChildren<AnimeFaceOverlayController>();
            }
            if (faceCtrl != null) faceCtrl.SetExpression(AnimeExpression.Determined);

            // Raise net overhead
            if (netVisual != null)
            {
                netVisual.transform.localRotation = Quaternion.Euler(-30f, 0, 0);
                netVisual.transform.localPosition = new Vector3(0, 0.3f, -0.2f);
            }
            Debug.Log("[BUG NET] Stalking mode engaged... creeping up silently!");
        }

        public void StopStalkingAndSwoop()
        {
            isStalking = false;
            SwingNet();
        }

        public void SwingNet()
        {
            if (isSwinging) return;
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            swingCoroutine = StartCoroutine(SwingRoutine());
        }

        private IEnumerator SwingRoutine()
        {
            isSwinging = true;

            if (faceCtrl == null)
            {
                faceCtrl = GetComponentInParent<AnimeFaceOverlayController>() 
                        ?? GetComponentInChildren<AnimeFaceOverlayController>();
            }
            if (faceCtrl != null) faceCtrl.SetExpression(AnimeExpression.Determined, autoResetDuration: 1.0f);

            // Fast forward swoop animation
            float elapsed = 0f;
            Quaternion startRot = netVisual.transform.localRotation;
            Vector3 startPos = netVisual.transform.localPosition;

            Quaternion strikeRot = Quaternion.Euler(75f, 0, 0);
            Vector3 strikePos = new Vector3(0, -0.1f, 0.4f);

            while (elapsed < 0.18f)
            {
                float t = elapsed / 0.18f;
                netVisual.transform.localRotation = Quaternion.Slerp(startRot, strikeRot, t);
                netVisual.transform.localPosition = Vector3.Lerp(startPos, strikePos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Catch Detection: Check sphere in front of net hoop
            Vector3 checkPoint = netHoop != null ? netHoop.position : transform.position + transform.forward * 1.5f;
            var hits = Physics.OverlapSphere(checkPoint, 1.2f);
            InsectEntity caughtInsect = null;

            foreach (var h in hits)
            {
                var insect = h.GetComponent<InsectEntity>() ?? h.GetComponentInParent<InsectEntity>();
                if (insect != null && insect.State != InsectState.Caught)
                {
                    caughtInsect = insect;
                    break;
                }
            }

            if (caughtInsect != null)
            {
                var data = caughtInsect.Data;
                caughtInsect.Catch();

                if (InsectCatchCelebrationUI.Instance != null)
                {
                    InsectCatchCelebrationUI.Instance.ShowCatchCelebration(data, faceCtrl);
                }
                Debug.Log($"[BUG NET] Successfully caught a {data.insectName}!");
            }
            else
            {
                Debug.Log("[BUG NET] Swooped, but caught only air!");
            }

            // Return to neutral ready stance
            yield return new WaitForSeconds(0.25f);
            elapsed = 0f;
            while (elapsed < 0.25f)
            {
                float t = elapsed / 0.25f;
                netVisual.transform.localRotation = Quaternion.Slerp(strikeRot, Quaternion.identity, t);
                netVisual.transform.localPosition = Vector3.Lerp(strikePos, Vector3.zero, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            netVisual.transform.localRotation = Quaternion.identity;
            netVisual.transform.localPosition = Vector3.zero;
            isSwinging = false;
        }

        public void SetEquipped(bool equipped)
        {
            isEquipped = equipped;
            if (netVisual != null) netVisual.SetActive(equipped);
        }
    }
}
