using System.Collections;
using UnityEngine;

namespace BattleSummoner.Customization
{
    public enum AnimeExpression
    {
        Neutral,
        Smile,
        Joy,
        Determined,
        Surprised,
        Blink,
        Speech
    }

    /// <summary>
    /// AnimeFaceOverlayController dynamically controls 2D anime facial expressions
    /// (eyes, blinking, brows, mouth visemes) on the 3D character starter models
    /// without requiring 3D skeletal facial rigs, achieving a polished Genshin / Persona aesthetic.
    /// </summary>
    [ExecuteAlways]
    public class AnimeFaceOverlayController : MonoBehaviour
    {
        [Header("Character Type")]
        public bool isFemale = false;

        [Header("Atlas Texture")]
        public Texture2D faceAtlas;

        [Header("Face Overlay Quads")]
        [SerializeField] private GameObject eyesQuad;
        [SerializeField] private MeshRenderer eyesRenderer;
        [SerializeField] private GameObject mouthQuad;
        [SerializeField] private MeshRenderer mouthRenderer;

        [Header("Active State")]
        [SerializeField] private AnimeExpression currentExpression = AnimeExpression.Neutral;
        public bool enableAutoBlink = true;

        private Material eyesMaterial;
        private Material mouthMaterial;

        // Auto-blink state
        private float nextBlinkTimer = 3.0f;
        private Coroutine blinkCoroutine;
        private Coroutine speechCoroutine;
        private Coroutine expressionResetCoroutine;

        public AnimeExpression CurrentExpression => currentExpression;

        private void Awake()
        {
            InitializeFaceMeshes();
        }

        private void Start()
        {
            ScheduleNextBlink();
        }

        private void OnEnable()
        {
            InitializeFaceMeshes();
        }

        private static Mesh quadMeshInstance;
        public static Mesh GetOrCreateQuadMesh()
        {
            if (quadMeshInstance != null) return quadMeshInstance;
            var mesh = new Mesh { name = "AnimeFace_QuadMesh" };
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f)
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.normals = new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
            mesh.RecalculateBounds();
            quadMeshInstance = mesh;
            return mesh;
        }

        public void InitializeFaceMeshes()
        {
            if (gameObject.name.ToLower().Contains("female") || (transform.parent != null && transform.parent.name.ToLower().Contains("female")))
            {
                isFemale = true;
            }
            else if (gameObject.name.ToLower().Contains("male") && !gameObject.name.ToLower().Contains("female"))
            {
                isFemale = false;
            }

            string expectedAtlasPath = isFemale 
                ? "Assets/Textures/Characters/AnimeFace/AnimeFace_Female_Atlas.png"
                : "Assets/Textures/Characters/AnimeFace/AnimeFace_Male_Atlas.png";

            if (faceAtlas == null || (isFemale && faceAtlas.name.Contains("Male")) || (!isFemale && faceAtlas.name.Contains("Female")))
            {
#if UNITY_EDITOR
                faceAtlas = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(expectedAtlasPath);
#endif
            }

            // Eyes Quad
            Transform eyesTr = transform.Find("FaceOverlay_Eyes");
            if (eyesTr == null)
            {
                eyesQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                eyesQuad.name = "FaceOverlay_Eyes";
                var col = eyesQuad.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                eyesQuad.transform.SetParent(transform, false);
                eyesRenderer = eyesQuad.GetComponent<MeshRenderer>();
                eyesTr = eyesQuad.transform;
            }
            else
            {
                eyesQuad = eyesTr.gameObject;
                eyesRenderer = eyesQuad.GetComponent<MeshRenderer>();
            }
            var mfEyes = eyesTr.GetComponent<MeshFilter>() ?? eyesTr.gameObject.AddComponent<MeshFilter>();
            if (mfEyes.sharedMesh == null) mfEyes.sharedMesh = GetOrCreateQuadMesh();

            // Mouth Quad
            Transform mouthTr = transform.Find("FaceOverlay_Mouth");
            if (mouthTr == null)
            {
                mouthQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mouthQuad.name = "FaceOverlay_Mouth";
                var col = mouthQuad.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                mouthQuad.transform.SetParent(transform, false);
                mouthRenderer = mouthQuad.GetComponent<MeshRenderer>();
                mouthTr = mouthQuad.transform;
            }
            else
            {
                mouthQuad = mouthTr.gameObject;
                mouthRenderer = mouthQuad.GetComponent<MeshRenderer>();
            }
            var mfMouth = mouthTr.GetComponent<MeshFilter>() ?? mouthTr.gameObject.AddComponent<MeshFilter>();
            if (mfMouth.sharedMesh == null) mfMouth.sharedMesh = GetOrCreateQuadMesh();

            // Positions and dimensions calibrated for MaleStarter and femalestarter
            if (isFemale)
            {
                eyesTr.localPosition = new Vector3(0f, 0.805f, 0.0985f);
                eyesTr.localRotation = Quaternion.Euler(-5f, 180f, 0f);
                eyesTr.localScale = new Vector3(0.138f, 0.072f, 1f);

                mouthTr.localPosition = new Vector3(0f, 0.738f, 0.0935f);
                mouthTr.localRotation = Quaternion.Euler(3f, 180f, 0f);
                mouthTr.localScale = new Vector3(0.048f, 0.028f, 1f);
            }
            else
            {
                eyesTr.localPosition = new Vector3(0f, 0.808f, 0.1285f);
                eyesTr.localRotation = Quaternion.Euler(-5f, 180f, 0f);
                eyesTr.localScale = new Vector3(0.138f, 0.072f, 1f);

                mouthTr.localPosition = new Vector3(0f, 0.736f, 0.1185f);
                mouthTr.localRotation = Quaternion.Euler(3f, 180f, 0f);
                mouthTr.localScale = new Vector3(0.048f, 0.028f, 1f);
            }

            // Setup Transparent Material
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Sprites/Default") 
                         ?? Shader.Find("Unlit/Transparent");

            Material CreateMat(string name)
            {
                var mat = new Material(shader) { name = name };
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3100;
                if (faceAtlas != null)
                {
                    mat.mainTexture = faceAtlas;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", faceAtlas);
                }
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                return mat;
            }

            if (eyesMaterial == null || eyesMaterial.mainTexture != faceAtlas) eyesMaterial = CreateMat("Mat_AnimeEyes_Runtime");
            if (mouthMaterial == null || mouthMaterial.mainTexture != faceAtlas) mouthMaterial = CreateMat("Mat_AnimeMouth_Runtime");

            eyesRenderer.sharedMaterial = eyesMaterial;
            mouthRenderer.sharedMaterial = mouthMaterial;

            ApplyExpression(currentExpression);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (enableAutoBlink && currentExpression != AnimeExpression.Blink && currentExpression != AnimeExpression.Joy)
            {
                nextBlinkTimer -= Time.deltaTime;
                if (nextBlinkTimer <= 0f)
                {
                    TriggerBlink();
                    ScheduleNextBlink();
                }
            }
        }

