using Netick.Unity;
using Netick;
using StinkySteak.N2D.Gameplay.PlayerInput;
using UnityEngine;
using StinkySteak.Netick.Timer;

namespace StinkySteak.N2D.Gameplay.Player.Character.Movement
{
    public class PlayerCharacterMovement : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;  // Reference to Rigidbody2D for applying physics
        [SerializeField] private float _moveSpeed = 5f;     // Normal movement speed
        [SerializeField] private float _dashForce = 10f;    // Force applied during dash (previously jump)
        [SerializeField] private float _doubleDashForce = 8f;  // Force applied for second dash (previously double jump)
        [SerializeField] private float _dashDelay = 0.2f;   // Delay between dashes (previously double jump delay)

        [Networked] private int _dashCount { get; set; }    // Number of remaining dashes (2 = both dashes available)
        [Networked] private bool _dashButtonPressed { get; set; }  // To track dash button press
        [Networked] private bool _isMoving { get; set; }    // To track whether the player is moving

        private TickTimer _timerDashDelay;  // Timer to track the delay between the first and second dash

        public bool IsMoving => _isMoving;

        public override void NetworkFixedUpdate()
        {
            // Ensure the player has both dashes available at the start
            if (_dashCount == 0) _dashCount = 2;

            // Fetch input
            if (FetchInput(out PlayerCharacterInput input))
            {
                // Movement vector based on WASD input (normalized for balanced movement speed in all directions)
                Vector2 inputDirection = new Vector2(input.HorizontalMove, input.VerticalMove).normalized;

                // Normal movement velocity
                Vector2 velocity = _moveSpeed * inputDirection * Sandbox.FixedDeltaTime;

                // Dashing logic (replacing jumping logic)
                bool dashButtonWasPressedThisTick = !_dashButtonPressed && input.Jump; // Dash is triggered by the jump button

                // First Dash
                if (dashButtonWasPressedThisTick && _dashCount == 2)
                {
                    _rigidbody2D.velocity = _dashForce * inputDirection;
                    _dashCount--;
                    _timerDashDelay = TickTimer.CreateFromSeconds(Sandbox, _dashDelay);
                }
                // Second Dash
                else if (dashButtonWasPressedThisTick && _dashCount == 1 && _timerDashDelay.IsExpiredOrNotRunning(Sandbox))
                {
                    _rigidbody2D.velocity = _doubleDashForce * inputDirection;
                    _dashCount--;
                }

                // Update movement state
                _isMoving = inputDirection.magnitude > 0.1f;

                // Apply the movement velocity to the player
                _rigidbody2D.velocity += velocity;  // Adding to the velocity to account for dash + movement

                // Store the jump button press status
                _dashButtonPressed = input.Jump;
            }
        }
    }
}
