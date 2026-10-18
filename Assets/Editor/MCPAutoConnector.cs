using System;
using UnityEditor;

namespace BattleSummoner.EditorTools
{
    [InitializeOnLoad]
    public static class MCPAutoConnector
    {
        static MCPAutoConnector()
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);

            EditorApplication.delayCall += () =>
            {
                try
                {
                    var windowType = Type.GetType("MCPForUnity.Editor.Windows.MCPForUnityEditorWindow, MCPForUnity.Editor");
                    if (windowType != null)
                    {
                        var showMethod = windowType.GetMethod("ShowWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        showMethod?.Invoke(null, null);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[MCPAutoConnector] " + ex.Message);
                }
            };
        }
    }
}
