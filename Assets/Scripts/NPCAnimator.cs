using Netick.Unity;
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.Enemy.Animate
{
    public class EnemyAnimator : NetickBehaviour
    {
        [SerializeField] private EnemyAI _enemyAI;  // Reference to the EnemyAI script
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody2D _rb;

        private readonly int PARAM_IS_WALKING = Animator.StringToHash("IsWalking");

        public override void NetworkRender()
        {
            PlayAnimation();
        }

        private void PlayAnimation()
        {
            // Check if the enemy is moving by examining the Rigidbody2D velocity
            bool isWalking = _rb.linearVelocity.magnitude > 0.1f;  // Tweak threshold as needed

            // Set the animator parameter to control the animation state
            _animator.SetBool(PARAM_IS_WALKING, isWalking);
        }
    }
}
