using Netick.Unity;
using StinkySteak.Netick.Timer;
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.Bullet.VFX
{
    public class BulletTravelVFX : NetickBehaviour
    {
        [SerializeField] private float _distanceToDestroy = 2f;
        [SerializeField] private float _bulletSpeed = 2f;
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private GameObject _bulletImpactVFX;

        private Vector2 _targetPosition;
        private Vector2 _bulletDirection;
        private TickTimer _timerLifetime;
        private bool _isHitPlayer;

        public void Initialize(NetworkSandbox networkSandbox, Vector2 targetPosition, Vector2 bulletDirection, bool isHitPlayer)
        {
            networkSandbox.AttachBehaviour(this);
            _targetPosition = targetPosition;
            _bulletDirection = bulletDirection;
            _isHitPlayer = isHitPlayer;

            // Set the rotation of the bullet based on its direction
            float angle = Mathf.Atan2(_bulletDirection.y, _bulletDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            _timerLifetime = TickTimer.CreateFromSeconds(networkSandbox, _lifetime);
        }

        public void LateUpdate()
        {
            float distance = Vector2.Distance(transform.position, _targetPosition);

            bool isDestinationReached = distance <= _distanceToDestroy;

            if (isDestinationReached)
            {
                Destroy();
                return;
            }

            if (_timerLifetime.IsExpired(Sandbox))
            {
                Destroy();
                return;
            }

            // Move the bullet forward in the direction of the _bulletDirection
            transform.position += (Vector3)_bulletDirection * Time.deltaTime * _bulletSpeed;
        }

        private void Destroy()
        {
            if (!_isHitPlayer)
            {
                // Instantiate bullet impact VFX
                Sandbox.Instantiate(_bulletImpactVFX, transform.position, Quaternion.identity);
            }

            // Detach and destroy the bullet
            Sandbox.DetachBehaviour(this);
            Destroy(gameObject);
        }
    }
}
