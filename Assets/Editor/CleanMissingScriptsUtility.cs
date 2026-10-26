#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorTools
{
    public static class CleanMissingScriptsUtility
    {
        [MenuItem("BattleSummoner/Tools/Clean All Missing Scripts In Scene")]
        public static void CleanAllMissingScriptsInActiveScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            int totalCleaned = 0;
            int objectsCleaned = 0;
            var cleanedNames = new List<string>();

            var stack = new Stack<GameObject>(roots);
            while (stack.Count > 0)
            {
                var go = stack.Pop();
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingCount > 0)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    if (removed > 0)
                    {
                        totalCleaned += removed;
                        objectsCleaned++;
                        if (cleanedNames.Count < 25)
                        {
                            cleanedNames.Add($"{go.name} ({removed} script(s) removed)");
                        }
                    }
                }

                for (int i = 0; i < go.transform.childCount; i++)
                {
                    stack.Push(go.transform.GetChild(i).gameObject);
                }
            }

            if (totalCleaned > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Clean Missing Scripts] Successfully purged {totalCleaned} missing script component(s) across {objectsCleaned} GameObject(s) in '{scene.name}'!\nSample objects:\n" + string.Join("\n", cleanedNames));
            }
            else
            {
                Debug.Log($"[Clean Missing Scripts] No missing script components found in active scene '{scene.name}'. Scene is clean!");
            }
        }
    }
}
#endif
