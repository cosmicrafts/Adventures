using Netick.Unity;
using Netick;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Weapon;  // To access weapon's rotation
using StinkySteak.N2D.Gameplay.Player.Character.Energy;  // Import the energy system
using UnityEngine;
using StinkySteak.N2D.Gameplay.Player.Character.Skills;

namespace StinkySteak.N2D.Gameplay.Player.Character.Movement
{
    public class PlayerCharacterMovement : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private float _moveSpeed = 5f;
        [Networked] private bool _isDashing { get; set; }       // To track if dashing is in progress
        [Networked] private bool _canDash { get; set; } = true; // To track if dash is allowed
        [Networked] private Vector2 _dashDirection { get; set; } // Direction of the current dash
        [SerializeField] public PlayerCharacterWeapon _weapon;  // Reference to the weapon to get aiming direction
        [Networked] private Vector2 _currentTargetPosition { get; set; } = Vector2.zero;

        private PlayerEnergySystem _energySystem;
        [SerializeField] private DashSkillSO dashSkillSO;
        private CooldownUIManager cooldownUIManager;

        public bool IsWalking => _rigidbody2D.linearVelocity.magnitude > 0.1f;

        public override void NetworkStart()
        {
            _energySystem = GetComponent<PlayerEnergySystem>();
            GameObject cdManagerObject = GameObject.FindWithTag("MovementSkill");
            cooldownUIManager = cdManagerObject?.GetComponent<CooldownUIManager>();
        }

    public override void NetworkFixedUpdate()
    {
        if (FetchInput(out PlayerCharacterInput input))
        {
            // Check if a new target position has been set (right-click)
            if (input.TargetPosition != Vector2.zero)
            {
                _currentTargetPosition = input.TargetPosition;
            }

            // Prioritize dash input regardless of movement state
            if (input.Jump && _canDash && _energySystem.Energy >= dashSkillSO.energyCost)
            {
                StartDash();
                return;
            }


            if (_isDashing)
            {
                ApplyDash();
            }
            else if (_currentTargetPosition != Vector2.zero) // Move toward the stored target position
            {
                MoveTowardTarget(_currentTargetPosition);
            }
            else
            {
                // Regular WASD movement
                Vector2 inputDirection = new Vector2(input.HorizontalMove, input.VerticalMove).normalized;
                _rigidbody2D.linearVelocity = inputDirection.magnitude > 0.1f ? _moveSpeed * inputDirection : Vector2.zero;
            }
        }
    }


    private void MoveTowardTarget(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        _rigidbody2D.linearVelocity = direction * _moveSpeed;

        // Stop movement and reset the target once close enough
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            _currentTargetPosition = Vector2.zero; // Clear the target
        }
    }

    public void StartDash()
    {
        _dashDirection = GetDashDirectionFromWeapon();
        _rigidbody2D.linearVelocity = _dashDirection * dashSkillSO.dashForce;
        _energySystem.DeductEnergy(dashSkillSO.energyCost);
        _isDashing = true;
        _canDash = false;

        // Start the dash duration timer
        Sandbox.StartCoroutine(DashDurationCoroutine());

        // Start the cooldown UI with a small buffer (0.25 seconds)
        float uiCooldown = dashSkillSO.dashCooldown + 0.65f;
        cooldownUIManager?.StartCooldown(uiCooldown, dashSkillSO.energyCost);
    }


    private System.Collections.IEnumerator DashDurationCoroutine()
    {
        yield return new WaitForSeconds(dashSkillSO.dashDuration);
        _isDashing = false; // Ends the dash, allowing movement to resume

        // Start cooldown timer after dash duration ends
        Sandbox.StartCoroutine(DashCooldownCoroutine());
    }

    private System.Collections.IEnumerator DashCooldownCoroutine()
    {
        yield return new WaitForSeconds(dashSkillSO.dashCooldown);
        _canDash = true; // Allows dash to be used again after the cooldown period
    }


        private void ApplyDash()
        {
            _rigidbody2D.linearVelocity = _dashDirection * dashSkillSO.dashForce;
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
