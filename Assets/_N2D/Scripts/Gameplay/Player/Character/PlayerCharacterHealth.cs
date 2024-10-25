using Netick;
using Netick.Unity;
using StinkySteak.Netick.Timer;
using UnityEngine;
using System;

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

        [Networked] private float _health { get; set; }                 // Current health value
        [Networked] private float _shield { get; set; }                 // Current shield value
        [Networked] private TickTimer _timerHealthReplenish { get; set; }
        [Networked] private TickTimer _timerShieldReplenish { get; set; }

        public float MaxHealth => _maxHealth;
        public float Health => _health;
        public float MaxShield => _maxShield;
        public float Shield => _shield;

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
            _health = _maxHealth;  // Initialize health to the max value
            _shield = _maxShield;  // Initialize shield to the max value
        }

        public override void NetworkFixedUpdate()
        {
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

        public void DeductShieldAndHealth(float damageAmount)
        {
            // First, apply damage to the shield
            if (_shield > 0)
            {
                float remainingDamage = damageAmount - _shield;
                _shield -= damageAmount;

                if (_shield < 0)
                    _shield = 0;

                OnShieldChanged?.Invoke();
                OnShieldReduced?.Invoke();

                // If the shield is depleted, apply the remaining damage to health
                if (remainingDamage > 0)
                {
                    DeductHealth(remainingDamage);
                }
                else
                {
                    // Reset the shield replenishment delay
                    _timerShieldReplenish = TickTimer.CreateFromSeconds(Sandbox, _shieldReplenishDelay);
                }
            }
            else
            {
                // No shield, apply all damage to health
                DeductHealth(damageAmount);
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
    }
}
