using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;
using System;
using StinkySteak.N2D.Gameplay.Bullet.Dataset;
using UnityEngine;
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
        [SerializeField] private float _energyCostPerShot = 10f;

        [Networked][Smooth(false)] public float Degree { get; private set; }
        [Networked] private ProjectileHit _lastProjectileHit { get; set; }
        [Networked] private TickTimer _timerFireRate { get; set; }

        public ProjectileHit LastProjectileHit => _lastProjectileHit;
        public event Action OnLastProjectileHitChanged;

        [SerializeField] private RaycastType _raycastType;
        private PlayerEnergySystem _energySystem;
        [SerializeField] private float _laserEnergyCostPerSecond = 5f;


        [SerializeField] private float _laserDuration = 2f;
        [SerializeField] private LineRenderer _laserRenderer; // LineRenderer for the laser effect
        [SerializeField] private float _laserDamagePerSecond = 15f;
        private bool _isLaserActive;
        private TickTimer _laserDurationTimer;


        private enum RaycastType
        {
            UnityPhysX,
            NetickLagComp
        }

        [OnChanged(nameof(_lastProjectileHit))]
        private void OnChanged(OnChangedData onChangedData)
        {
            ProjectileHit old = onChangedData.GetPreviousValue<ProjectileHit>();
            ProjectileHit current = _lastProjectileHit;

            if (old.Tick == current.Tick) return;

            OnLastProjectileHitChanged?.Invoke();
        }

        public override void NetworkStart()
        {
            // Ensure we have the energy system on the same GameObject
            _energySystem = GetComponent<PlayerEnergySystem>();
            if (_energySystem == null)
            {
                Sandbox.LogError("PlayerEnergySystem is missing on the player. Please add it.");
            }
        }

        public override void NetworkFixedUpdate()
        {
            ProcessAim();
            
            if (FetchInput(out PlayerCharacterInput input))
            {
                // Start laser if firing and energy is available
                if (input.ActivateLaser && _energySystem.HasEnoughEnergy(_laserEnergyCostPerSecond) && !_isLaserActive)
                {
                    StartLaser();
                }
                else if (_isLaserActive)
                {
                    UpdateLaser();
                }
                else
                {
                    ProcessShooting();
                }
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

            if (!_energySystem.HasEnoughEnergy(_energyCostPerShot)) return; // Check if enough energy to shoot

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

            _energySystem.DeductEnergy(_energyCostPerShot); // Deduct energy for shooting

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
                playerCharacterHealth.DeductShieldAndHealth(_damage, transform);
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

        private void StartLaser()
        {
            _isLaserActive = true;
            _laserDurationTimer = TickTimer.CreateFromSeconds(Sandbox, _laserDuration);
            _laserRenderer.enabled = true; // Activate the visual effect

            UpdateLaser(); // Immediately update to apply initial damage
        }

        private void UpdateLaser()
        {
            if (!_energySystem.HasEnoughEnergy(_laserEnergyCostPerSecond) || _laserDurationTimer.IsExpired(Sandbox))
            {
                StopLaser();
                return;
            }

            _energySystem.DeductEnergy(_laserEnergyCostPerSecond * Sandbox.FixedDeltaTime);

            Vector2 direction = DegreesToDirection(Degree);
            Vector2 originPoint = GetWeaponOriginPoint(direction);
            
            if (_raycastType == RaycastType.UnityPhysX)
            {
                if (ShootUnity(originPoint, direction, out ShootingRaycastResult hitResult))
                {
                    ApplyLaserDamage(hitResult);
                    _laserRenderer.SetPosition(1, hitResult.Point); // Set laser end position
                }
            }
            else if (_raycastType == RaycastType.NetickLagComp)
            {
                if (ShootLagComp(originPoint, direction, out ShootingRaycastResult hitResult))
                {
                    ApplyLaserDamage(hitResult);
                    _laserRenderer.SetPosition(1, hitResult.Point);
                }
            }
            else
            {
                _laserRenderer.SetPosition(1, originPoint + (direction * _distance));
            }

            _laserRenderer.SetPosition(0, originPoint); // Set laser origin position
        }

        private void StopLaser()
        {
            _isLaserActive = false;
            _laserRenderer.enabled = false; // Deactivate the visual effect
        }

        private void ApplyLaserDamage(ShootingRaycastResult hitResult)
        {
            if (hitResult.HitObject.TryGetComponent<PlayerCharacterHealth>(out var health))
            {
                health.DeductShieldAndHealth(_laserDamagePerSecond * Sandbox.FixedDeltaTime, transform);
            }
        }


    }
}
