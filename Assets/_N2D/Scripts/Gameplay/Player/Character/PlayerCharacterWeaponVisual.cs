using Netick;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.Bullet.Dataset;
using StinkySteak.N2D.Gameplay.Bullet.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StinkySteak.N2D.Gameplay.Player.Character.Weapon
{
    public class PlayerCharacterWeaponVisual : NetickBehaviour
    {
        [SerializeField] private PlayerCharacterWeapon _weapon;
        [SerializeField] private Transform _weaponVisual;
        [SerializeField] private SpriteRenderer _weaponRenderer;
        [SerializeField] private BulletTravelVFX _bulletVfxPrefab;

        public override void NetworkStart()
        {
            _weapon.OnLastProjectileHitChanged += OnLastProjectileHitChanged;
        }

        private void OnLastProjectileHitChanged()
        {
            ProjectileHit lastProjectileHit = _weapon.LastProjectileHit;
            Vector2 originPossition = lastProjectileHit.OriginPosition;
            Vector2 hitPosition = lastProjectileHit.HitPosition;
            Vector2 bulletDirection = (hitPosition - originPossition).normalized;
            bool isHitPlayer = lastProjectileHit.IsHitPlayer;

            BulletTravelVFX bullet = Instantiate(_bulletVfxPrefab, originPossition, Quaternion.identity);
            bullet.Initialize(Sandbox, hitPosition, bulletDirection, isHitPlayer);
            
            //TODO: Temporary multipeer compatibility
            SceneManager.MoveGameObjectToScene(bullet.gameObject, Sandbox.Scene);
        }

        public override void NetworkRender()
        {
            UpdateWeaponRotationVisual();
        }

    private void UpdateWeaponRotationVisual()
    {
        // Smoothing speed for weapon rotation
        float rotationSmoothingSpeed = 64f;

        // Get the interpolated rotation from the network (if available)
        var interpolator = _weapon.FindInterpolator(nameof(_weapon.Degree));
        bool didGetData = interpolator.GetInterpolationData(InterpolationSource.Auto, out float from, out float to, out float alpha);

        // Calculate the interpolated degree
        float interpolatedDegree = didGetData ? LerpDegree(from, to, alpha) : _weapon.Degree;

        // Convert degree to Quaternion for rotation
        Quaternion targetRotation = Quaternion.Euler(0, 0, interpolatedDegree);

        // Smoothly rotate weapon visual towards the target rotation
        _weaponVisual.rotation = Quaternion.Lerp(_weaponVisual.rotation, targetRotation, Time.deltaTime * rotationSmoothingSpeed);
    }
    
        private const float INTERPOLATION_TOLERANCE = 100f;

        private float LerpDegree(float from, float to, float alpha)
        {
            float difference = Mathf.Abs(from - to);

            if (difference >= INTERPOLATION_TOLERANCE)
            {
                return to;
            }

            return Mathf.Lerp(from, to, alpha);
        }
    }
}