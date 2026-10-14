using System.Collections;
using System.IO;
using UnityEngine;

public class CaptureHelper : MonoBehaviour
{
    public static bool CaptureComplete = false;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.25f);
        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string artifactDir = @"C:\Users\jedij\.gemini\antigravity-ide\brain\bd703b22-4582-4bd5-8097-d9fe73a35c5e";
        string artifactPath = Path.Combine(artifactDir, "mockup_full_hud_verified.png");
        File.WriteAllBytes(artifactPath, bytes);

        Destroy(tex);
        CaptureComplete = true;
        Debug.Log("📸 [SCREENSHOT] Successfully captured full HUD frame to: " + artifactPath);
        Destroy(gameObject);
    }
}
