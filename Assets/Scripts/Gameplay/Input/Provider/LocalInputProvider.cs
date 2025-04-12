using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character;
using StinkySteak.N2D.Gameplay.PlayerManager.LocalPlayer;
using UnityEngine;
using System.Collections;

namespace StinkySteak.N2D.Gameplay.PlayerInput
{
    public class LocalInputProvider : NetworkEventsListener
    {
        private PlayerCharacter _localPlayer;
        public bool activateShieldSkillButton = false; // Persistent flag for button press

        public override void OnStartup(NetworkSandbox sandbox)
        {
            if (sandbox.TryGetComponent(out LocalPlayerManager localPlayerManager))
            {
                localPlayerManager.OnCharacterSpawned += OnLocalPlayerSpawned;
            }
        }

        private void OnLocalPlayerSpawned(PlayerCharacter playerCharacter)
        {
            _localPlayer = playerCharacter;
        }

        public override void OnInput(NetworkSandbox sandbox)
        {
            PlayerCharacterInput input = new PlayerCharacterInput();

            input.HorizontalMove = Input.GetAxis("Horizontal");
            input.VerticalMove = Input.GetAxis("Vertical");
            input.Jump = Input.GetKey(KeyCode.Space);
            input.LookDegree = GetLookDegree();
            input.IsFiring = Input.GetKey(KeyCode.Mouse0);
            input.ActivateLaser = Input.GetKey(KeyCode.E);

            // Activate shield skill as long as Q or the button is held
            input.ActivateRegenerativeShield = Input.GetKey(KeyCode.Q) || activateShieldSkillButton;

            // Reset `activateShieldSkillButton` only after input is processed in the network
            // if (input.ActivateRegenerativeShield)
            // {
            //     activateShieldSkillButton = false; // Reset once network syncs the active state
            // }

            // Capture right-click target position for movement
            if (Input.GetKey(KeyCode.Mouse1))
            {
                Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                input.TargetPosition = targetPosition;
            }

            sandbox.SetInput(input);
        }

        // Method for button press to activate shield skill continuously
        public void TriggerShieldSkill()
        {
            activateShieldSkillButton = true; // Activates shield skill continuously until reset
            
            // Automatically reset flag on pointer up using Coroutine or custom delay if needed
            StartCoroutine(ResetActivateShieldSkillButton());
        }

        // Coroutine to wait for "pointer up" and reset the flag
        private IEnumerator ResetActivateShieldSkillButton()
        {
            // Wait for 0.25 seconds before resetting the flag
            yield return new WaitForSeconds(0.25f);
            activateShieldSkillButton = false;
        }



        private float GetLookDegree()
        {
            Vector2 mouseWorldSpace = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 playerPosition = GetPlayerPosition();
            Vector2 lookDirection = mouseWorldSpace - playerPosition;

            return Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        }

        private Vector2 GetPlayerPosition()
        {
            return _localPlayer != null ? _localPlayer.transform.position : Vector2.zero;
        }
    }
}
