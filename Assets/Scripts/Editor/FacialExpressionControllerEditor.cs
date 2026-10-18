using UnityEngine;
using UnityEditor;
using BattleSummoner.Customization;

namespace BattleSummoner.EditorScripts
{
    [CustomEditor(typeof(FacialExpressionController))]
    public class FacialExpressionControllerEditor : UnityEditor.Editor
    {
        private string testDialogue = "System link established! The Spire awaits.";
        private float speechDuration = 2.4f;

        public override void OnInspectorGUI()
        {
            var controller = (FacialExpressionController)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("✦ ANIME FACIAL RIG & EXPRESSION MATRIX ✦", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select presets, adjust on-the-fly sliders, or test procedural vowel speech lip flap.", MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Quick Expression Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Neutral", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Neutral);
            if (GUILayout.Button("Joy", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Joy);
            if (GUILayout.Button("Angry", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Angry);
            if (GUILayout.Button("Surprised", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Surprised);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Determined", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Determined);
            if (GUILayout.Button("Sad", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Sad);
            if (GUILayout.Button("Smug", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Smug);
            if (GUILayout.Button("Thinking", GUILayout.Height(26))) controller.SetExpression(ExpressionPreset.Thinking);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Vowel Visemes (Mouth Shapes)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("A (Aa)", GUILayout.Height(24))) controller.SetVowel(VisemeVowel.A, 90f);
            if (GUILayout.Button("I (Ee)", GUILayout.Height(24))) controller.SetVowel(VisemeVowel.I, 80f);
            if (GUILayout.Button("U (Oo)", GUILayout.Height(24))) controller.SetVowel(VisemeVowel.U, 75f);
            if (GUILayout.Button("E (Eh)", GUILayout.Height(24))) controller.SetVowel(VisemeVowel.E, 80f);
            if (GUILayout.Button("O (Oh)", GUILayout.Height(24))) controller.SetVowel(VisemeVowel.O, 85f);
            if (GUILayout.Button("Clear", GUILayout.Height(24))) controller.ClearVowels();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Procedural Speech & Vowel Lip-Flap Test", EditorStyles.boldLabel);
            testDialogue = EditorGUILayout.TextField("Dialogue Line", testDialogue);
            speechDuration = EditorGUILayout.Slider("Duration (s)", speechDuration, 0.8f, 5.0f);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 0.85f, 0.55f);
            if (GUILayout.Button("▶ Speak Dialogue (Animate Vowels)", GUILayout.Height(28)))
            {
                controller.SpeakDialogue(testDialogue, speechDuration);
            }
            GUI.backgroundColor = new Color(0.95f, 0.40f, 0.40f);
            if (GUILayout.Button("■ Stop", GUILayout.Height(28), GUILayout.Width(70)))
            {
                controller.StopSpeaking();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("✦ Blender Baked Action Clips ✦", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Vowels Cycle", GUILayout.Height(24))) controller.PlayBlenderAction("Action_Phonetic_Vowels_Cycle");
            if (GUILayout.Button("▶ Dialogue: Where Am I", GUILayout.Height(24))) controller.PlayBlenderAction("Action_Dialogue_Intro_WhereAmI");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Dialogue: Thinking", GUILayout.Height(24))) controller.PlayBlenderAction("Action_Dialogue_Thinking_Spire");
            if (GUILayout.Button("▶ Dialogue: Determined", GUILayout.Height(24))) controller.PlayBlenderAction("Action_Dialogue_Determined_Conquer");
            if (GUILayout.Button("▶ Joy Smile", GUILayout.Height(24))) controller.PlayBlenderAction("Action_Expression_Joy_Smile");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Granular Controls", EditorStyles.boldLabel);
            DrawDefaultInspector();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(controller);
                controller.ApplyFacialState();
            }
        }
    }
}
