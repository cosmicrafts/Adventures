using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using StinkySteak.N2D.Gameplay.Player.Character.Energy; // Import the energy system
using StinkySteak.N2D.Gameplay.Player.Session;
using StinkySteak.N2D.Gameplay.PlayerManager.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StinkySteak.N2D.Gameplay.Player.Character.UI
{
    public class PlayerCharacterUIStatus : NetickBehaviour
    {
        [SerializeField] private TMP_Text _textNametag;

        [SerializeField] private PlayerCharacterHealth _health;
        [SerializeField] private PlayerEnergySystem _energySystem; // Use PlayerEnergySystem instead of PlayerCharacterWeapon
        [SerializeField] private Slider _healthbar;
        [SerializeField] private Slider _shieldbar; // New shield slider
        [SerializeField] private Slider _energyBar; // Renamed from _ammobar to _energyBar
        private PlayerSession _session;

        public override void NetworkStart()
        {
            _health.OnHealthChanged += OnHealthChanged;
            _health.OnShieldChanged += OnShieldChanged; // Subscribe to shield changes
            _energySystem.OnEnergyChanged += OnEnergyChanged; // Listen to energy changes

            GlobalPlayerManager globalPlayerManager = Sandbox.GetComponent<GlobalPlayerManager>();

            if (globalPlayerManager.TryGetSession(Entity.InputSourcePlayerId, out PlayerSession session))
            {
                _textNametag.SetText(session.Nickname);
                _session = session;
                _session.OnNicknameChanged += OnNicknameChanged;
                return;
            }

            Debug.LogError($"[{nameof(PlayerCharacterUIStatus)}]: No Player Session found for this player! inputSourceId: {Entity.InputSourcePlayerId}", this);
        }

        private void OnNicknameChanged()
        {
            _textNametag.SetText(_session.Nickname);
        }

        private void OnEnergyChanged() // Renamed to reflect the energy system
        {
            _energyBar.maxValue = _energySystem.MaxEnergy; // Use energy system's MaxEnergy
            _energyBar.value = _energySystem.Energy; // Use energy system's Energy
        }

        private void OnHealthChanged()
        {
            _healthbar.maxValue = _health.MAX_HEALTH;
            _healthbar.value = _health.Health;
        }

        private void OnShieldChanged() // New method to update shield bar
        {
            _shieldbar.maxValue = _health.MAX_SHIELD; // Set max shield value
            _shieldbar.value = _health.Shield; // Update shield value
        }
    }
}