        private void ScheduleNextBlink()
        {
            nextBlinkTimer = Random.Range(2.8f, 4.8f);
        }

        public void TriggerBlink()
        {
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkRoutine());
        }

        private IEnumerator BlinkRoutine()
        {
            SetEyeFrame(EyeFrame.HalfBlink);
            yield return new WaitForSeconds(0.04f);

            SetEyeFrame(EyeFrame.Closed);
            yield return new WaitForSeconds(0.06f);

            SetEyeFrame(EyeFrame.HalfBlink);
            yield return new WaitForSeconds(0.04f);

            ApplyExpressionEyes(currentExpression);

            // 18% chance of immediate secondary flutter blink
            if (Random.value < 0.18f)
            {
                yield return new WaitForSeconds(0.08f);
                SetEyeFrame(EyeFrame.Closed);
                yield return new WaitForSeconds(0.05f);
                ApplyExpressionEyes(currentExpression);
            }
        }

        public void SetExpression(AnimeExpression expr, float autoResetDuration = 0f)
        {
            currentExpression = expr;
            ApplyExpression(expr);

            if (expressionResetCoroutine != null) StopCoroutine(expressionResetCoroutine);
            if (autoResetDuration > 0f && Application.isPlaying)
            {
                expressionResetCoroutine = StartCoroutine(ResetExpressionAfter(autoResetDuration));
            }
        }

        private IEnumerator ResetExpressionAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetExpression(AnimeExpression.Neutral);
        }

        public void StartSpeaking(float duration = 2.0f)
        {
            if (speechCoroutine != null) StopCoroutine(speechCoroutine);
            speechCoroutine = StartCoroutine(SpeechRoutine(duration));
        }

        public void StopSpeaking()
        {
            if (speechCoroutine != null)
            {
                StopCoroutine(speechCoroutine);
                speechCoroutine = null;
            }
            SetMouthFrame(MouthFrame.Neutral);
        }

        private IEnumerator SpeechRoutine(float duration)
        {
            float elapsed = 0f;
            MouthFrame[] frames = new MouthFrame[] { MouthFrame.Smile, MouthFrame.OpenTalk, MouthFrame.GaspO, MouthFrame.Neutral };
            while (elapsed < duration)
            {
                SetMouthFrame(frames[Random.Range(0, frames.Length)]);
                float step = Random.Range(0.09f, 0.15f);
                yield return new WaitForSeconds(step);
                elapsed += step;
            }
            SetMouthFrame(MouthFrame.Neutral);
            speechCoroutine = null;
        }

        private void ApplyExpression(AnimeExpression expr)
        {
            ApplyExpressionEyes(expr);
            ApplyExpressionMouth(expr);
        }

        private void ApplyExpressionEyes(AnimeExpression expr)
        {
            switch (expr)
            {
                case AnimeExpression.Neutral:
                case AnimeExpression.Speech:
                case AnimeExpression.Smile:
                    SetEyeFrame(EyeFrame.Open);
                    break;
                case AnimeExpression.Joy:
                    SetEyeFrame(EyeFrame.Joy);
                    break;
                case AnimeExpression.Determined:
                    SetEyeFrame(EyeFrame.Determined);
                    break;
                case AnimeExpression.Surprised:
                    SetEyeFrame(EyeFrame.Surprised);
                    break;
                case AnimeExpression.Blink:
                    SetEyeFrame(EyeFrame.Closed);
                    break;
            }
        }

        private void ApplyExpressionMouth(AnimeExpression expr)
        {
            switch (expr)
            {
                case AnimeExpression.Neutral:
                    SetMouthFrame(MouthFrame.Neutral);
                    break;
                case AnimeExpression.Smile:
                    SetMouthFrame(MouthFrame.Smile);
                    break;
                case AnimeExpression.Joy:
                    SetMouthFrame(MouthFrame.Smile);
                    break;
                case AnimeExpression.Determined:
                    SetMouthFrame(MouthFrame.Neutral);
                    break;
                case AnimeExpression.Surprised:
                    SetMouthFrame(MouthFrame.GaspO);
                    break;
                case AnimeExpression.Speech:
                    // Handled by speech coroutine
                    break;
            }
        }

        public enum EyeFrame { Open, HalfBlink, Closed, Joy, Determined, Surprised }
        public enum MouthFrame { Neutral, Smile, OpenTalk, GaspO }

        public void SetEyeFrame(EyeFrame frame)
        {
            if (eyesMaterial == null) return;

            // Atlas 4x4 layout:
            // Open: Col 0..1 (X span 0..0.5), Row 3 (Y: 0.75..1.0)
            // HalfBlink: Col 2..3 (X span 0.5..1.0), Row 3 (Y: 0.75..1.0)
            // Closed: Col 0..1, Row 2 (Y: 0.50..0.75)
            // Joy: Col 2..3, Row 2 (Y: 0.50..0.75)
            // Determined: Col 0..1, Row 1 (Y: 0.25..0.50)
            // Surprised: Col 2..3, Row 1 (Y: 0.25..0.50)

            Vector4 st = Vector4.zero;
            switch (frame)
            {
                case EyeFrame.Open:
                    st = new Vector4(0.50f, 0.25f, 0.0f, 0.75f);
                    break;
                case EyeFrame.HalfBlink:
                    st = new Vector4(0.50f, 0.25f, 0.50f, 0.75f);
                    break;
                case EyeFrame.Closed:
                    st = new Vector4(0.50f, 0.25f, 0.0f, 0.50f);
                    break;
                case EyeFrame.Joy:
                    st = new Vector4(0.50f, 0.25f, 0.50f, 0.50f);
                    break;
                case EyeFrame.Determined:
                    st = new Vector4(0.50f, 0.25f, 0.0f, 0.25f);
                    break;
                case EyeFrame.Surprised:
                    st = new Vector4(0.50f, 0.25f, 0.50f, 0.25f);
                    break;
            }

            if (eyesMaterial.HasProperty("_BaseMap_ST")) eyesMaterial.SetVector("_BaseMap_ST", st);
            if (eyesMaterial.HasProperty("_MainTex_ST")) eyesMaterial.SetVector("_MainTex_ST", st);
        }

        public void SetMouthFrame(MouthFrame frame)
        {
            if (mouthMaterial == null) return;

            // Mouths in Row 0 (Y: 0..0.25):
            // Neutral: Col 0 (X: 0..0.25)
            // Smile: Col 1 (X: 0.25..0.50)
            // OpenTalk: Col 2 (X: 0.50..0.75)
            // GaspO: Col 3 (X: 0.75..1.00)

            Vector4 st = Vector4.zero;
            switch (frame)
            {
                case MouthFrame.Neutral:
                    st = new Vector4(0.25f, 0.25f, 0.0f, 0.0f);
                    break;
                case MouthFrame.Smile:
                    st = new Vector4(0.25f, 0.25f, 0.25f, 0.0f);
                    break;
                case MouthFrame.OpenTalk:
                    st = new Vector4(0.25f, 0.25f, 0.50f, 0.0f);
                    break;
                case MouthFrame.GaspO:
                    st = new Vector4(0.25f, 0.25f, 0.75f, 0.0f);
                    break;
            }

            if (mouthMaterial.HasProperty("_BaseMap_ST")) mouthMaterial.SetVector("_BaseMap_ST", st);
            if (mouthMaterial.HasProperty("_MainTex_ST")) mouthMaterial.SetVector("_MainTex_ST", st);
        }
    }
}
