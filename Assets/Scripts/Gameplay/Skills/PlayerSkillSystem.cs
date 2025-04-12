using Netick;
using Netick.Unity;
using UnityEngine;
using System;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;

namespace StinkySteak.N2D.Gameplay.Player.Character.Skills
{
//     public class SkillSystem : NetworkBehaviour
//     {
//         [SerializeField] private SkillSO[] skills; // Array of skill SOs
//         private PlayerEnergySystem _energySystem;

//         public event Action<SkillSO> OnSkillActivated;

//         public override void NetworkStart()
//         {
//             _energySystem = GetComponent<PlayerEnergySystem>();
//             if (_energySystem == null)
//             {
//                 Sandbox.LogError("PlayerEnergySystem is missing on the player.");
//                 return;
//             }
//         }

//         public void ActivateSkill(int skillIndex)
//         {
//             if (skillIndex < 0 || skillIndex >= skills.Length) return;

//             var skill = skills[skillIndex];
//             if (_energySystem.HasEnoughEnergy(skill.energyCost))
//             {
//                 skill.Activate(gameObject); // Pass player GameObject for activation
//                 _energySystem.DeductEnergy(skill.energyCost);
//                 OnSkillActivated?.Invoke(skill);
//             }
//         }

//         public SkillSO[] GetSkills() => skills;
//     }
}
