using Netick.Unity;
using StinkySteak.Netick.Timer;
using UnityEngine;
using TMPro;

namespace StinkySteak.N2D.Gameplay.Player.Character.Health.Visual
{
    public class PlayerCharacterHealthVisual : NetickBehaviour
    {
        public PlayerCharacterHealth _health;

        [Space]
        [SerializeField] private TextMeshProUGUI _healthText;    // TMP for health display
        [SerializeField] private TextMeshProUGUI _shieldText;    // TMP for shield display

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Material _materialDefault;
        [SerializeField] private Material _materialOnHit;
        [SerializeField] private Material _materialOnShieldHit;
        [SerializeField] private float _materialOnHitLifetime = 0.2f;

        [SerializeField] private GameObject _vfxBloodPrefab;
        [SerializeField] private GameObject _vfxShieldPrefab;

        private AuthTickTimer _timerMaterialOnHitLifetime;

        public override void NetworkStart()
        {
            _health.OnHealthChanged += UpdateHealthUI;       // Health update logic
            _health.OnShieldChanged += UpdateShieldUI;       // Shield update logic
            _health.OnHealthReduced += OnDamaged;            // Health damage logic
            _health.OnShieldReduced += OnShieldDamaged;      // Shield damage logic

            // Initialize values at start
            UpdateHealthUI();
            UpdateShieldUI();
        }

        public override void NetworkRender()
        {
            if (_timerMaterialOnHitLifetime.IsExpired(Sandbox))
            {
                _renderer.material = _materialDefault;
                _timerMaterialOnHitLifetime = AuthTickTimer.None;
            }
        }

        private void OnDamaged()
        {
            Sandbox.Instantiate(_vfxBloodPrefab, transform.position, Quaternion.identity);

            _renderer.material = _materialOnHit;
            _timerMaterialOnHitLifetime = AuthTickTimer.CreateFromSeconds(Sandbox, _materialOnHitLifetime);
        }

        private void OnShieldDamaged()
        {
            Sandbox.Instantiate(_vfxShieldPrefab, transform.position, Quaternion.identity);

            _renderer.material = _materialOnShieldHit;
            _timerMaterialOnHitLifetime = AuthTickTimer.CreateFromSeconds(Sandbox, _materialOnHitLifetime);
        }

        private void UpdateHealthUI()
        {
            _healthText.text = $"{_health.Health}/{_health.MaxHealth}";
        }

        private void UpdateShieldUI()
        {
            _shieldText.text = $"{_health.Shield}/{_health.MaxShield}";
        }
    }
}