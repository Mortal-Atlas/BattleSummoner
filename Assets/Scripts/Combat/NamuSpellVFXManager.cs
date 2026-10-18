using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// NamuSpellVFXManager loads, pools, and instantiates visual effects from the
    /// newly downloaded NamuFX StylizedWaterEffects package:
    /// - Water_Beam.prefab (continuous torrent)
    /// - Water_Ball_Explosion.prefab (water sphere detonation)
    /// - Water_Impact.prefab (ground splash / impact)
    /// - Hit_01.prefab / Hit_02.prefab (stylized combat impact sparks)
    /// - Bubbles_Burst.prefab
    /// </summary>
    public class NamuSpellVFXManager : MonoBehaviour
    {
        private static NamuSpellVFXManager _instance;
        public static NamuSpellVFXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<NamuSpellVFXManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("NamuSpellVFXManager");
                        _instance = go.AddComponent<NamuSpellVFXManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("NamuFX Prefabs")]
        [SerializeField] private GameObject waterBeamPrefab;
        [SerializeField] private GameObject waterBallExplosionPrefab;
        [SerializeField] private GameObject waterImpactPrefab;
        [SerializeField] private GameObject hit01Prefab;
        [SerializeField] private GameObject hit02Prefab;
        [SerializeField] private GameObject bubblesBurstPrefab;

        private GameObject activeBeamInstance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            LoadPrefabs();
        }

        private void LoadPrefabs()
        {
#if UNITY_EDITOR
            if (waterBeamPrefab == null)
                waterBeamPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Beam.prefab");
            if (waterBallExplosionPrefab == null)
                waterBallExplosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Ball_Explosion.prefab");
            if (waterImpactPrefab == null)
                waterImpactPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Impact.prefab");
            if (hit01Prefab == null)
                hit01Prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Hit_01.prefab");
            if (hit02Prefab == null)
                hit02Prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Hit_02.prefab");
            if (bubblesBurstPrefab == null)
                bubblesBurstPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Bubbles_Burst.prefab");
#endif
        }

        /// <summary>
        /// Updates or activates the continuous water beam stream pointing from origin toward target.
        /// </summary>
        public void UpdateContinuousWaterBeam(Vector3 origin, Vector3 target)
        {
            LoadPrefabs();
            if (waterBeamPrefab == null) return;

            if (activeBeamInstance == null)
            {
                activeBeamInstance = Instantiate(waterBeamPrefab, origin, Quaternion.identity);
                activeBeamInstance.name = "ActiveNamuWaterBeam";
            }

            activeBeamInstance.SetActive(true);
            activeBeamInstance.transform.position = origin;
            Vector3 dir = (target - origin).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                activeBeamInstance.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            float dist = Vector3.Distance(origin, target);
            activeBeamInstance.transform.localScale = new Vector3(1f, 1f, Mathf.Clamp(dist / 8f, 0.4f, 3.5f));
        }

        public void StopContinuousWaterBeam()
        {
            if (activeBeamInstance != null)
            {
                activeBeamInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Spawns a stylized water ball explosion at the given position.
        /// </summary>
        public GameObject SpawnWaterBallExplosion(Vector3 position, float scale = 1.0f)
        {
            LoadPrefabs();
            if (waterBallExplosionPrefab == null) return null;

            var obj = Instantiate(waterBallExplosionPrefab, position, Quaternion.identity);
            obj.transform.localScale = Vector3.one * scale;
            Destroy(obj, 3.5f);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("malformed_wheeze", 0.95f);
            }

            return obj;
        }

        /// <summary>
        /// Spawns a water impact / ground splash.
        /// </summary>
        public GameObject SpawnWaterImpact(Vector3 position)
        {
            LoadPrefabs();
            if (waterImpactPrefab == null) return null;

            var obj = Instantiate(waterImpactPrefab, position, Quaternion.identity);
            Destroy(obj, 2.5f);
            return obj;
        }

        /// <summary>
        /// Spawns high-impact stylized combat sparks (Hit_01 or Hit_02) on an enemy.
        /// </summary>
        public GameObject SpawnHitVFX(Vector3 position, Vector3 normal, bool heavy = false)
        {
            LoadPrefabs();
            GameObject prefab = heavy ? (hit02Prefab ?? hit01Prefab) : (hit01Prefab ?? hit02Prefab);
            if (prefab == null) return null;

            Quaternion rot = normal.sqrMagnitude > 0.01f ? Quaternion.LookRotation(normal) : Quaternion.identity;
            var obj = Instantiate(prefab, position, rot);
            Destroy(obj, 2.0f);
            return obj;
        }

        /// <summary>
        /// Spawns bubble burst particles.
        /// </summary>
        public GameObject SpawnBubbleBurst(Vector3 position)
        {
            LoadPrefabs();
            if (bubblesBurstPrefab == null) return null;

            var obj = Instantiate(bubblesBurstPrefab, position, Quaternion.identity);
            Destroy(obj, 2.0f);
            return obj;
        }
    }
}
