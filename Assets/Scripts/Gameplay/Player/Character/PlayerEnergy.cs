using Netick;
using Netick.Unity;
using StinkySteak.Netick.Timer;
using UnityEngine;
using System;

namespace StinkySteak.N2D.Gameplay.Player.Character.Energy
{
    public class PlayerEnergySystem : NetworkBehaviour
    {
        [SerializeField] private float _maxEnergy = 100f;              // Maximum energy pool
        [SerializeField] private float _energyReplenishDelay = 2f;     // Delay before energy replenishment starts
        [SerializeField] private float _energyReplenishSpeed = 1f;     // Speed at which energy replenishes

        [Networked] private float _energy { get; set; }                // Current energy value
        [Networked] private TickTimer _timerEnergyReplenish { get; set; }

        public float MaxEnergy => _maxEnergy;
        public float Energy => _energy;

        public event Action OnEnergyChanged;

        [OnChanged(nameof(_energy))]
        private void OnChangedEnergy(OnChangedData onChangedData)
        {
            OnEnergyChanged?.Invoke();
        }

        public override void NetworkStart()
        {
            _energy = _maxEnergy;  // Initialize energy to the max value
        }

        public override void NetworkFixedUpdate()
        {
            ProcessEnergyReplenish();
        }

        private void ProcessEnergyReplenish()
        {
            if (!IsServer) return;

            if (_timerEnergyReplenish.IsExpired(Sandbox))
            {
                // Gradually replenish energy over time
                _energy += Sandbox.FixedDeltaTime * _energyReplenishSpeed;

                if (_energy >= _maxEnergy)
                    _energy = _maxEnergy;
            }
        }

        public void DeductEnergy(float amount)
        {
            if (_energy < amount) return; // Prevent over-deduction
            _energy -= amount;
            if (_energy < 0) _energy = 0;

            // Reset the replenishment delay
            _timerEnergyReplenish = TickTimer.CreateFromSeconds(Sandbox, _energyReplenishDelay);

            OnEnergyChanged?.Invoke();
        }

        public bool HasEnoughEnergy(float amount)
        {
            return _energy >= amount;
        }
    }
}
