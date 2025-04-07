using UnityEngine;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.PlayerInput;

namespace StinkySteak.N2D.Gameplay.PlayerController
{
    /// <summary>
    /// Allows switching between manual player control and automatic AI control 
    /// </summary>
    public class PlayerControllerSwitcher : MonoBehaviour
    {
        [Header("Input Providers")]
        [SerializeField] private LocalInputProvider humanInputProvider;
        [SerializeField] private AutoInputProvider botInputProvider;
        
        [Header("Settings")]
        [SerializeField] private bool startWithBotControl = false;
        [SerializeField] private KeyCode toggleControlKey = KeyCode.F1;
        
        private bool _isBotControlActive = false;
        
        private void Start()
        {
            // Initialize controllers
            if (humanInputProvider == null)
            {
                humanInputProvider = GetComponentInChildren<LocalInputProvider>();
                if (humanInputProvider == null)
                {
                    humanInputProvider = gameObject.AddComponent<LocalInputProvider>();
                }
            }
            
            if (botInputProvider == null)
            {
                botInputProvider = GetComponentInChildren<AutoInputProvider>();
                if (botInputProvider == null)
                {
                    botInputProvider = gameObject.AddComponent<AutoInputProvider>();
                }
            }
            
            // Set initial control state
            _isBotControlActive = startWithBotControl;
            UpdateControlState();
        }
        
        private void Update()
        {
            // Check for toggle key press
            if (Input.GetKeyDown(toggleControlKey))
            {
                ToggleControl();
            }
        }
        
        public void ToggleControl()
        {
            _isBotControlActive = !_isBotControlActive;
            UpdateControlState();
            
            Debug.Log($"[PlayerControllerSwitcher] Switched to {(_isBotControlActive ? "BOT" : "HUMAN")} control");
        }
        
        public void SetBotControl(bool enabled)
        {
            if (_isBotControlActive != enabled)
            {
                _isBotControlActive = enabled;
                UpdateControlState();
                
                Debug.Log($"[PlayerControllerSwitcher] {(_isBotControlActive ? "Enabled" : "Disabled")} bot control");
            }
        }
        
        private void UpdateControlState()
        {
            if (humanInputProvider != null)
            {
                humanInputProvider.enabled = !_isBotControlActive;
            }
            
            if (botInputProvider != null)
            {
                botInputProvider.enabled = _isBotControlActive;
            }
        }
    }
} 