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
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxShield = 50f;
        [SerializeField] private float _shieldReplenishDelay = 2f;
        [SerializeField] private float _shieldReplenishSpeed = 1f;
        [SerializeField] private float _healthReplenishDelay = 2f;
        [SerializeField] private float _healthReplenishSpeed = 1f;
        [SerializeField] private float _reflectPercentage = 0.2f;
        [SerializeField] private RegenerativeShieldSO regenerativeShieldSO;

        [SerializeField] private bool enableReflectiveShield = true;  // Now visible in Inspector

        public bool EnableReflectiveShield
        {
            get => enableReflectiveShield;
            set => enableReflectiveShield = value;
        }

        [Networked] private float _health { get; set; }
        [Networked] private float _shield { get; set; }
        [Networked] private TickTimer _timerHealthReplenish { get; set; }
        [Networked] private TickTimer _timerShieldReplenish { get; set; }
        [Networked] private TickTimer _regenerativeShieldTimer { get; set; }
        private bool _isRegenerativeShieldActive = false;

        private PlayerEnergySystem _energySystem;
        private float _originalMaxShield;
        private CooldownUIManager cooldownUIManager;

        public float MaxHealth => _maxHealth;
        public float Health => _health;
        public float MaxShield => _maxShield;
        public float Shield => _shield;

        public event Action OnHealthChanged;
        public event Action OnShieldChanged;
        public event Action OnHealthReduced;
        public event Action OnShieldReduced;

        [OnChanged(nameof(_health))]
        private void OnChangedHealth(OnChangedData onChangedData)
        {
            float previousHealth = onChangedData.GetPreviousValue<float>();
            if (_health < previousHealth)
            {
                OnHealthReduced?.Invoke();
            }
            OnHealthChanged?.Invoke();
        }

        [OnChanged(nameof(_shield))]
        private void OnChangedShield(OnChangedData onChangedData)
        {
            float previousShield = onChangedData.GetPreviousValue<float>();
            if (_shield < previousShield)
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

            _energySystem = GetComponent<PlayerEnergySystem>();
            if (_energySystem == null)
            {
                Sandbox.LogError("PlayerEnergySystem is missing on the player. Please add it.");
            }

            GameObject cdManagerObject = GameObject.FindWithTag("ShieldSkill");
            if (cdManagerObject != null)
            {
                cooldownUIManager = cdManagerObject.GetComponent<CooldownUIManager>();
            }
            else
            {
                Sandbox.LogError("CooldownUIManager with tag 'ShieldSkill' not found in the scene.");
            }
        }

        public override void NetworkFixedUpdate()
        {
            if (_isRegenerativeShieldActive && _regenerativeShieldTimer.IsExpired(Sandbox))
            {
                DeactivateRegenerativeShield();
            }

            if (FetchInput(out PlayerCharacterInput input) && input.ActivateRegenerativeShield)
            {
                if (regenerativeShieldSO != null)
                {
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
                _shield += Sandbox.FixedDeltaTime * _shieldReplenishSpeed;

                if (_shield >= _maxShield)
                    _shield = _maxShield;
            }
        }

        public void DeductShieldAndHealth(float damageAmount, Transform attacker = null)
        {
            if (_shield > 0)
            {
                float remainingDamage = damageAmount - _shield;
                float reflectedDamage = damageAmount * _reflectPercentage;

                _shield -= damageAmount;

                if (_shield < 0)
                    _shield = 0;

                OnShieldChanged?.Invoke();
                OnShieldReduced?.Invoke();

                if (EnableReflectiveShield && attacker != null)
                {
                    ReflectDamageToAttacker(attacker, reflectedDamage);
                }

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

        private void ReflectDamageToAttacker(Transform attacker, float reflectedDamage)
        {
            if (attacker.TryGetComponent<PlayerCharacterHealth>(out var attackerHealth))
            {
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

            _timerHealthReplenish = TickTimer.CreateFromSeconds(Sandbox, _healthReplenishDelay);

            OnHealthChanged?.Invoke();
            OnHealthReduced?.Invoke();
        }

        public void ActivateRegenerativeShield(float energyCost, float shieldBoost, float instantRegen, float duration)
        {
            if (_isRegenerativeShieldActive || _energySystem == null || !_energySystem.HasEnoughEnergy(energyCost)) return;

            _energySystem.DeductEnergy(energyCost);

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

            _maxShield = _originalMaxShield;
            if (_shield > _maxShield) _shield = _maxShield;

            _isRegenerativeShieldActive = false;
            Sandbox.Log("Regenerative Shield Deactivated: Shield capacity reset to normal.");
        }
    }
}
