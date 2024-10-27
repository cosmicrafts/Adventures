using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;
using System;
using UnityEngine;
using StinkySteak.N2D.Gameplay.PlayerInput;

namespace StinkySteak.N2D.Gameplay.Player.Character.Weapon
{
    public class PlayerCharacterLaser : NetworkBehaviour
    {
        [SerializeField] private float _laserEnergyCostPerTick = 1f;
        [SerializeField] private float _laserDamagePerSecond = 15f;
        [SerializeField] private float _distance = 10f;
        [SerializeField] private LayerMask _hitableLayer;
        [SerializeField] private LineRenderer _laserRenderer;
        private PlayerEnergySystem _energySystem;
        private bool _isLaserActive;
        private PlayerCharacterWeapon _weapon;
        public Transform _weaponTransform;

        public override void NetworkStart()
        {
            _energySystem = GetComponent<PlayerEnergySystem>();
            if (_energySystem == null)
            {
                Sandbox.LogError("PlayerEnergySystem is missing on the player. Please add it.");
            }
            _weapon = GetComponent<PlayerCharacterWeapon>();
        }

        public override void NetworkFixedUpdate()
        {
            if (FetchInput(out PlayerCharacterInput input))
            {
                if (input.ActivateLaser && _energySystem.HasEnoughEnergy(_laserEnergyCostPerTick) && !_isLaserActive)
                {
                    StartLaser();
                }
                else if (_isLaserActive)
                {
                    if (!input.ActivateLaser || !_energySystem.HasEnoughEnergy(_laserEnergyCostPerTick))
                    {
                        StopLaser();
                    }
                    else
                    {
                        UpdateLaser();
                    }
                }
            }
        }

        private void StartLaser()
        {
            _isLaserActive = true;
            _laserRenderer.enabled = true;
            UpdateLaser(); // Initial update
        }

private void UpdateLaser()
{
    if (_weapon == null || _weaponTransform == null) return;

    Vector2 direction = _weapon.DegreesToDirection(_weapon.Degree); 
    Vector2 originPoint = _weaponTransform.position;

    ShootingRaycastResult hitResult = default;
    bool isHit = PerformRaycast(originPoint, direction, out hitResult);

    _laserRenderer.SetPosition(0, originPoint);
    _laserRenderer.SetPosition(1, isHit ? hitResult.Point : originPoint + (direction * _distance));

    if (isHit)
    {
        ApplyLaserDamage(hitResult);
        _energySystem.DeductEnergy(_laserEnergyCostPerTick);
    }
    else if (!_energySystem.HasEnoughEnergy(_laserEnergyCostPerTick))
    {
        StopLaser();
    }
}

        private void StopLaser()
        {
            _isLaserActive = false;
            _laserRenderer.enabled = false;
        }

        private bool PerformRaycast(Vector3 originPoint, Vector3 direction, out ShootingRaycastResult result)
        {
            RaycastHit2D hit = Physics2D.Raycast(originPoint, direction, _distance, _hitableLayer);
            result = new ShootingRaycastResult { Point = hit.point, HitObject = hit.collider?.transform };
            return hit.collider != null;
        }

        private void ApplyLaserDamage(ShootingRaycastResult hitResult)
        {
            if (hitResult.HitObject != null && _weapon.TryGetComponentOrInParent(hitResult.HitObject, out PlayerCharacterHealth playerCharacterHealth))
            {
                float damageAmount = _laserDamagePerSecond * Sandbox.FixedDeltaTime;
                playerCharacterHealth.DeductShieldAndHealth(damageAmount, transform);
            }
        }

        public struct ShootingRaycastResult
        {
            public Transform HitObject;
            public Vector3 Point;
        }
    }
}
