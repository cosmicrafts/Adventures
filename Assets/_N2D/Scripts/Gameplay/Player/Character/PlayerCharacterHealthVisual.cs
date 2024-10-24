using Netick.Unity;
using StinkySteak.Netick.Timer;
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.Player.Character.Health.Visual
{
    public class PlayerCharacterHealthVisual : NetickBehaviour
    {
        [SerializeField] private PlayerCharacterHealth _health;

        [Space]
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Material _materialDefault;
        [SerializeField] private Material _materialOnHit;
        [SerializeField] private Material _materialOnShieldHit; // New material for shield hit
        [SerializeField] private float _materialOnHitLifetime = 0.2f;

        [SerializeField] private GameObject _vfxBloodPrefab;
        [SerializeField] private GameObject _vfxShieldHitPrefab; // New VFX for shield hit

        private AuthTickTimer _timerMaterialOnHitLifetime;

        private int _previousHealth; // Track previous health value to detect damage
        private int _previousShield; // Track previous shield value to detect damage

        public override void NetworkStart()
        {
            _health.OnHealthReduced += OnDamaged;
            _health.OnShieldChanged += OnShieldChanged; // Subscribe to shield hit

            _previousHealth = _health.Health; // Initialize health tracker
            _previousShield = _health.Shield; // Initialize shield tracker
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
            // Trigger health VFX only if health value decreases (damage)
            if (_health.Health < _previousHealth)
            {
                // Show blood VFX when health is damaged
                Sandbox.Instantiate(_vfxBloodPrefab, transform.position, Quaternion.identity);

                // Switch material to "on hit" and start the timer
                _renderer.material = _materialOnHit;
                _timerMaterialOnHitLifetime = AuthTickTimer.CreateFromSeconds(Sandbox, _materialOnHitLifetime);
            }

            // Update the previous health value for the next comparison
            _previousHealth = _health.Health;
        }

        private void OnShieldChanged()
        {
            // Trigger shield VFX only if the shield value decreases (damage)
            if (_health.Shield < _previousShield)
            {
                // Show shield VFX when the shield is hit
                Sandbox.Instantiate(_vfxShieldHitPrefab, transform.position, Quaternion.identity);

                // Switch material to "on shield hit" and start the timer
                _renderer.material = _materialOnShieldHit;
                _timerMaterialOnHitLifetime = AuthTickTimer.CreateFromSeconds(Sandbox, _materialOnHitLifetime);
            }

            // Update the previous shield value for the next comparison
            _previousShield = _health.Shield;
        }
    }
}
