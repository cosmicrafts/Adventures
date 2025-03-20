using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character;
using StinkySteak.N2D.Gameplay.PlayerManager.LocalPlayer;
using UnityEngine;
using System.Collections;
using Netick;
using StinkySteak.N2D.Gameplay.PlayerInput;

namespace StinkySteak.N2D.Gameplay.Input.Provider
{
    public class LocalInputProvider : NetworkBehaviour
    {
        [SerializeField] private KeyCode _laserKey = KeyCode.E;

        private PlayerCharacter _localPlayer;
        public bool activateShieldSkillButton = false; // Persistent flag for button press

        public override void NetworkFixedUpdate()
        {
            if (!IsInputSource) return;

            var input = new PlayerCharacterInput
            {
                HorizontalMove = Input.GetAxisRaw("Horizontal"),
                VerticalMove = Input.GetAxisRaw("Vertical"),
                Jump = Input.GetKeyDown(KeyCode.Space),
                IsFiring = Input.GetMouseButton(0),
                LookDegree = GetLookDegree(),
                ActivateRegenerativeShield = Input.GetKeyDown(KeyCode.Q),
                TargetPosition = GetMouseWorldPosition(),
                ActivateLaser = Input.GetKey(_laserKey)
            };

            Sandbox.SetInput(input);
        }

        private float GetLookDegree()
        {
            Vector2 mousePosition = GetMouseWorldPosition();
            Vector2 direction = mousePosition - (Vector2)transform.position;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private Vector2 GetMouseWorldPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(mousePos);
        }

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
    }
}
