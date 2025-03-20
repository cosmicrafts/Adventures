using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Weapon;
using StinkySteak.Netick.Timer;
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.Skills
{
    public class Laser : NetworkBehaviour
    {
        [SerializeField] private LaserSkillSO laserSkillSO;
        [SerializeField] private LineRenderer laserRenderer;
        [SerializeField] private Transform weaponTransform;
        [SerializeField] private LayerMask hitableLayer;

        private PlayerEnergySystem _energySystem;
        private PlayerCharacterWeapon _weapon;
        private CooldownUIManager _cooldownUIManager;

        // Networked properties
        [Networked] private bool _isLaserActive { get; set; }
        [Networked] private Vector2 _laserStartPoint { get; set; }
        [Networked] private Vector2 _laserEndPoint { get; set; }
        [Networked] private TickTimer _laserDurationTimer { get; set; }
        [Networked] private TickTimer _cooldownTimer { get; set; }

        public override void NetworkStart()
        {
            _energySystem = GetComponent<PlayerEnergySystem>();
            _weapon = GetComponent<PlayerCharacterWeapon>();
            
            // Find cooldown UI manager
            GameObject cdManagerObject = GameObject.FindWithTag("LaserSkill");
            if (cdManagerObject != null)
            {
                _cooldownUIManager = cdManagerObject.GetComponent<CooldownUIManager>();
            }
            else
            {
                Sandbox.LogError("CooldownUIManager with tag 'LaserSkill' not found in the scene.");
            }

            // Setup laser renderer
            if (laserRenderer != null)
            {
                laserRenderer.startWidth = laserSkillSO.laserWidth;
                laserRenderer.endWidth = laserSkillSO.laserWidth;
                laserRenderer.startColor = laserSkillSO.laserColor;
                laserRenderer.endColor = laserSkillSO.laserColor;
                laserRenderer.enabled = false;
            }
        }

        public override void NetworkFixedUpdate()
        {
            if (!IsServer) return;

            if (FetchInput(out PlayerCharacterInput input))
            {
                if (input.ActivateLaser && CanActivateLaser())
                {
                    StartLaser();
                }
                else if (_isLaserActive)
                {
                    if (!input.ActivateLaser || !CanContinueLaser())
                    {
                        StopLaser();
                    }
                    else
                    {
                        UpdateLaser();
                    }
                }
            }

            // Check laser duration
            if (_isLaserActive && _laserDurationTimer.IsExpired(Sandbox))
            {
                StopLaser();
            }
        }

        private bool CanActivateLaser()
        {
            return !_isLaserActive && 
                   _cooldownTimer.IsExpiredOrNotRunning(Sandbox) && 
                   _energySystem.HasEnoughEnergy(laserSkillSO.energyCostPerTick);
        }

        private bool CanContinueLaser()
        {
            return _energySystem.HasEnoughEnergy(laserSkillSO.energyCostPerTick);
        }

        private void StartLaser()
        {
            _isLaserActive = true;
            _laserDurationTimer = TickTimer.CreateFromSeconds(Sandbox, laserSkillSO.laserDuration);
            _cooldownTimer = TickTimer.CreateFromSeconds(Sandbox, laserSkillSO.cooldownDuration);
            
            UpdateLaser();
            RpcStartLaser(_laserStartPoint, _laserEndPoint);

            // Start cooldown UI
            _cooldownUIManager?.StartCooldown(laserSkillSO.cooldownDuration, laserSkillSO.energyCostPerTick);
        }

        private void StopLaser()
        {
            _isLaserActive = false;
            RpcStopLaser();
        }

        private void UpdateLaser()
        {
            if (weaponTransform == null) return;

            Vector2 direction = DegreesToDirection(_weapon.Degree);
            Vector2 originPoint = weaponTransform.position;
            
            RaycastHit2D hit = Physics2D.Raycast(originPoint, direction, laserSkillSO.maxDistance, hitableLayer);
            Vector2 endPoint = hit.collider ? hit.point : originPoint + (direction * laserSkillSO.maxDistance);

            _laserStartPoint = originPoint;
            _laserEndPoint = endPoint;

            if (hit.collider)
            {
                ApplyLaserDamage(hit);
                _energySystem.DeductEnergy(laserSkillSO.energyCostPerTick);
            }
        }

        private void ApplyLaserDamage(RaycastHit2D hit)
        {
            var health = hit.collider.GetComponentInParent<PlayerCharacterHealth>();
            if (health)
            {
                float damageAmount = laserSkillSO.damagePerSecond * Sandbox.FixedDeltaTime;
                health.DeductShieldAndHealth(damageAmount, transform);
            }
        }

        private Vector2 DegreesToDirection(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Proxies, isReliable: true)]
        private void RpcStartLaser(Vector2 startPoint, Vector2 endPoint)
        {
            if (laserRenderer != null)
            {
                laserRenderer.enabled = true;
                laserRenderer.SetPosition(0, startPoint);
                laserRenderer.SetPosition(1, endPoint);
            }
        }

        [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Proxies, isReliable: true)]
        private void RpcStopLaser()
        {
            if (laserRenderer != null)
            {
                laserRenderer.enabled = false;
            }
        }

        [OnChanged(nameof(_laserStartPoint))]
        private void OnLaserStartPointChanged(OnChangedData data)
        {
            if (laserRenderer != null)
            {
                laserRenderer.SetPosition(0, _laserStartPoint);
            }
        }

        [OnChanged(nameof(_laserEndPoint))]
        private void OnLaserEndPointChanged(OnChangedData data)
        {
            if (laserRenderer != null)
            {
                laserRenderer.SetPosition(1, _laserEndPoint);
            }
        }
    }
}
