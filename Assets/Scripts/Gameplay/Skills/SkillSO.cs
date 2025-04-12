using UnityEngine;
using StinkySteak.Netick.Timer;

namespace StinkySteak.N2D.Gameplay.Player.Character.Skills
{
    [CreateAssetMenu(menuName = "Skills/New Skill")]
    public class SkillSO : ScriptableObject
    {
        public string skillName;
        public float cooldownDuration;
        public float energyCost;
        public Sprite icon;

        public virtual void Activate(GameObject user)
        {
            // Define base activation behavior if needed, or leave empty for specialized skill behavior.
            Debug.Log($"{skillName} activated.");
        }
    }
}
