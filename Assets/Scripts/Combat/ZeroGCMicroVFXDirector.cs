using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// ZeroGCMicroVFXDirector pre-allocates a fixed ring buffer of 32 slash sparks,
    /// 32 elemental bursts, and 32 floating damage numbers.
    /// Strictly eliminates Instantiate/Destroy during high-frequency combat swipes.
    /// Manages 0.045s Hit-Stop frame freeze and directional camera micro-shake.
    /// </summary>
    public class ZeroGCMicroVFXDirector : MonoBehaviour
    {
        public static ZeroGCMicroVFXDirector Instance { get; private set; }

        private const int BufferCapacity = 32;

        [Header("Hit-Stop & Shake Tuning")]
        [SerializeField] private float defaultHitStopSeconds = 0.045f;
        [SerializeField] private float microShakeAmplitude = 0.08f;

        // Ring Buffer Nodes
        private GameObject[] _sparkPool = new GameObject[BufferCapacity];
        private ParticleSystem[] _sparkParticles = new ParticleSystem[BufferCapacity];
        private int _sparkIndex = 0;

        private GameObject[] _burstPool = new GameObject[BufferCapacity];
        private ParticleSystem[] _burstParticles = new ParticleSystem[BufferCapacity];
        private int _burstIndex = 0;

        private GameObject[] _dmgNumberPool = new GameObject[BufferCapacity];
        private TextMeshPro[] _dmgTMPs = new TextMeshPro[BufferCapacity];
        private int _dmgIndex = 0;

        private bool _isFrozen = false;
        private Camera _cachedMainCam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _cachedMainCam = Camera.main;

            InitializeRingBuffers();
        }

        private void InitializeRingBuffers()
        {
            var root = new GameObject("ZeroGC_VFX_RingBufferPool");
            root.transform.SetParent(transform, false);

            // 1. Slash Sparks (32)
            for (int i = 0; i < BufferCapacity; i++)
            {
                var sparkObj = new GameObject($"Spark_Node_{i:D2}");
                sparkObj.transform.SetParent(root.transform, false);
                var ps = sparkObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = 0.25f;
                main.startSpeed = 8f;
                main.startSize = 0.12f;
                main.startColor = new Color(1f, 0.9f, 0.4f, 1f);
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
                sparkObj.SetActive(false);
                _sparkPool[i] = sparkObj;
                _sparkParticles[i] = ps;
            }

            // 2. Elemental Bursts (32)
            for (int i = 0; i < BufferCapacity; i++)
            {
                var burstObj = new GameObject($"Burst_Node_{i:D2}");
                burstObj.transform.SetParent(root.transform, false);
                var ps = burstObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = 0.35f;
                main.startSpeed = 4f;
                main.startSize = 0.22f;
                main.startColor = new Color(0.3f, 0.85f, 1f, 1f);
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
                burstObj.SetActive(false);
                _burstPool[i] = burstObj;
                _burstParticles[i] = ps;
            }

            // 3. Tabular Floating Damage Numbers (32)
            for (int i = 0; i < BufferCapacity; i++)
            {
                var dmgObj = new GameObject($"DmgText_Node_{i:D2}");
                dmgObj.transform.SetParent(root.transform, false);
                var tmp = dmgObj.AddComponent<TextMeshPro>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 2.4f;
                tmp.color = Color.white;
                dmgObj.SetActive(false);
                _dmgNumberPool[i] = dmgObj;
                _dmgTMPs[i] = tmp;
            }
        }

        /// <summary>
        /// Frame Freeze (Hit-Stop) + Directional Camera Micro-Impulse on heavy impacts and parries.
        /// </summary>
        public void TriggerHitStop(float duration = 0.045f, Vector3 impulseDir = default)
        {
            if (_isFrozen) return;
            StartCoroutine(HitStopRoutine(duration <= 0f ? defaultHitStopSeconds : duration, impulseDir));
        }

        private IEnumerator HitStopRoutine(float freezeSeconds, Vector3 dir)
        {
            _isFrozen = true;
            float prevScale = Time.timeScale;
            Time.timeScale = 0.0001f;

            if (_cachedMainCam == null) _cachedMainCam = Camera.main;
            Vector3 camOrig = _cachedMainCam != null ? _cachedMainCam.transform.localPosition : Vector3.zero;

            if (_cachedMainCam != null && dir.sqrMagnitude > 0.01f)
            {
                _cachedMainCam.transform.localPosition += dir.normalized * microShakeAmplitude;
            }

            yield return new WaitForSecondsRealtime(freezeSeconds);

            if (_cachedMainCam != null && dir.sqrMagnitude > 0.01f)
            {
                _cachedMainCam.transform.localPosition = camOrig;
            }

            Time.timeScale = prevScale;
            _isFrozen = false;
        }

        public void PlaySlashSpark(Vector3 worldPos, Vector3 slashDir, Color sparkColor)
        {
            var node = _sparkPool[_sparkIndex];
            var ps = _sparkParticles[_sparkIndex];
            _sparkIndex = (_sparkIndex + 1) % BufferCapacity;

            node.transform.position = worldPos;
            if (slashDir.sqrMagnitude > 0.01f)
            {
                node.transform.rotation = Quaternion.LookRotation(slashDir);
            }

            var main = ps.main;
            main.startColor = sparkColor;

            node.SetActive(true);
            ps.Play();
        }

        public void PlayElementalBurst(Vector3 worldPos, Color burstColor, float radius = 1.0f)
        {
            var node = _burstPool[_burstIndex];
            var ps = _burstParticles[_burstIndex];
            _burstIndex = (_burstIndex + 1) % BufferCapacity;

            node.transform.position = worldPos;
            var main = ps.main;
            main.startColor = burstColor;
            main.startSize = 0.22f * radius;

            node.SetActive(true);
            ps.Play();
        }

        public void SpawnDamageNumber(Vector3 worldPos, float damage, bool isCrit = false)
        {
            var node = _dmgNumberPool[_dmgIndex];
            var tmp = _dmgTMPs[_dmgIndex];
            _dmgIndex = (_dmgIndex + 1) % BufferCapacity;

            node.transform.position = worldPos + Vector3.up * 0.4f;
            if (_cachedMainCam != null)
            {
                node.transform.rotation = _cachedMainCam.transform.rotation;
            }

            if (isCrit)
            {
                tmp.text = $"<color=#F59E0B><b>{Mathf.RoundToInt(damage)}!</b></color>";
                tmp.fontSize = 3.2f;
            }
            else
            {
                tmp.text = $"<color=#F8FAFC>{Mathf.RoundToInt(damage)}</color>";
                tmp.fontSize = 2.4f;
            }

            node.SetActive(true);
            StartCoroutine(FadeDamageNumber(node));
        }

        private IEnumerator FadeDamageNumber(GameObject node)
        {
            float elapsed = 0f;
            Vector3 startPos = node.transform.position;
            while (elapsed < 0.65f)
            {
                elapsed += Time.unscaledDeltaTime;
                node.transform.position = startPos + Vector3.up * (elapsed * 0.8f);
                yield return null;
            }
            node.SetActive(false);
        }
    }
}
