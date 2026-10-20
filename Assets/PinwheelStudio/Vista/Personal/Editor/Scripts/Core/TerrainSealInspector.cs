#if VISTA
using Pinwheel.Vista;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor
{
    [CustomEditor(typeof(TerrainSeal))]
    public class TerrainSealInspector : Editor
    {
        private static class SectionGUI
        {
            public static readonly string GLOBAL_SETTINGS_ID = "pinwheel.vista.terrainseal.globalsettings";
            public static readonly GUIContent GLOBAL_SETTINGS = new GUIContent("Global Settings");
            public static readonly GUIContent SEAL_WORKFLOW = new GUIContent(
                "Seal Workflow",
                "Automatic captures affected tiles at the start of each generation. Manual uses persisted tile snapshots and supports falloff.");
            public static readonly string CHANNELS_ID = "pinwheel.vista.terrainseal.channels";
            public static readonly GUIContent CHANNELS = new GUIContent("Channels");
            public static readonly string CAPTURE_ID = "pinwheel.vista.terrainseal.capture";
            public static readonly GUIContent CAPTURE = new GUIContent("Capture");
            public static readonly string FALLOFF_ID = "pinwheel.vista.terrainseal.falloff";
            public static readonly GUIContent FALLOFF = new GUIContent("Falloff");
            public static readonly string REGION_ID = "pinwheel.vista.terrainseal.region";
            public static readonly GUIContent REGION = new GUIContent("Region");
        }

        private PolygonalAreaSceneGuiUtils m_polygonalSceneGui;
        private VistaManager m_manager;
        private bool m_isEditingAnchors;

        private void OnEnable()
        {
            m_polygonalSceneGui = new PolygonalAreaSceneGuiUtils(target as IPolygonalArea);
            VistaManager[] managers = Utilities.FindObjectsNoSortCompat<VistaManager>();
            m_manager = managers.Length > 0 ? managers[0] : null;
        }

        private void OnDisable()
        {
            m_polygonalSceneGui.Dispose();
        }

        public override void OnInspectorGUI()
        {
            TerrainSeal seal = target as TerrainSeal;
            VistaRuntimeSettings settings = VistaRuntimeSettings.Get();

            if (EditorCommon.BeginFoldout(SectionGUI.GLOBAL_SETTINGS_ID, SectionGUI.GLOBAL_SETTINGS, null, true))
            {
                using (new EditorGUI.DisabledScope(IsAnyManagerGenerating()))
                {
                    EditorGUI.BeginChangeCheck();
                    TerrainSealWorkflow workflow = (TerrainSealWorkflow)EditorGUILayout.EnumPopup(
                        SectionGUI.SEAL_WORKFLOW,
                        settings.sealWorkflow);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(settings, "Modify Seal Workflow");
                        settings.sealWorkflow = workflow;
                        EditorUtility.SetDirty(settings);
                        SceneView.RepaintAll();
                    }
                }
            }
            EditorCommon.EndFoldout();

            bool isManual = settings.sealWorkflow == TerrainSealWorkflow.Manual;
            if (isManual)
            {
                if (EditorCommon.BeginFoldout(SectionGUI.CAPTURE_ID, SectionGUI.CAPTURE, null, true))
                {
                    using (new EditorGUI.DisabledScope(SceneBiomeService.isCapturingSealSnapshots))
                    {
                        string label = SceneBiomeService.isCapturingSealSnapshots
                            ? "Capturing..."
                            : "Capture Overlapping Tiles";
                        if (GUILayout.Button(label))
                            SceneBiomeService.CaptureSnapshots(seal);
                    }
                }
                EditorCommon.EndFoldout();

                if (EditorCommon.BeginFoldout(SectionGUI.FALLOFF_ID, SectionGUI.FALLOFF, null, true))
                {
                    EditorGUI.BeginChangeCheck();
                    FalloffDirection direction = (FalloffDirection)EditorGUILayout.EnumPopup(
                        "Direction", seal.falloffDirection);
                    float distance = EditorGUILayout.DelayedFloatField("Distance", seal.falloffDistance);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(seal, $"Modify {seal.name}");
                        seal.falloffDirection = direction;
                        seal.falloffDistance = distance;
                        EditorUtility.SetDirty(seal);
                        SceneView.RepaintAll();
                    }
                }
                EditorCommon.EndFoldout();
            }

            if (EditorCommon.BeginFoldout(SectionGUI.REGION_ID, SectionGUI.REGION, null, true))
            {
                Bounds displayedBounds = isManual
                    ? seal.worldBounds
                    : LocalAreaUtils.CalculateWorldBounds(
                        seal.transform, seal.anchors, null, FalloffDirection.Inner);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.BoundsField("Bounds", displayedBounds);
                }

                if (m_isEditingAnchors)
                {
                    EditorGUILayout.LabelField(
                        "- Use arrow gizmos to move an anchor.\n" +
                        "- Shift Click to add a new anchor between the 2 nearest ones.\n" +
                        "- Ctrl Click on an anchor to remove it.",
                        EditorCommon.Styles.infoLabel);
                }

                PolygonalAreaInspectorUtils.EditAnchorButtons(
                    area: seal,
                    isEditing: m_isEditingAnchors,
                    editingChanged: SetAnchorEditing,
                    areaChanged: Repaint);
                using (new EditorGUI.DisabledScope(m_isEditingAnchors))
                {
                    PolygonalAreaInspectorUtils.SnapToButtons(
                        area: seal,
                        areaChanged: Repaint);
                }
            }
            EditorCommon.EndFoldout();

            if (EditorCommon.BeginFoldout(SectionGUI.CHANNELS_ID, SectionGUI.CHANNELS, null, true))
            {
                TerrainSealChannels channels = seal.channels;
                channels = DrawChannelToggle("Geometry", channels, TerrainSealChannels.Geometry);
                channels = DrawChannelToggle("Textures", channels, TerrainSealChannels.Textures);
                channels = DrawChannelToggle("Population", channels, TerrainSealChannels.Population);
                if (channels != seal.channels)
                {
                    Undo.RecordObject(seal, $"Modify {seal.name}");
                    seal.channels = channels;
                    EditorUtility.SetDirty(seal);
                }
            }
            EditorCommon.EndFoldout();
        }

        private void OnSceneGUI()
        {
            bool includeFalloff = VistaRuntimeSettings.Get().sealWorkflow == TerrainSealWorkflow.Manual;
            m_polygonalSceneGui.DrawBounds(
                m_manager != null ? m_manager.terrainMaxHeight : 0,
                includeFalloff);
            m_polygonalSceneGui.DrawSceneGUI(m_isEditingAnchors, 1, includeFalloff);
        }

        private void SetAnchorEditing(bool isEditing)
        {
            m_isEditingAnchors = isEditing;
        }

        private static bool IsAnyManagerGenerating()
        {
            foreach (VistaManager manager in VistaManager.allInstances)
            {
                if (manager != null && manager.isGenerating)
                    return true;
            }
            return false;
        }

        private static TerrainSealChannels DrawChannelToggle(
            string label,
            TerrainSealChannels channels,
            TerrainSealChannels channel)
        {
            bool selected = channels.HasFlag(channel);
            using (new EditorGUI.DisabledScope(selected && channels == channel))
            {
                bool next = EditorGUILayout.ToggleLeft(label, selected);
                if (next)
                    channels |= channel;
                else
                    channels &= ~channel;
            }
            return channels;
        }
    }
}
#endif
