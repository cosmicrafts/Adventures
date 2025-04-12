using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Editor script that modifies the WebGL template to prevent audio warnings.
/// </summary>
public class WebGLAudioFixer : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL)
        {
            Debug.Log("WebGLAudioFixer: Applying audio fixes to WebGL template...");
            
            // Get the path to the WebGL template
            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates");
            
            // Check if we're using the default template
            string templateName = PlayerSettings.WebGL.template;
            if (string.IsNullOrEmpty(templateName) || templateName == "PROJECT:Default")
            {
                // Get the path to the Unity default template
                templatePath = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/Default");
            }
            else
            {
                // Custom template path
                templatePath = Path.Combine(templatePath, templateName);
            }
            
            // Check if the template exists
            string indexPath = Path.Combine(templatePath, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning("WebGLAudioFixer: Could not find index.html at " + indexPath);
                return;
            }
            
            // Read the index.html file
            string content = File.ReadAllText(indexPath);
            
            // Add our audio fix code to the head section
            string audioPatch = @"
    <!-- WebGL Audio Fix Script -->
    <script>
      window.addEventListener('load', function() {
        // Prevent audio warnings by creating a silent audio context
        var silentFixAudio = function() {
          if (typeof (window.AudioContext) !== 'undefined' || typeof (window.webkitAudioContext) !== 'undefined') {
            var context = new (window.AudioContext || window.webkitAudioContext)();
            
            // Check if it's suspended
            if (context.state === 'suspended') {
              console.log('Resuming AudioContext after user interaction');
              context.resume();
            }
            
            // Create a silent oscillator
            var oscillator = context.createOscillator();
            var gainNode = context.createGain();
            gainNode.gain.value = 0;  // Set the volume to 0
            oscillator.connect(gainNode);
            gainNode.connect(context.destination);
            oscillator.start(0);
            oscillator.stop(0.001);  // Very short to be silent
            
            // Remove the event listeners now that they're used
            document.removeEventListener('click', silentFixAudio);
            document.removeEventListener('touchstart', silentFixAudio);
            document.removeEventListener('keydown', silentFixAudio);
          }
        };
        
        // Add event listeners to trigger audio context creation on user interaction
        document.addEventListener('click', silentFixAudio);
        document.addEventListener('touchstart', silentFixAudio);
        document.addEventListener('keydown', silentFixAudio);
      });
    </script>";
            
            // Add the patch before the closing head tag
            if (content.Contains("</head>"))
            {
                content = content.Replace("</head>", audioPatch + "\n  </head>");
                
                // Write the updated content
                File.WriteAllText(indexPath, content);
                Debug.Log("WebGLAudioFixer: Successfully patched index.html to prevent audio warnings");
            }
            else
            {
                Debug.LogWarning("WebGLAudioFixer: Could not find </head> tag in index.html");
            }
        }
    }
} 