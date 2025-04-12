using UnityEngine;

/// <summary>
/// Simple script to disable all audio in WebGL builds to prevent warnings.
/// Add this to a GameObject in your first scene.
/// </summary>
public class AudioDisabler : MonoBehaviour
{
    void Awake()
    {
        // Completely disable audio system
        AudioListener.volume = 0f;
        
        // Destroy all audio sources in the scene
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in sources)
        {
            source.enabled = false;
            source.volume = 0f;
        }
        
        // Keep this object throughout the game
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("Audio system disabled to prevent WebGL warnings");
    }
} 