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

        // Add these at the top of your class
[Header("Laser Visual Components")]
[SerializeField] private LineRenderer _laserRenderer;
[SerializeField] private GameObject _hitEffect;
[SerializeField] private float _hitOffset = 0f;
[SerializeField] private bool _useLaserRotation = false;
[SerializeField] private float _mainTextureLength = 1f;
[SerializeField] private float _noiseTextureLength = 1f;
private Vector4 _length = new Vector4(1, 1, 1, 1);
private ParticleSystem[] _effects;
private ParticleSystem[] _hitParticles;


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
        [SerializeField] private float _laserEnergyCostPerTick = 1f;

        [SerializeField] private float _laserDamagePerSecond = 15f;
        private bool _isLaserActive;

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
            // Initialize laser visual components
    if (_laserRenderer != null)
    {
        _laserRenderer.enabled = false; // Start with laser disabled
    }
    if (_hitEffect != null)
    {
        _hitParticles = _hitEffect.GetComponentsInChildren<ParticleSystem>();
        _hitEffect.SetActive(false);
    }
    _effects = GetComponentsInChildren<ParticleSystem>();

        }

        public override void NetworkFixedUpdate()
        {
            ProcessAim();
            
            if (FetchInput(out PlayerCharacterInput input))
            {
                // Start laser if E is held down and energy is available
                if (input.ActivateLaser && _energySystem.HasEnoughEnergy(_laserEnergyCostPerTick) && !_isLaserActive)
                {
                    StartLaser();
                }
                else if (_isLaserActive)
                {
                    // Stop laser if input is released or energy is insufficient
                    if (!input.ActivateLaser || !_energySystem.HasEnoughEnergy(_laserEnergyCostPerTick  ))
                    {
                        StopLaser();
                    }
                    else
                    {
                        UpdateLaser();
                    }
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
            if (_laserRenderer != null)
            {
                _laserRenderer.enabled = true;
                ResetLaserVisuals();
            }

            // Start playing the origin particles when the laser is activated
            if (_effects != null)
            {
                foreach (var effect in _effects)
                {
                    if (!effect.isPlaying) effect.Play();
                }
            }

            UpdateLaser(); // Immediately update to apply initial damage
        }



private void UpdateLaser()
{
    Vector2 direction = DegreesToDirection(Degree);
    Vector2 originPoint2D = GetWeaponOriginPoint(direction);
    Vector3 originPoint = new Vector3(originPoint2D.x, originPoint2D.y, 0);

    // Update laser visual position and rotation
    SetLaserVisuals(originPoint, direction);

    // Check if the laser hits something
    ShootingRaycastResult hitResult = default;
    bool isHit = _raycastType == RaycastType.UnityPhysX
        ? ShootUnity(originPoint2D, direction, out hitResult)
        : ShootLagComp(originPoint2D, direction, out hitResult);

    // Handle laser visuals and effects
    if (isHit)
    {
        // Update laser endpoint
        Vector3 hitPoint = new Vector3(hitResult.Point.x, hitResult.Point.y, 0);
        _laserRenderer.SetPosition(1, hitPoint);

        // Activate hit effect at collision point
        Vector2 hitNormal2D = hitResult.HitObject != null
            ? (Vector2)(hitResult.HitObject.position - originPoint).normalized
            : -direction; // If no object, use opposite of laser direction
        ActivateHitEffect(hitResult.Point, hitNormal2D);

        // Adjust laser texture length
        float distance = Vector2.Distance(originPoint2D, hitResult.Point);
        UpdateLaserTextureLength(distance);

        ApplyLaserDamage(hitResult);

        // Deduct energy per tick only when damage is applied
        _energySystem.DeductEnergy(_laserEnergyCostPerTick);
    }
    else
    {
        // No hit, laser goes to max distance
        Vector2 endPos2D = originPoint2D + direction * _distance;
        Vector3 endPos = new Vector3(endPos2D.x, endPos2D.y, 0);
        _laserRenderer.SetPosition(1, endPos);

        DeactivateHitEffect();

        // Adjust laser texture length
        float distance = _distance;
        UpdateLaserTextureLength(distance);

        // Deduct energy per tick even if no hit
        _energySystem.DeductEnergy(_laserEnergyCostPerTick);
    }
}


private void StopLaser()
{
    _isLaserActive = false;

    if (_laserRenderer != null)
    {
        _laserRenderer.enabled = false;
    }

    DeactivateHitEffect();

    // Stop playing the origin particles when the laser is deactivated
    if (_effects != null)
    {
        foreach (var effect in _effects)
        {
            if (effect.isPlaying) effect.Stop();
        }
    }
}


private void ActivateHitEffect(Vector2 position, Vector2 normal)
{
    if (_hitEffect == null) return;

    Vector2 hitPosition = position + normal * _hitOffset;
    _hitEffect.transform.position = new Vector3(hitPosition.x, hitPosition.y, 0);

    // Set rotation to align with the 2D plane using the Z-axis only
    float angle = Vector2.SignedAngle(Vector2.up, normal);
    _hitEffect.transform.rotation = Quaternion.Euler(0, 0, angle);

    if (!_hitEffect.activeSelf)
    {
        _hitEffect.SetActive(true);
    }

    foreach (var particle in _hitParticles)
    {
        if (!particle.isPlaying)
        {
            particle.Clear();
            particle.Play();
        }
    }
}

private void SetLaserVisuals(Vector3 origin, Vector2 direction)
{
    // Set laser start position
    _laserRenderer.SetPosition(0, origin);

    // Align particles at the origin with the direction of the laser
    if (_effects != null)
    {
        foreach (var effect in _effects)
        {
            effect.transform.position = origin;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effect.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // Set laser rotation if needed
    if (_useLaserRotation)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _laserRenderer.transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // Adjust as needed
    }
}

private void DeactivateHitEffect()
{
    if (_hitEffect == null) return;

    if (_hitEffect.activeSelf)
    {
        _hitEffect.SetActive(false);
    }

    foreach (var particle in _hitParticles)
    {
        if (particle.isPlaying)
            particle.Stop();
    }
}
private void UpdateLaserTextureLength(float distance)
{
    _length[0] = _mainTextureLength * distance;
    _length[2] = _noiseTextureLength * distance;

    _laserRenderer.material.SetTextureScale("_MainTex", new Vector2(_length[0], _length[1]));
    _laserRenderer.material.SetTextureScale("_Noise", new Vector2(_length[2], _length[3]));
}
private void ResetLaserVisuals()
{
    if (_laserRenderer == null) return;

    _laserRenderer.SetPosition(0, transform.position);
    _laserRenderer.SetPosition(1, transform.position);

    // Reset textures
    _length = new Vector4(1, 1, 1, 1);
    _laserRenderer.material.SetTextureScale("_MainTex", new Vector2(_length[0], _length[1]));
    _laserRenderer.material.SetTextureScale("_Noise", new Vector2(_length[2], _length[3]));

    // Reset particles
    foreach (var effect in _effects)
    {
        if (!effect.isPlaying) effect.Play();
    }

    DeactivateHitEffect();
}


        private void ApplyLaserDamage(ShootingRaycastResult hitResult)
        {
            if (hitResult.HitObject != null)
            {
                if (TryGetComponentOrInParent(hitResult.HitObject, out PlayerCharacterHealth playerCharacterHealth))
                {
                    float damageAmount = _laserDamagePerSecond * Sandbox.FixedDeltaTime;
                    playerCharacterHealth.DeductShieldAndHealth(damageAmount, transform);
                   // Sandbox.Log("Laser applied " + damageAmount + " damage to target at: " + hitResult.Point);
                }
                else
                {
                    //Sandbox.Log("Laser hit an object without a health component.");
                }
            }
        }



    }
}
