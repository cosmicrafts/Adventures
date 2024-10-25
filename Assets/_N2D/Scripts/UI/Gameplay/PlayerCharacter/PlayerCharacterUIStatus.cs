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

namespace StinkySteak.N2D.Gameplay.Player.Character.UI
{
    public class PlayerCharacterUIStatus : NetickBehaviour
    {
        [SerializeField] private TMP_Text _textNametag;

        [SerializeField] private PlayerCharacterHealth _health;
        [SerializeField] private PlayerCharacterWeapon _weapon;
        [SerializeField] private Slider _healthbar;
        [SerializeField] private Slider _shieldbar;
        [SerializeField] private PlayerEnergySystem _energySystem;
        [SerializeField] private Slider _energyBar;
        private PlayerSession _session;

        public override void NetworkStart()
        {
            _health.OnHealthChanged += OnHealthChanged;
            _health.OnShieldChanged += OnShieldChanged;
            _energySystem.OnEnergyChanged += OnEnergyChanged;

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

        private void OnEnergyChanged()
        {
            _energyBar.maxValue = _energySystem.MaxEnergy;
            _energyBar.value = _energySystem.Energy;
        }

        private void OnHealthChanged()
        {
            _healthbar.maxValue = _health.MaxHealth;
            _healthbar.value = _health.Health;
        }

        private void OnShieldChanged()
        {
            _shieldbar.maxValue = _health.MaxShield;
            _shieldbar.value = _health.Shield;
        }
    }
}
