using Netick;
using Netick.Unity;
using Cosmicrafts.Gameplay.Player.Character.Health;
using Cosmicrafts.Gameplay.Player.Character.Weapon;
using Cosmicrafts.Gameplay.Player.Character.Energy;
using Cosmicrafts.Gameplay.Player.Character;
using Cosmicrafts.Gameplay.Player.Session;
using Cosmicrafts.Gameplay.PlayerManager.Global;
using Cosmicrafts.Gameplay.PlayerManager.LocalPlayer;
using Cosmicrafts.Netick;
using Cosmicrafts.Finder;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cosmicrafts.backend.Models;

namespace Cosmicrafts.UI.Gameplay
{
    public class GUIGameplay : MonoBehaviour, INetickSceneLoaded
    {
        [SerializeField] private Button _buttonRespawn;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _buttonSetNickname;
        [SerializeField] private Slider _energyBar; // Energy bar UI element
        private NetworkSandbox _networkSandbox;
        private PlayerEnergySystem _energySystem;
        private Cosmicrafts.backend.Models.Player _blockchainPlayerData;

        public void OnSceneLoaded(NetworkSandbox sandbox)
        {
            _networkSandbox = sandbox;

            LocalPlayerManager localPlayerManager = _networkSandbox.GetComponent<LocalPlayerManager>();
            localPlayerManager.OnCharacterSpawned += OnCharacterSpawned;
            localPlayerManager.OnCharacterDespawned += OnCharacterDespawned;
            localPlayerManager.OnSessionSpawned += OnSessionSpawned;

            _buttonSetNickname.onClick.AddListener(OnButtonSetNickname);
            _buttonRespawn.onClick.AddListener(OnButtonRespawn);

            // Subscribe to ICPService events if it exists
            if (ICPService.Instance != null)
            {
                ICPService.Instance.OnPlayerDataReceived += OnBlockchainPlayerDataReceived;
                
                // If player data already exists, use it immediately
                if (ICPService.Instance.CurrentPlayer != null)
                {
                    OnBlockchainPlayerDataReceived(ICPService.Instance.CurrentPlayer);
                }
            }
        }

        private void OnCharacterSpawned(PlayerCharacter playerCharacter)
        {
            _buttonRespawn.gameObject.SetActive(false);

            // Find and assign the PlayerEnergySystem after character spawns
            _energySystem = playerCharacter.GetComponent<PlayerEnergySystem>();
            if (_energySystem != null)
            {
                _energySystem.OnEnergyChanged += OnEnergyChanged;

                // Initialize the energy bar with current values
                _energyBar.maxValue = _energySystem.MaxEnergy;
                _energyBar.value = _energySystem.Energy;
            }
            else
            {
                Debug.LogError("PlayerEnergySystem not found for the local player character.");
            }
        }

        private void OnCharacterDespawned()
        {
            _buttonRespawn.gameObject.SetActive(true);

            // Unsubscribe from the energy change event to avoid memory leaks
            if (_energySystem != null)
            {
                _energySystem.OnEnergyChanged -= OnEnergyChanged;
                _energySystem = null;
            }
        }

        private void OnButtonSetNickname()
        {
            _networkSandbox.GetComponent<LocalPlayerManager>().Session.RPC_SetNickname(_inputField.text);
        }

        private void OnButtonRespawn()
        {
            _networkSandbox.GetComponent<LocalPlayerManager>().Session.RPC_Respawn();
        }

        private void OnEnergyChanged()
        {
            if (_energySystem != null)
            {
                _energyBar.value = _energySystem.Energy;
            }
        }

        private void OnBlockchainPlayerDataReceived(Player player)
        {
            // Store the player data for later use when session spawns
            _blockchainPlayerData = player;
            
            // If we already have a session, update nickname immediately
            if (_networkSandbox != null && _networkSandbox.GetComponent<LocalPlayerManager>().Session != null)
            {
                _networkSandbox.GetComponent<LocalPlayerManager>().Session.RPC_SetNickname(player.Username);
            }
        }

        private void OnSessionSpawned(PlayerSession session)
        {
            // If we have blockchain data, set the nickname
            if (_blockchainPlayerData != null)
            {
                session.RPC_SetNickname(_blockchainPlayerData.Username);
            }
        }
    }
}