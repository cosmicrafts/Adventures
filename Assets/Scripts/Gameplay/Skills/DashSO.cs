using UnityEngine;
using Cosmicrafts.Gameplay.Player.Character.Movement;

namespace Cosmicrafts.Gameplay.Player.Character.Skills
{
    [CreateAssetMenu(menuName = "Skills/Dash")]
    public class DashSkillSO : SkillSO
    {
        public float dashForce;
        public float dashDuration;
        public float dashCooldown;

        public override void Activate(GameObject user)
        {
            var movement = user.GetComponent<PlayerCharacterMovement>();
            if (movement != null)
            {
                movement.StartDash();
            }
        }
    }
}
