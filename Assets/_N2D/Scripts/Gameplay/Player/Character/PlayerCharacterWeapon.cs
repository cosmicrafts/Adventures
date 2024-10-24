using UnityEngine;
using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using System;
using StinkySteak.N2D.Gameplay.Bullet.Dataset;
using StinkySteak.Netick.Timer;

#if NETICK_LAGCOMP
using Netick.Unity.Pro;
#endif

namespace StinkySteak.N2D.Gameplay.Player.Character.Weapon
{
    public class PlayerCharacterWeapon : NetworkBehaviour
    {
        [SerializeField] private float _fireRate;
        [SerializeField] private int _damage;
        [SerializeField] private float _distance;
        [SerializeField] private float _weaponOriginPointOffset = 1.5f;
        [SerializeField] private LayerMask _hitableLayer;
        [SerializeField] private float _energyReplenishDelay = 2f; // Replenish delay after energy is used
        [SerializeField] private float _energyReplenishSpeed = 1f; // Speed of energy replenishment
        [SerializeField] private float _maxEnergy = 100f; // Max energy pool
        [SerializeField] private float _energyCostPerShot = 10f; // Energy cost per shot
        [SerializeField] private float _energyCostPerAbility = 20f; // Energy cost for abilities

        [Networked][Smooth(false)] public float Degree { get; private set; }
        [Networked] private ProjectileHit _lastProjectileHit { get; set; }
        [Networked] private TickTimer _timerFireRate { get; set; }
        [Networked] private float _energy { get; set; }
        [Networked] private TickTimer _timerEnergyReplenish { get; set; }

        public ProjectileHit LastProjectileHit => _lastProjectileHit;

        public event Action OnLastProjectileHitChanged;
        public event Action OnEnergyChanged;

        [SerializeField] private RaycastType _raycastType;

        private enum RaycastType
        {
            UnityPhysX,
            NetickLagComp
        }

        public float MaxEnergy => _maxEnergy;
        public float Energy => _energy;

        [OnChanged(nameof(_energy))]
        private void OnChangedEnergy(OnChangedData onChangedData)
        {
            OnEnergyChanged?.Invoke();
        }

        public override void NetworkStart()
        {
            _energy = _maxEnergy; // Initialize the energy pool to max at the start
        }

        [OnChanged(nameof(_lastProjectileHit))]
        private void OnChanged(OnChangedData onChangedData)
        {
            ProjectileHit old = onChangedData.GetPreviousValue<ProjectileHit>();
            ProjectileHit current = _lastProjectileHit;

            if (old.Tick == current.Tick) return;

            OnLastProjectileHitChanged?.Invoke();
        }

        public override void NetworkFixedUpdate()
        {
            ProcessAim();
            ProcessShooting();
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

        private void ProcessAim()
        {
            if (!FetchInput(out PlayerCharacterInput input)) return;

            Degree = input.LookDegree;
        }

        public struct ShootingRaycastResult
        {
            public Transform HitObject;
            public Vector3 Point;
        }

        public bool ShootUnity(Vector3 originPoint, Vector3 direction, out ShootingRaycastResult result)
        {
            RaycastHit2D hit = Physics2D.Raycast(originPoint, direction, _distance, _hitableLayer);

            result = new ShootingRaycastResult()
            {
                Point = hit.point,
                HitObject = hit.collider != null ? hit.collider.transform : null
            };

            return hit.collider != null;
        }

        public bool ShootLagComp(Vector3 originPoint, Vector3 direction, out ShootingRaycastResult result)
        {
#if NETICK_LAGCOMP
            bool isHit = Sandbox.Raycast2D(originPoint, direction, out LagCompHit2D hit, InputSource, _distance, _hitableLayer);

            result = new ShootingRaycastResult()
            {
                Point = hit.Point,
                HitObject = hit.GameObject.transform,
            };

            return isHit;
#else
            result = default;
            return false;
#endif
        }

        private void ProcessShooting()
        {
            if (!FetchInput(out PlayerCharacterInput input)) return;

            if (!input.IsFiring) return;

            if (!_timerFireRate.IsExpiredOrNotRunning(Sandbox)) return;

            if (!IsServer) return;

            if (_energy < _energyCostPerShot) return; // Check if enough energy to shoot

            _timerFireRate = TickTimer.CreateFromSeconds(Sandbox, _fireRate);

            Vector2 direction = DegreesToDirection(Degree);
            Vector2 originPoint = GetWeaponOriginPoint(direction);

            ShootingRaycastResult hitResult = default;
            bool isHit = false;

            if (_raycastType == RaycastType.UnityPhysX)
            {
                isHit = ShootUnity(originPoint, direction, out hitResult);
            }
            else if (_raycastType == RaycastType.NetickLagComp)
            {
                isHit = ShootLagComp(originPoint, direction, out hitResult);
            }

            _timerEnergyReplenish = TickTimer.CreateFromSeconds(Sandbox, _energyReplenishDelay);
            _energy -= _energyCostPerShot; // Deduct energy for shooting

            if (!isHit)
            {
                Vector2 fakeHitPosition = originPoint + (direction * 1000f);

                _lastProjectileHit = new ProjectileHit()
                {
                    Tick = Sandbox.Tick.TickValue,
                    HitPosition = fakeHitPosition,
                    OriginPosition = originPoint,
                    IsHitPlayer = false,
                };
                return;
            }

            bool isHitPlayer = false;

            if (TryGetComponentOrInParent(hitResult.HitObject, out PlayerCharacterHealth playerCharacterHealth))
            {
                isHitPlayer = true;

                playerCharacterHealth.ReduceHealth(_damage);
            }

            _lastProjectileHit = new ProjectileHit()
            {
                Tick = Sandbox.Tick.TickValue,
                HitPosition = hitResult.Point,
                OriginPosition = originPoint,
                IsHitPlayer = isHitPlayer,
            };
        }

        public bool TryGetComponentOrInParent<T>(Transform transform, out T component) where T : Component
        {
            if (transform.TryGetComponent(out T outCompA))
            {
                component = outCompA;
                return true;
            }

            if (transform.parent == null)
            {
                component = null;
                return false;
            }

            if (transform.parent.TryGetComponent(out T outCompB))
            {
                component = outCompB;
                return true;
            }

            component = null;
            return false;
        }

        private Vector2 GetWeaponOriginPoint(Vector3 direction)
          => transform.position + (direction * _weaponOriginPointOffset);

        public Vector2 DegreesToDirection(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;

            float x = Mathf.Cos(radians);
            float y = Mathf.Sin(radians);

            return new Vector2(x, y);
        }
    }
}
