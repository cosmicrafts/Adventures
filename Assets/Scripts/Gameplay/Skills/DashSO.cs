using UnityEngine;
using StinkySteak.N2D.Gameplay.Player.Character.Movement;

namespace StinkySteak.N2D.Gameplay.Player.Character.Skills
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
