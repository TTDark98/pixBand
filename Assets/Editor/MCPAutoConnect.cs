using UnityEditor;
using UnityEngine;

namespace HorrorMansion.Editor
{
    [InitializeOnLoad]
    public static class MCPAutoConnect
    {
        static MCPAutoConnect()
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Call MCPForUnityEditorWindow.ShowWindow() via MenuItem or Reflection
                    EditorApplication.ExecuteMenuItem("Window/MCP for Unity/Toggle MCP Window");
                    Debug.Log("[MCPAutoConnect] Toggled MCP Window via ExecuteMenuItem");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[MCPAutoConnect] Failed to execute menu item: " + ex);
                }
            };
        }
    }
}
