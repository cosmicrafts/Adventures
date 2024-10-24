using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public Text fpsText; // If you're using UI Text, assign this from the Inspector
    private float deltaTime = 0.0f;

    void Update()
    {
        // Calculate the delta time
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        // Calculate FPS
        float fps = 1.0f / deltaTime;
        
        // Display FPS as text
        if (fpsText != null)
        {
            fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
        }
    }

    void OnGUI()
    {
        // Optionally, you can display FPS using OnGUI for testing purposes
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = Color.white;

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
}
