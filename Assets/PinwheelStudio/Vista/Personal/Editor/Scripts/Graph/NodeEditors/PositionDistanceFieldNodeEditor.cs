#if VISTA
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(PositionDistanceFieldNode))]
    public class PositionDistanceFieldNodeEditor : ImageNodeEditorBase, INeedUpdateNodeVisual
    {
        private static readonly GUIContent GEOMETRY = new GUIContent(
            "Geometry",
            "Points measures to each valid sample independently. Path connects valid samples in buffer order, skipping invalid samples.");
        private static readonly GUIContent CLOSED = new GUIContent(
            "Closed",
            "Connect the last valid point back to the first valid point.");
        private static readonly GUIContent PROFILE = new GUIContent(
            "Profile",
            "Edge Distance grows away from the edge on either side. Inside & Outside crosses 0.5 at the edge. Inside Only discards the exterior and creates a smooth 0-1 interior shape.");
        private static readonly GUIContent RANGE = new GUIContent(
            "Range (Meters)",
            "Distance represented by the normalized output. Inside & Outside places the edge at 0.5. Inside Only places the edge and exterior at 0, then rises to 1 inside. Edge Distance places the geometry at 0.");
        private static readonly GUIContent RANGE_MODE = new GUIContent(
            "Range",
            "Auto uses the maximum useful distance found entirely on the GPU. Manual uses an explicit distance in meters.");

        public override void OnGUI(INode node)
        {
            PositionDistanceFieldNode n = node as PositionDistanceFieldNode;
            EditorGUI.BeginChangeCheck();
            PositionDistanceFieldNode.GeometryMode geometry =
                (PositionDistanceFieldNode.GeometryMode)EditorGUILayout.EnumPopup(GEOMETRY, n.geometry);
            bool closed = n.closed;
            VistaLib.PositionDistanceFieldProfile profile = n.profile;
            PositionDistanceFieldNode.RangeMode rangeMode =
                (PositionDistanceFieldNode.RangeMode)EditorGUILayout.EnumPopup(RANGE_MODE, n.rangeMode);
            float range = n.range;
            if (rangeMode == PositionDistanceFieldNode.RangeMode.Manual)
                range = EditorGUILayout.FloatField(RANGE, n.range);
            if (geometry == PositionDistanceFieldNode.GeometryMode.Path)
            {
                closed = EditorGUILayout.Toggle(CLOSED, n.closed);
                if (closed)
                    profile = (VistaLib.PositionDistanceFieldProfile)EditorGUILayout.EnumPopup(PROFILE, n.profile);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.geometry = geometry;
                n.closed = closed;
                n.profile = profile;
                n.range = range;
                n.rangeMode = rangeMode;
            }
        }

        public void UpdateVisual(INode node, NodeView nv)
        {
            PositionDistanceFieldNode n = node as PositionDistanceFieldNode;
            nv.title = n.geometry == PositionDistanceFieldNode.GeometryMode.Points
                ? "Position SDF (Points)"
                : n.closed
                    ? "Position SDF (Polygon)"
                    : "Position SDF (Path)";
        }
    }
}
#endif
