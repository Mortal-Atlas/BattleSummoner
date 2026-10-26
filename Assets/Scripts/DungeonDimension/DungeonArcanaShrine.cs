using System.Collections;
using UnityEngine;
using BattleSummoner.Combat;

namespace BattleSummoner.DungeonDimension
{
    public enum MajorArcanaType
    {
        TheFool,
        TheMagician,
        ThePriestess,
        TheEmpress,
        TheChariot,
        TheTower,
        TheSun
    }

    public class DungeonArcanaShrine : MonoBehaviour
    {
        [Header("Shrine Elements")]
        [SerializeField] private Transform hoveringCrystalTransform;
        [SerializeField] private Light shrineAuraLight;
        [SerializeField] private float interactRadius = 3.5f;

        private bool hasBeenUsed = false;
        private bool isPlayerInRange = false;
        private Transform playerTransform;
        private GameObject promptCanvasObj;
        private TMPro.TextMeshProUGUI promptTMP;
        private MajorArcanaType rolledArcana;

        private void Start()
        {
            rolledArcana = (MajorArcanaType)Random.Range(0, System.Enum.GetValues(typeof(MajorArcanaType)).Length);
            SetupVisuals();
            SetupPromptUI();
            FindPlayer();
        }

        private void SetupVisuals()
        {
            if (shrineAuraLight != null)
            {
                shrineAuraLight.color = new Color(0.4f, 0.7f, 1.0f);
                shrineAuraLight.intensity = 2.8f;
                shrineAuraLight.range = 10f;
            }
        }

        private void SetupPromptUI()
        {
            GameObject canvasObj = new GameObject("ShrinePromptCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 70f);
            canvasObj.transform.localScale = Vector3.one * 0.009f;

            GameObject textObj = new GameObject("Text_Prompt");
            textObj.transform.SetParent(canvasObj.transform, false);
            promptTMP = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            promptTMP.alignment = TMPro.TextAlignmentOptions.Center;
            promptTMP.fontSize = 24f;
            promptTMP.fontStyle = TMPro.FontStyles.Bold;

            promptTMP.text = "<color=#FFEE44><b>[E]</b></color> // <color=#40EBFF>COMMUNE WITH ARCANA ALTAR</color>\n<size=70%><color=#D0A0FF>Invoke Ancient Tarot Blessing</color></size>";
            canvasObj.SetActive(false);
            promptCanvasObj = canvasObj;
        }

        private void Update()
        {
            // Spin hovering crystal
            if (hoveringCrystalTransform != null)
            {
                hoveringCrystalTransform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
                hoveringCrystalTransform.localPosition = new Vector3(0f, 1.6f + Mathf.Sin(Time.time * 2.5f) * 0.15f, 0f);
            }

            if (hasBeenUsed) return;

            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= interactRadius;

            if (inRange != isPlayerInRange)
            {
                isPlayerInRange = inRange;
                if (promptCanvasObj != null) promptCanvasObj.SetActive(isPlayerInRange);
            }

            if (isPlayerInRange)
            {
                if (Camera.main != null && promptCanvasObj != null)
                {
                    promptCanvasObj.transform.rotation = Quaternion.LookRotation(promptCanvasObj.transform.position - Camera.main.transform.position);
                }

                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
                {
                    CommuneWithShrine();
                }
            }
        }

        public void CommuneWithShrine()
        {
            if (hasBeenUsed) return;
            hasBeenUsed = true;

            if (promptCanvasObj != null) promptCanvasObj.SetActive(false);

            ApplyArcanaBlessing();
        }

        private void ApplyArcanaBlessing()
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }

            string arcanaTitle;
            string description;

            switch (rolledArcana)
            {
                case MajorArcanaType.TheFool:
                    arcanaTitle = "THE FOOL // WILD CARD";
                    description = "+35% Critical Strike Multiplier & Armor Pierce!";
                    break;
                case MajorArcanaType.TheMagician:
                    arcanaTitle = "THE MAGICIAN // ARCANE SURGE";
                    description = "+40% Spellpower & Faster Mana Regeneration!";
                    break;
                case MajorArcanaType.ThePriestess:
                    arcanaTitle = "THE PRIESTESS // SACRED VEIL";
                    description = "+120 Max HP & Constant Health Regeneration!";
                    var prog = FindAnyObjectByType<PlayerProgression>();
                    if (prog != null) { prog.maxHp += 120f; prog.Heal(120f); }
                    break;
                case MajorArcanaType.TheEmpress:
                    arcanaTitle = "THE EMPRESS // ABUNDANCE";
                    description = "Double Gold Payouts & Guaranteed Relic Drops!";
                    break;
                case MajorArcanaType.TheChariot:
                    arcanaTitle = "THE CHARIOT // RELENTLESS RUSH";
                    description = "+30% Movement Speed & Rapid Dash Cooldowns!";
                    break;
                case MajorArcanaType.TheTower:
                    arcanaTitle = "THE TOWER // KINETIC CATALYST";
                    description = "Slain enemies trigger violent shockwaves!";
                    break;
                default:
                    arcanaTitle = "THE SUN // RADIANT TRIUMPH";
                    description = "Trap Immunity & Radiant Aura Damaging Shadows!";
                    break;
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#40EBFF><b>[TAROT COMMUNION]</b></color>\n<color=#FFD700><b>{arcanaTitle}</b></color>\n<size=85%>{description}</size>");
            }

            // Visual flare
            if (shrineAuraLight != null)
            {
                shrineAuraLight.color = new Color(1.0f, 0.9f, 0.4f);
                shrineAuraLight.intensity = 4.5f;
            }
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null) playerTransform = prog.transform;
            }
        }
    }
}
