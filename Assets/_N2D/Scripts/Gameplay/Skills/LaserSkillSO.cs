using UnityEngine;

namespace StinkySteak.N2D.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "LaserSkill", menuName = "N2D/Skills/LaserSkill")]
    public class LaserSkillSO : ScriptableObject
    {
        [Header("Energy Settings")]
        public float energyCostPerTick = 1f;
        public float damagePerSecond = 15f;

        [Header("Cooldown Settings")]
        public float cooldownDuration = 5f;
        public float laserDuration = 3f;

        [Header("Visual Settings")]
        public float laserWidth = 0.1f;
        public Color laserColor = Color.red;
        public float maxDistance = 10f;
    }
} 