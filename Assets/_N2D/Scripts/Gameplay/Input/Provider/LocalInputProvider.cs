using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character;
using StinkySteak.N2D.Gameplay.PlayerManager.LocalPlayer;
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.PlayerInput
{
    public class LocalInputProvider : NetworkEventsListener
    {
        private PlayerCharacter _localPlayer;

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
            // Initialize the input struct
            PlayerCharacterInput input = new PlayerCharacterInput();
            
            // Capture horizontal and vertical movement input
            input.HorizontalMove = Input.GetAxis("Horizontal");  // A/D or Left/Right arrow
            input.VerticalMove = Input.GetAxis("Vertical");      // W/S or Up/Down arrow
            
            // Capture jump, look direction, and firing input
            input.Jump = Input.GetKey(KeyCode.Space);
            input.LookDegree = GetLookDegree();
            input.IsFiring = Input.GetKey(KeyCode.Mouse0);

            // Set the input in the sandbox
            sandbox.SetInput(input);
        }

        // Calculate the look direction in degrees based on mouse position
        private float GetLookDegree()
        {
            Vector2 mouseWorldSpace = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 playerPosition = GetPlayerPosition();
            Vector2 lookDirection = mouseWorldSpace - playerPosition;

            return Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        }

        // Get the player's current position
        private Vector2 GetPlayerPosition()
        {
            if (_localPlayer == null)
            {
                return Vector2.zero;
            }

            return _localPlayer.transform.position;
        }
    }
}
