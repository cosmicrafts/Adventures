using Netick.Unity;
using Netick;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Weapon;  // To access weapon's rotation
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.Player.Character.Movement
{
    public class PlayerCharacterMovement : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _dashForce = 10f;        // Dash force magnitude
        [SerializeField] private float _dashCooldown = 0.5f;    // Cooldown between dashes

        [Networked] private bool _isDashing { get; set; }       // To track if dashing is in progress
        [Networked] private bool _canDash { get; set; } = true; // To track if dash is allowed
        [Networked] private Vector2 _dashDirection { get; set; } // Direction of the current dash
        [SerializeField] public PlayerCharacterWeapon _weapon;  // Reference to the weapon to get aiming direction

        public bool IsWalking => !_isDashing;

        public override void NetworkFixedUpdate()
        {
            if (FetchInput(out PlayerCharacterInput input))
            {
                if (_isDashing)
                {
                    // Continue applying dash velocity if the dash is in progress
                    ApplyDash();
                }
                else
                {
                    // Handle normal movement using WASD
                    Vector2 inputDirection = new Vector2(input.HorizontalMove, input.VerticalMove).normalized;
                    _rigidbody2D.linearVelocity = inputDirection.magnitude > 0.1f ? _moveSpeed * inputDirection : Vector2.zero;

                    // Handle dash initiation
                    if (input.Jump && _canDash)
                    {
                        StartDash();
                    }
                }
            }
        }

        private void StartDash()
        {
            // Calculate the dash direction based on the weapon's current rotation (degree)
            _dashDirection = GetDashDirectionFromWeapon();

            // Apply dash force instantly in the direction the weapon is pointing
            _rigidbody2D.linearVelocity = _dashDirection * _dashForce;

            // Update dash state
            _isDashing = true;
            _canDash = false;

            // Reset dash after cooldown
            Sandbox.StartCoroutine(DashCooldownCoroutine());
        }

        private void ApplyDash()
        {
            // Simply retain the applied dash velocity until the dash is finished
            _rigidbody2D.linearVelocity = _dashDirection * _dashForce;
        }

        private System.Collections.IEnumerator DashCooldownCoroutine()
        {
            yield return new WaitForSeconds(_dashCooldown);

            // End the dash and allow dashing again after the cooldown
            _isDashing = false;
            _canDash = true;
        }

        private Vector2 GetDashDirectionFromWeapon()
        {
            // Get the weapon's aiming degree (rotation in degrees)
            float weaponDegree = _weapon.Degree;

            // Convert degree to a direction vector (x, y)
            float radians = weaponDegree * Mathf.Deg2Rad;
            Vector2 dashDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            return dashDirection.normalized;  // Return normalized direction
        }
    }
}
