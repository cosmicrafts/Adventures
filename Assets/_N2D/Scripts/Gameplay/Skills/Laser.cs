using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using StinkySteak.N2D.Gameplay.PlayerInput;
using UnityEngine;

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

        public Transform _weaponTransform;
        private Vector2 _originPoint;
        private Vector2 _direction;

        // Network properties for start and end points
        [Networked] private Vector2 _laserStartPoint { get; set; }
        [Networked] private Vector2 _laserEndPoint { get; set; }

        public void Initialize(NetworkSandbox networkSandbox, Vector2 originPoint, Vector2 direction)
        {
            networkSandbox.AttachBehaviour(this);
            _originPoint = originPoint;
            _direction = direction;

            // Set initial line renderer state
            _laserRenderer.enabled = true;
            _laserStartPoint = _originPoint;
            _laserEndPoint = _originPoint + (_direction.normalized * _distance);
        }

        public override void NetworkStart()
        {
            _energySystem = GetComponent<PlayerEnergySystem>();
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
    UpdateLaser();
    RpcStartLaser(_laserStartPoint, _laserEndPoint); // Sync laser start across clients
}

private void StopLaser()
{
    _isLaserActive = false;
    _laserRenderer.enabled = false;
    RpcStopLaser(); // Sync laser stop across clients
}


        private void UpdateLaser()
        {
            if (_weaponTransform == null) return;

            // Calculate direction and set start and end points
            Vector2 direction = (_direction != Vector2.zero) ? _direction : (Vector2)_weaponTransform.right;
            Vector2 originPoint = _weaponTransform.position;
            RaycastHit2D hit = Physics2D.Raycast(originPoint, direction, _distance, _hitableLayer);
            Vector2 endPoint = hit.collider ? hit.point : originPoint + (direction * _distance);

            // Update networked properties to sync across clients
            _laserStartPoint = originPoint;
            _laserEndPoint = endPoint;

            if (hit.collider)
            {
                ApplyLaserDamage(hit);
                _energySystem.DeductEnergy(_laserEnergyCostPerTick);
            }
        }


        private void ApplyLaserDamage(RaycastHit2D hit)
        {
            var health = hit.collider.GetComponentInParent<PlayerCharacterHealth>();
            if (health)
            {
                float damageAmount = _laserDamagePerSecond * Sandbox.FixedDeltaTime;
                health.DeductShieldAndHealth(damageAmount, transform);
            }
        }

        [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Proxies, isReliable: true)]
private void RpcStartLaser(Vector2 startPoint, Vector2 endPoint)
{
    _laserRenderer.enabled = true;
    _laserRenderer.SetPosition(0, startPoint);
    _laserRenderer.SetPosition(1, endPoint);
}

[Rpc(source: RpcPeers.Everyone, target: RpcPeers.Proxies, isReliable: true)]
private void RpcStopLaser()
{
    _laserRenderer.enabled = false;
}


// OnChanged callback for start point
[OnChanged(nameof(_laserStartPoint))]
private void OnLaserStartPointChanged(OnChangedData data)
{
    _laserRenderer.SetPosition(0, _laserStartPoint);
}

// OnChanged callback for end point
[OnChanged(nameof(_laserEndPoint))]
private void OnLaserEndPointChanged(OnChangedData data)
{
    _laserRenderer.SetPosition(1, _laserEndPoint);
}

    }
}
