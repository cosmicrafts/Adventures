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
    // Get the raw weapon degree (no smoothing, just instant rotation)
    float weaponDegree = _weapon.Degree;

    // Directly set the rotation, no interpolation
    _weaponVisual.rotation = Quaternion.Euler(0, 0, weaponDegree);
}

    
        private const float INTERPOLATION_TOLERANCE = 15;

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