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

        private PlayerEnergySystem _energySystem; // Reference to the energy system

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
            // Use a single input fetch that includes auto-shooter assistance
            PlayerCharacterInput modifiedInput;
            bool hasInput = GetInputWithAutoShooter(out modifiedInput);
            
            if (hasInput)
            {
                // Cache original degree for logging
                float originalDegree = Degree;
                
                // Update aim direction
                Degree = modifiedInput.LookDegree;
                
                // Log if the degree changed significantly
                if (Mathf.Abs(originalDegree - Degree) > 1f)
                {
                   // Debug.Log($"[PlayerCharacterWeapon] Weapon rotated: {originalDegree:F1}° -> {Degree:F1}°");
                }
                
                // Process shooting with the same modified input
                ProcessShootingWithInput(modifiedInput);
            }
        }
        
        // Original ProcessAim is no longer needed since we combine aiming and shooting
        // with the same modified input that includes AutoShooter assistance
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
        
        // This helper method checks if auto-shooter should modify the input
        private bool GetInputWithAutoShooter(out PlayerCharacterInput input)
        {
            // First get the regular input
            bool hasInput = FetchInput(out input);
            
            // Save original aim before AutoShooter modification
            float originalAim = input.LookDegree;
            
            // If we have input and we have an AutoShooter component, allow it to modify the input
            if (hasInput)
            {
                var autoShooter = GetComponent<AutoShooter>();
                if (autoShooter != null)
                {
                    // Let the AutoShooter modify the input (it will only change it if necessary)
                    bool wasModified = autoShooter.ModifyInput(ref input);
                    
                    if (wasModified)
                    {
                        // Log that input was modified
                        Debug.Log($"[PlayerCharacterWeapon] Input modified: Aim {originalAim:F1}° -> {input.LookDegree:F1}°, IsFiring: {input.IsFiring}");
                    }
                }
            }
            
            return hasInput;
        }
        
        // Renamed from ProcessShooting to clarify it uses provided input
        private void ProcessShootingWithInput(PlayerCharacterInput input)
        {
            if (!input.IsFiring) return;

            if (!_timerFireRate.IsExpiredOrNotRunning(Sandbox)) return;

            if (!IsServer) return;

            if (!_energySystem.HasEnoughEnergy(_energyCostPerShot)) return; // Check if enough energy to shoot

            _timerFireRate = TickTimer.CreateFromSeconds(Sandbox, _fireRate);
            
            // Log the firing direction and angle
            Debug.Log($"[PlayerCharacterWeapon] FIRING at angle {Degree:F1}°");

            // CRITICAL: Use the Degree property to calculate the firing direction
            Vector2 direction = DegreesToDirection(Degree);
            Vector2 originPoint = GetWeaponOriginPoint(direction);
            
            // Debug visualization of firing direction
            Debug.DrawRay(originPoint, direction * 10f, Color.red, 1.0f);
            Debug.Log($"[PlayerCharacterWeapon] Firing vector: ({direction.x:F3}, {direction.y:F3}) from angle {Degree:F1}°");

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
        
        // Original ProcessShooting - keep this for compatibility but redirect to new method
        private void ProcessShooting()
        {
            // Use our helper method that integrates AutoShooter
            if (!GetInputWithAutoShooter(out PlayerCharacterInput input)) return;
            
            // Pass to the renamed method that actually does the shooting work
            ProcessShootingWithInput(input);
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

        public Vector2 GetWeaponOriginPoint(Vector3 direction)
          => transform.position + (direction * _weaponOriginPointOffset);

        public Vector2 DegreesToDirection(float degrees)
        {
            // Convert to radians and calculate direction vector
            float radians = degrees * Mathf.Deg2Rad;

            float x = Mathf.Cos(radians);
            float y = Mathf.Sin(radians);

            // Debug to verify conversion
            Debug.Log($"[PlayerCharacterWeapon] DegreesToDirection: {degrees:F1}° -> ({x:F3}, {y:F3})");

            return new Vector2(x, y);
        }

        // Direct firing method for automation (AutoShooter)
        public bool FireDirectly(float aimDegree, bool doFire = true)
        {
            // Only the server can actually process shots
            if (!IsServer) return false;
            
            // Update the aim direction - make sure to set the networked property
            // This ensures the visualization system picks up the change
            Degree = aimDegree;
            
            // If we're just updating aim and not actually firing, we're done
            if (!doFire)
            {
                // The Degree property is already [Networked] so it will sync automatically
                return true;
            }
            
            // Respect fire rate restrictions
            if (!_timerFireRate.IsExpiredOrNotRunning(Sandbox)) return false;
            
            // Check energy system
            if (_energySystem != null && !_energySystem.HasEnoughEnergy(_energyCostPerShot)) return false;
            
            // Start fire rate timer
            _timerFireRate = TickTimer.CreateFromSeconds(Sandbox, _fireRate);
            
            // Perform the actual shot
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
            
            // Deduct energy
            if (_energySystem != null)
            {
                _energySystem.DeductEnergy(_energyCostPerShot);
            }
            
            // Handle hit results
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
                
                return true;
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
            
            return true;
        }

        // Special method for AutoShooter to directly fire at an angle without using the input system
        public bool AutoFire(float aimAngle)
        {
            if (!IsServer) return false;
            
            // Check fire rate and energy
            if (!_timerFireRate.IsExpiredOrNotRunning(Sandbox)) return false;
            if (!_energySystem.HasEnoughEnergy(_energyCostPerShot)) return false;
            
            // Set fire rate timer
            _timerFireRate = TickTimer.CreateFromSeconds(Sandbox, _fireRate);
            
            Debug.Log($"[PlayerCharacterWeapon] AUTO-FIRING directly at angle {aimAngle:F1}°");
            
            // Use the provided angle directly without affecting the weapon's rotation
            Vector2 direction = DegreesToDirectionRaw(aimAngle);
            Vector2 originPoint = GetWeaponOriginPoint(direction);
            
            // Debug visualization
            Debug.DrawRay(originPoint, direction * 20f, Color.blue, 1.0f);
            
            // Perform the raycast
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
            
            // Deduct energy
            _energySystem.DeductEnergy(_energyCostPerShot);
            
            // Process hit results
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
                
                return true;
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
            
            return true;
        }
        
        // Raw direction calculation that doesn't log - for internal use
        private Vector2 DegreesToDirectionRaw(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        // Helper method to set the Degree property from AutoShooter
        public void SetAimAngle(float angle)
        {
            if (!IsServer) return;
            Degree = angle;
            Debug.Log($"[PlayerCharacterWeapon] Weapon aim angle set to {angle:F1}°");
        }
    }
}