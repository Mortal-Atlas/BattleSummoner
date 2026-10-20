#if VISTA
#if UNITY_2021_2_OR_NEWER
using Pinwheel.Vista;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Pinwheel.VistaEditor.UIElements
{
    [InitializeOnLoad]
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class DrawTerrainSealsToolbarToggle : EditorToolbarToggle
    {
        public const string ID = "vista-draw-terrain-seals-toggle";

        private static bool s_isEnabled;

        static DrawTerrainSealsToolbarToggle()
        {
            SceneViewOverlay.populateItemCallback += PopulateSceneViewOverlay;
            SceneView.duringSceneGui += DuringSceneViewGUI;
        }

        public DrawTerrainSealsToolbarToggle()
        {
            text = "";
            onIcon = Resources.Load<Texture2D>("Vista/Textures/SealIcon");
            offIcon = onIcon;
            tooltip = "Draw all active Terrain Seal gizmos";
            value = s_isEnabled;
        }

        protected override void ToggleValue()
        {
            base.ToggleValue();
            s_isEnabled = value;
            SceneView.RepaintAll();
        }

        private static void PopulateSceneViewOverlay(Collector<string> elementIds)
        {
            elementIds.Add(ID);
        }

        private static void DuringSceneViewGUI(SceneView sceneView)
        {
            if (!s_isEnabled || Event.current == null || Event.current.type != EventType.Repaint)
                return;

            float terrainMaxHeight = GetTerrainMaxHeight();
            bool includeFalloff = VistaRuntimeSettings.Get().sealWorkflow == TerrainSealWorkflow.Manual;
            foreach (TerrainSeal seal in TerrainSeal.allInstances)
            {
                if (seal == null || !seal.isActiveAndEnabled || Selection.activeGameObject == seal.gameObject)
                    continue;

                PolygonalAreaSceneGuiUtils.DrawGizmo(seal, terrainMaxHeight, includeFalloff);
            }
        }

        private static float GetTerrainMaxHeight()
        {
            foreach (VistaManager manager in VistaManager.allInstances)
            {
                if (manager != null && manager.isActiveAndEnabled)
                    return manager.terrainMaxHeight;
            }
            return 0;
        }
    }
}
#endif
#endif
