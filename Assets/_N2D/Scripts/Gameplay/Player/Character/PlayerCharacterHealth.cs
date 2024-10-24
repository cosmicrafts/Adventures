using Netick.Unity;
using Netick;
using StinkySteak.Netick.Timer;
using System;
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.Player.Character.Health
{
    public class PlayerCharacterHealth : NetworkBehaviour
    {
        [Networked] private int _health { get; set; }
        [Networked] private int _shield { get; set; }

        public const int MAX_HEALTH = 100;
        public const int MAX_SHIELD = 50;

        public event Action OnHealthChanged;
        public event Action OnShieldChanged;
        public event Action OnHealthReduced;

        public int Health => _health;
        public int Shield => _shield;

        public override void NetworkStart()
        {
            // Ensure both health and shield are at max at the start
            _health = MAX_HEALTH;
            _shield = MAX_SHIELD;
        }

        public void ReduceHealth(int amount)
        {
            // Apply damage to shield first
            if (_shield > 0)
            {
                _shield -= amount;

                if (_shield < 0)
                {
                    int remainingDamage = -_shield;
                    _shield = 0;
                    _health -= remainingDamage;
                }
            }
            else
            {
                _health -= amount;
            }

            // Trigger health reduction event
            if (_health <= 0)
            {
                Sandbox.Destroy(Object); // Destroy player object if health is 0
            }

            // Trigger the health and shield changed events
            OnHealthChanged?.Invoke();
            OnShieldChanged?.Invoke();
        }

        [OnChanged(nameof(_health))]
        private void OnChangedHealth(OnChangedData onChangedData)
        {
            OnHealthChanged?.Invoke();

            if (_health < onChangedData.GetPreviousValue<int>())
            {
                OnHealthReduced?.Invoke();
            }
        }

        [OnChanged(nameof(_shield))]
        private void OnChangedShield(OnChangedData onChangedData)
        {
            OnShieldChanged?.Invoke();
        }

        public override void NetworkFixedUpdate()
        {
            // No regeneration logic
        }
    }
}
