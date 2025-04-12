using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class WebGLAudioManager : MonoBehaviour
{
    [SerializeField] private GameObject audioStartPanel;
    private bool audioInitialized = false;

    // Import the JavaScript function
    [DllImport("__Internal")]
    private static extern void ResumeAudioContext();

    void Start()
    {
        // Pause all audio on start
        AudioListener.pause = true;

#if !UNITY_WEBGL || UNITY_EDITOR
        // If not WebGL or in editor, initialize audio immediately
        InitializeAudio();
#else
        // Show UI element that needs to be clicked
        if (audioStartPanel != null)
            audioStartPanel.SetActive(true);
#endif
    }

    // Call this method from a UI button click
    public void InitializeAudio()
    {
        if (audioInitialized)
            return;

        audioInitialized = true;
        AudioListener.pause = false;

        // Hide the audio start panel
        if (audioStartPanel != null)
            audioStartPanel.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Call JavaScript function to resume audio context
        ResumeAudioContext();
#endif

        Debug.Log("Audio initialized successfully");
    }
} 