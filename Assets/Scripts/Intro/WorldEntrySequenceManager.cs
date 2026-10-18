using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleSummoner.Intro
{
    public enum WorldEntryStage
    {
        SAOHyperspeedWarpTunnel,
        LogoTitleScreen,
        SAOAccountInduction,
        WorldDiveTransition
    }

    /// <summary>
    /// WorldEntrySequenceManager orchestrates the complete first-time user experience:
    /// Stage 1: Sword Art Online "Link Start" dive sequence (starts black, white hexagons shoot past, screen turns white).
    /// Stage 2: Logo Screen ("BATTLE SUMMONER" with pulsating "CLICK TO START").
    /// Stage 3: Direct transition to Character Customizer screen (create a new character with overlaid clothing).
    /// </summary>
    public class WorldEntrySequenceManager : MonoBehaviour
    {
        public static WorldEntrySequenceManager Instance { get; private set; }

        [Header("Stage Roots")]
        public GameObject saoWarpTunnelRoot;
        public GameObject titleLogoRoot;
        public GameObject saoAccountInductionRoot;

        [Header("Subsystem Controllers")]
        public SAOWarpTunnelController saoWarpTunnelController;
        public TitleLogoScreenUI titleLogoScreenUI;
        public AccountInductionUI accountInductionUI;

        [Header("Scene Transition Settings")]
        public string targetWorldSceneName = "CharacterEquipmentShowcase";

        [Header("Current State")]
        public WorldEntryStage currentStage = WorldEntryStage.SAOHyperspeedWarpTunnel;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            EnsureComponents();
        }

        private void Start()
        {
            SetStage(WorldEntryStage.SAOHyperspeedWarpTunnel);
        }

        private void EnsureComponents()
        {
            if (saoWarpTunnelController == null)
                saoWarpTunnelController = UnityEngine.Object.FindAnyObjectByType<SAOWarpTunnelController>();

            if (titleLogoScreenUI == null)
            {
                titleLogoScreenUI = UnityEngine.Object.FindAnyObjectByType<TitleLogoScreenUI>();
                if (titleLogoScreenUI == null)
                {
                    if (titleLogoRoot == null)
                    {
                        titleLogoRoot = new GameObject("Title_Logo_Screen_Root");
                        titleLogoRoot.transform.SetParent(transform, false);
                    }
                    titleLogoScreenUI = titleLogoRoot.AddComponent<TitleLogoScreenUI>();
                }
            }

            if (titleLogoRoot == null && titleLogoScreenUI != null)
            {
                titleLogoRoot = titleLogoScreenUI.gameObject;
            }
        }

        public void SetStage(WorldEntryStage stage)
        {
            currentStage = stage;

            if (saoWarpTunnelRoot != null)
                saoWarpTunnelRoot.SetActive(stage == WorldEntryStage.SAOHyperspeedWarpTunnel);

            if (titleLogoRoot != null)
                titleLogoRoot.SetActive(stage == WorldEntryStage.LogoTitleScreen);

            if (saoAccountInductionRoot != null)
                saoAccountInductionRoot.SetActive(stage == WorldEntryStage.SAOAccountInduction);

            switch (stage)
            {
                case WorldEntryStage.SAOHyperspeedWarpTunnel:
                    if (saoWarpTunnelController != null)
                    {
                        saoWarpTunnelController.BeginWarpSequence(() =>
                        {
                            SetStage(WorldEntryStage.LogoTitleScreen);
                        });
                    }
                    else
                    {
                        SetStage(WorldEntryStage.LogoTitleScreen);
                    }
                    break;

                case WorldEntryStage.LogoTitleScreen:
                    if (titleLogoScreenUI != null)
                    {
                        titleLogoScreenUI.targetCharacterSceneName = targetWorldSceneName;
                        titleLogoScreenUI.EnsureUI();
                    }
                    break;

                case WorldEntryStage.SAOAccountInduction:
                    break;
            }
        }

        public void OnAccountInductionComplete()
        {
            StartCoroutine(WorldDiveTransitionRoutine());
        }

        private IEnumerator WorldDiveTransitionRoutine()
        {
            currentStage = WorldEntryStage.WorldDiveTransition;
            yield return new WaitForSeconds(1.0f);

            if (Application.CanStreamedLevelBeLoaded(targetWorldSceneName))
            {
                SceneManager.LoadScene(targetWorldSceneName);
            }
            else
            {
                Debug.Log($"[WORLD ENTRY] Target scene '{targetWorldSceneName}' loaded or simulated.");
            }
        }
    }
}
