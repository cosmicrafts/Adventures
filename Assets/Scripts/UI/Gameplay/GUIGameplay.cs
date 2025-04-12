using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using StinkySteak.N2D.Gameplay.Player.Character.Weapon;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;
using StinkySteak.N2D.Gameplay.Player.Session;
using StinkySteak.N2D.Gameplay.PlayerManager.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StinkySteak.N2D.Gameplay.Player.Character;
using StinkySteak.N2D.Gameplay.PlayerManager.LocalPlayer;
using StinkySteak.N2D.Netick;
using StinkySteak.N2D.Finder;


namespace StinkySteak.N2D.UI.Gameplay
{
    public class GUIGameplay : MonoBehaviour, INetickSceneLoaded
    {
        [SerializeField] private Button _buttonRespawn;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _buttonSetNickname;
        [SerializeField] private Slider _energyBar; // Energy bar UI element
        private NetworkSandbox _networkSandbox;
        private PlayerEnergySystem _energySystem;

        public void OnSceneLoaded(NetworkSandbox sandbox)
        {
            _networkSandbox = sandbox;

            LocalPlayerManager localPlayerManager = _networkSandbox.GetComponent<LocalPlayerManager>();
            localPlayerManager.OnCharacterSpawned += OnCharacterSpawned;
            localPlayerManager.OnCharacterDespawned += OnCharacterDespawned;

            _buttonSetNickname.onClick.AddListener(OnButtonSetNickname);
            _buttonRespawn.onClick.AddListener(OnButtonRespawn);
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
    }
}