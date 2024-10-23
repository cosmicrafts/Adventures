using Netick.Unity;
using Netick;
using StinkySteak.N2D.Gameplay.PlayerInput;
using UnityEngine;
using StinkySteak.Netick.Timer;

namespace StinkySteak.N2D.Gameplay.Player.Character.Movement
{
    public class PlayerCharacterMovement : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _dashForce = 10f;
        [SerializeField] private float _doubleDashForce = 8f;
        [SerializeField] private float _dashDelay = 0.2f;

        [Networked] private int _dashCount { get; set; }
        [Networked] private bool _dashButtonPressed { get; set; }
        [Networked] private bool _isMoving { get; set; }

        private TickTimer _timerDashDelay;

        public bool IsWalking => _isMoving;

        public override void NetworkFixedUpdate()
        {
            if (_dashCount == 0) _dashCount = 2;

            if (FetchInput(out PlayerCharacterInput input))
            {
                // Now with vertical movement from the new input field
                Vector2 inputDirection = new Vector2(input.HorizontalMove, input.VerticalMove).normalized;
                Vector2 velocity = _moveSpeed * inputDirection * Sandbox.FixedDeltaTime;

                bool dashButtonWasPressedThisTick = !_dashButtonPressed && input.Jump;

                if (dashButtonWasPressedThisTick && _dashCount == 2)
                {
                    _rigidbody2D.linearVelocity = _dashForce * inputDirection;
                    _dashCount--;
                    _timerDashDelay = TickTimer.CreateFromSeconds(Sandbox, _dashDelay);
                }
                else if (dashButtonWasPressedThisTick && _dashCount == 1 && _timerDashDelay.IsExpiredOrNotRunning(Sandbox))
                {
                    _rigidbody2D.linearVelocity = _doubleDashForce * inputDirection;
                    _dashCount--;
                }

                _isMoving = inputDirection.magnitude > 0.1f;

                _rigidbody2D.linearVelocity += velocity;
                _dashButtonPressed = input.Jump;
            }
        }
    }
}
