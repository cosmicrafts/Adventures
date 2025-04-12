using UnityEngine;
using Cosmicrafts.Gameplay.Player.Character.Health;

namespace Cosmicrafts.Gameplay.Player.Character.Skills
{
    [CreateAssetMenu(menuName = "Skills/Regenerative Shield")]
    public class RegenerativeShieldSO : SkillSO
    {
        public float shieldBoost;
        public float instantShieldRegeneration;
        public float shieldDuration;

        public override void Activate(GameObject user)
      {
      var health = user.GetComponent<PlayerCharacterHealth>();
      if (health != null)
      {
            health.ActivateRegenerativeShield(energyCost, shieldBoost, instantShieldRegeneration, shieldDuration);
      }
      }

    }
}
