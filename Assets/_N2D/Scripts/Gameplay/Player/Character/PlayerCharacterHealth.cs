using Netick;
using Netick.Unity;
using StinkySteak.Netick.Timer;
using UnityEngine;
using System;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;
using StinkySteak.N2D.Gameplay.Player.Character.Skills;

namespace StinkySteak.N2D.Gameplay.Player.Character.Health
{
    public class PlayerCharacterHealth : NetworkBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;               // Maximum health pool
        [SerializeField] private float _maxShield = 50f;                // Maximum shield pool
        [SerializeField] private float _shieldReplenishDelay = 2f;      // Delay before shield replenishment starts
        [SerializeField] private float _shieldReplenishSpeed = 1f;      // Speed at which shield replenishes
        [SerializeField] private float _healthReplenishDelay = 2f;      // Delay before health replenishment starts
        [SerializeField] private float _healthReplenishSpeed = 1f;      // Speed at which health replenishes
        [SerializeField] private float _reflectPercentage = 0.2f;       // Reflect Damage
        [SerializeField] private RegenerativeShieldSO regenerativeShieldSO;
        [Networked] private float _health { get; set; }
        [Networked] private float _shield { get; set; }
        [Networked] private TickTimer _timerHealthReplenish { get; set; }
        [Networked] private TickTimer _timerShieldReplenish { get; set; }
        [Networked] private TickTimer _regenerativeShieldTimer { get; set; }
        private bool _isRegenerativeShieldActive = false;

        private PlayerEnergySystem _energySystem;

        public float MaxHealth => _maxHealth;
        public float Health => _health;
        public float MaxShield => _maxShield;
        public float Shield => _shield;
        private float _originalMaxShield;
        public CooldownUIManager cooldownUIManager;

        public event Action OnHealthChanged;
        public event Action OnShieldChanged;
        public event Action OnHealthReduced;  // New event for health reduction
        public event Action OnShieldReduced;  // New event for shield reduction

        [OnChanged(nameof(_health))]
        private void OnChangedHealth(OnChangedData onChangedData)
        {
            float previousHealth = onChangedData.GetPreviousValue<float>();
            if (_health < previousHealth)  // Trigger OnHealthReduced if health decreases
            {
                OnHealthReduced?.Invoke();
            }
            OnHealthChanged?.Invoke();
        }

        [OnChanged(nameof(_shield))]
        private void OnChangedShield(OnChangedData onChangedData)
        {
            float previousShield = onChangedData.GetPreviousValue<float>();
            if (_shield < previousShield)  // Trigger OnShieldReduced if shield decreases
            {
                OnShieldReduced?.Invoke();
            }
            OnShieldChanged?.Invoke();
        }

        public override void NetworkStart()
        {
            _health = _maxHealth;
            _shield = _maxShield;
            _originalMaxShield = _maxShield;

            // Initialize energy system reference
            _energySystem = GetComponent<PlayerEnergySystem>();
            if (_energySystem == null)
            {
                Sandbox.LogError("PlayerEnergySystem is missing on the player. Please add it.");
            }
        }

        public override void NetworkFixedUpdate()
        {
            // Deactivate regenerative shield if timer expires
            if (_isRegenerativeShieldActive && _regenerativeShieldTimer.IsExpired(Sandbox))
            {
                DeactivateRegenerativeShield();
            }

            // Only activate if the cooldown is inactive and Q is pressed
            if (FetchInput(out PlayerCharacterInput input) && input.ActivateRegenerativeShield)
            {
                if (regenerativeShieldSO != null)
                {
                    // Use SO values to activate shield
                    ActivateRegenerativeShield(
                        regenerativeShieldSO.energyCost,
                        regenerativeShieldSO.shieldBoost,
                        regenerativeShieldSO.instantShieldRegeneration,
                        regenerativeShieldSO.shieldDuration
                    );
                }
                else
                {
                    Sandbox.LogError("RegenerativeShieldSO is not assigned.");
                }
            }

            ProcessHealthReplenish();
            ProcessShieldReplenish();
        }


        private void ProcessHealthReplenish()
        {
            if (!IsServer) return;

            if (_timerHealthReplenish.IsExpired(Sandbox))
            {
                // Gradually replenish health over time
                _health += Sandbox.FixedDeltaTime * _healthReplenishSpeed;

                if (_health >= _maxHealth)
                    _health = _maxHealth;
            }
        }

        private void ProcessShieldReplenish()
        {
            if (!IsServer) return;

            if (_timerShieldReplenish.IsExpired(Sandbox))
            {
                // Gradually replenish shield over time
                _shield += Sandbox.FixedDeltaTime * _shieldReplenishSpeed;

                if (_shield >= _maxShield)
                    _shield = _maxShield;
            }
        }

        public void DeductShieldAndHealth(float damageAmount, Transform attacker = null)
        {
            // Apply damage to the shield
            if (_shield > 0)
            {
                float remainingDamage = damageAmount - _shield;
                float reflectedDamage = damageAmount * _reflectPercentage;
                
                _shield -= damageAmount;

                if (_shield < 0)
                    _shield = 0;

                OnShieldChanged?.Invoke();
                OnShieldReduced?.Invoke();

                // Reflect damage back to the attacker
                if (attacker != null)
                {
                    ReflectDamageToAttacker(attacker, reflectedDamage);
                }

                // Apply remaining damage to health if shield is depleted
                if (remainingDamage > 0)
                {
                    DeductHealth(remainingDamage);
                }
                else
                {
                    _timerShieldReplenish = TickTimer.CreateFromSeconds(Sandbox, _shieldReplenishDelay);
                }
            }
            else
            {
                DeductHealth(damageAmount);
            }
        }

        // Reflect damage to attacker
        private void ReflectDamageToAttacker(Transform attacker, float reflectedDamage)
        {
            if (attacker.TryGetComponent<PlayerCharacterHealth>(out var attackerHealth))
            {
                // Use DeductShieldAndHealth to allow shield absorption on the attacker
                attackerHealth.DeductShieldAndHealth(reflectedDamage);
            }
        }


        public void DeductHealth(float amount)
        {
            _health -= amount;

            if (_health <= 0)
            {
                Sandbox.Destroy(Object);
                return;
            }

            // Reset the health replenishment delay
            _timerHealthReplenish = TickTimer.CreateFromSeconds(Sandbox, _healthReplenishDelay);

            OnHealthChanged?.Invoke();
            OnHealthReduced?.Invoke();
        }

        public void ActivateRegenerativeShield(float energyCost, float shieldBoost, float instantRegen, float duration)
        {
            if (_isRegenerativeShieldActive || _energySystem == null || !_energySystem.HasEnoughEnergy(energyCost)) return;

            _energySystem.DeductEnergy(energyCost);

            // Apply custom boost and instant regeneration values
            _maxShield += _maxShield * shieldBoost;
            _shield += _maxShield * instantRegen;
            if (_shield > _maxShield) _shield = _maxShield;

            _isRegenerativeShieldActive = true;
            _regenerativeShieldTimer = TickTimer.CreateFromSeconds(Sandbox, duration);

            cooldownUIManager.StartCooldown(duration, energyCost);

            Sandbox.Log("Regenerative Shield Activated: Custom values applied.");
        }


        private void DeactivateRegenerativeShield()
        {
            if (!_isRegenerativeShieldActive) return;

            _maxShield = _originalMaxShield; // Reset to original value
            if (_shield > _maxShield) _shield = _maxShield;

            _isRegenerativeShieldActive = false;
            Sandbox.Log("Regenerative Shield Deactivated: Shield capacity reset to normal.");
        }

    }
}
