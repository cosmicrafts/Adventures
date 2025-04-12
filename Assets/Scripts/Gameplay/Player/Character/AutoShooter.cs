using UnityEngine;
using Netick.Unity;
using Netick;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Weapon;
using StinkySteak.N2D.Gameplay.Player.Character.Movement;
using StinkySteak.N2D.Gameplay.Player.Character.Energy;
using System;
using System.Collections.Generic;
using StinkySteak.Netick.Timer;

namespace StinkySteak.N2D.Gameplay.Player.Character
{
    public class AutoShooter : NetworkBehaviour
    {
        [Header("Target Detection")]
        [SerializeField] private string _targetTag = "NPC";
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private LayerMask _targetLayerMask = -1;
        [SerializeField] private bool _requireLineOfSight = true;
        [SerializeField] private float _targetCheckInterval = 0.2f; // How often to look for targets (seconds)
        
        [Header("Shooting")]
        [SerializeField] private float _fireInterval = 0.5f; // Time between shots
        [SerializeField] private float _aimSpread = 5f; // Random spread for more natural shooting
        
        [Header("Movement")]
        [SerializeField] private float _movementRadius = 15f; // How far from start position the bot will roam
        [SerializeField] private float _combatMoveSpeed = 3f; // How fast to move during combat
        [SerializeField] private float _wanderMoveSpeed = 2f; // How fast to move during wandering
        [SerializeField] private float _positionUpdateInterval = 1.5f; // How often to choose a new position
        [SerializeField] private float _idealCombatDistance = 6f; // Preferred distance to target
        [SerializeField] private float _minimumCombatDistance = 4f; // Min distance before retreating
        
        [Header("Bot Control")]
        [SerializeField] private bool _botEnabled = true; // Master toggle for bot features
        [SerializeField] private float _playerInactivityTimeout = 2f; // After how many seconds of no input to take control
        [SerializeField] private bool _allowMovementControl = true; // Whether bot should control movement
        [SerializeField] private bool _allowCombatControl = true; // Whether bot should control aiming/shooting
        [SerializeField] private BotBehaviorType _behaviorType = BotBehaviorType.Balanced;
        [SerializeField] private bool _useDirectControl = true; // Whether to use direct method calls instead of input modification
        
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _verboseDebugLogs = false; // More detailed logs
        
        [Header("Auto-Aim Settings")]
        [SerializeField] private bool _rotateTowardsTarget = true;
        [SerializeField] private float _aimAssistStrength = 0.5f; // 0 = no assist, 1 = full auto-aim
        [SerializeField] private float _playerAimThreshold = 0.1f; // How much player input is considered "actively aiming"
        [SerializeField] private float _aimSpeed = 5f; // How fast to rotate towards target
        
        // Bot behavior types
        public enum BotBehaviorType
        {
            Aggressive,  // Prioritizes attacking, stays close
            Defensive,   // Prioritizes staying alive, maintains distance
            Balanced,    // Mix of aggressive and defensive
            Sniper,      // Stays far away, precise shots
            Berserker    // All-out attack, ignores danger
        }
        
        // Bot states
        private enum BotState
        {
            Idle,          // Not doing anything, waiting
            Wandering,     // Moving around without a target
            Pursuing,      // Moving toward a target
            Attacking,     // Firing at a target
            Retreating,    // Moving away from a target
            Strafing,      // Moving sideways while targeting
            PlayerControlled // Player has taken control
        }
        
        // Internal references
        private PlayerCharacterWeapon _weapon;
        private PlayerCharacterMovement _movement;
        private Transform _currentTarget;
        private float _targetCheckTimer;
        private float _fireTimer;  // Simple non-networked timer
        private float _positionUpdateTimer;
        private bool _wasTargeting = false;
        private PlayerEnergySystem _energySystem;
        [Networked] private TickTimer _timerFireRate { get; set; }
        [SerializeField] private float _energyCostPerShot = 10f;
        
        // Bot state tracking
        private BotState _currentState = BotState.Idle;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private Vector2 _lastMovementDirection;
        private float _timeSincePlayerInput = 0f;
        private bool _isPlayerControlling = false;
        
        // Visual indicators
        private LineRenderer _aimLine;
        private LineRenderer _movementLine;
        
        // Auto-aim tracking
        private float _autoAimAngle = 0f;
        
        // Player input tracking
        private float _lastPlayerAimInput = 0f;
        private Vector2 _lastPlayerMoveInput = Vector2.zero;
        private float _timeSincePlayerAimed = 0f;
        private float _timeSincePlayerMoved = 0f;
        
        // Class to hold the entity info for our automated input
        private class BotInputData
        {
            public int PlayerId;
            public Entity Entity;
            public bool HasCreatedSource;
        }
        
        private BotInputData _botInputData;
        
        // Special method for direct firing at a target angle
        public bool AutoFire(float aimAngle)
        {
            if (!IsServer) return false;
            
            // Check fire rate
            if (!_timerFireRate.IsExpiredOrNotRunning(Sandbox)) return false;
            
            // Check energy if energy system is available
            if (_energySystem != null && !_energySystem.HasEnoughEnergy(_energyCostPerShot)) return false;
            
            // Set fire rate timer
            _timerFireRate = TickTimer.CreateFromSeconds(Sandbox, _fireInterval);
            
            Debug.Log($"[AutoShooter] AUTO-FIRING directly at angle {aimAngle:F1}°");
            
            // Use the proper method to set the aim angle first
            _weapon.SetAimAngle(aimAngle);
            
            // Deduct energy if energy system is available
            if (_energySystem != null)
            {
                _energySystem.DeductEnergy(_energyCostPerShot);
            }
            
            // Then use the built-in FireDirectly method
            return _weapon.FireDirectly(aimAngle, true);
        }
        
        #region Main Methods
        
        public override void NetworkStart()
        {
            base.NetworkStart();
            
            // Get required components
            _weapon = GetComponent<PlayerCharacterWeapon>();
            _movement = GetComponent<PlayerCharacterMovement>();
            _energySystem = GetComponent<PlayerEnergySystem>();
            
            // Check component availability
            if (_weapon == null)
            {
                DebugLog("No PlayerCharacterWeapon found. AutoShooter will not control shooting.", LogType.Error);
                _allowCombatControl = false;
            }
            
            if (_movement == null)
            {
                DebugLog("No PlayerCharacterMovement found. AutoShooter will not control movement.", LogType.Error);
                _allowMovementControl = false;
            }
            
            if (_energySystem == null)
            {
                DebugLog("No PlayerEnergySystem found. AutoShooter will use unlimited energy.", LogType.Warning);
            }
            
            // Initialize state
            _startPosition = transform.position;
            _currentState = BotState.Idle;
            
            // Set up bot input system
            if (IsServer && _useDirectControl)
            {
                SetupBotInputSystem();
            }
            
            // Create visual indicators if debug is enabled
            if (_showGizmos)
            {
                CreateVisualIndicators();
            }
            
            DebugLog("AutoShooter initialized in " + _behaviorType + " mode");
        }
        
        private void SetupBotInputSystem()
        {
            try
            {
                // Store the entity this bot is attached to
                _botInputData = new BotInputData
                {
                    Entity = Object.Entity,
                    PlayerId = Object.Entity.InputSourcePlayerId,
                    HasCreatedSource = false
                };
                
                // Important: We DON'T change the InputSource here
                // We just piggyback on the existing input source
                // This allows the bot to act as a "phantom" that injects inputs
                // when the player isn't providing any
                
                DebugLog($"Bot input system initialized for entity with playerId {_botInputData.PlayerId}", LogType.Info);
            }
            catch (System.Exception ex)
            {
                DebugLog($"Failed to setup bot input system: {ex.Message}", LogType.Error);
            }
        }
        
        // Injects bot-generated input directly into Netick
        private void InjectBotInput(float lookDegree, Vector2 moveDirection, bool isFiring)
        {
            if (!IsServer)
            {
                DebugLog("InjectBotInput: Not running on server, input injection skipped", LogType.Warning);
                return;
            }
            
            if (_botInputData == null)
            {
                DebugLog("InjectBotInput: _botInputData is null, input injection skipped", LogType.Warning);
                return;
            }
            
            try
            {
                DebugLog($"InjectBotInput: Starting input injection with aim={lookDegree:F1}°, move=({moveDirection.x:F1},{moveDirection.y:F1}), fire={isFiring}", LogType.Info);
                
                // Get the current input struct - this is the key to proper Netick integration
                var botInput = Sandbox.GetInput<PlayerCharacterInput>();
                
                // Modify the input with our bot's desired actions
                botInput.LookDegree = lookDegree;
                botInput.HorizontalMove = moveDirection.x;
                botInput.VerticalMove = moveDirection.y;
                botInput.IsFiring = isFiring;
                
                // These values are typically not changed by the bot
                // but we could set them if needed
                botInput.Jump = false;
                botInput.ActivateRegenerativeShield = false;
                botInput.TargetPosition = Vector2.zero; // Clear any existing target position
                
                // Set the modified input back to Netick
                Sandbox.SetInput(botInput);
                
                DebugLog($"InjectBotInput: Successfully injected bot input: Aim={lookDegree:F1}°, Move=({moveDirection.x:F1},{moveDirection.y:F1}), Fire={isFiring}", LogType.Info);
            }
            catch (System.Exception ex)
            {
                DebugLog($"Failed to inject bot input: {ex.Message}", LogType.Error);
            }
        }
        
        public override void NetworkFixedUpdate()
        {
            if (!_botEnabled) return;
            if (!IsServer) return;
            
            // Update timers
            UpdateTimers();
            
            // Check for player input to determine control state
            CheckForPlayerControl();
            
            // Skip bot logic if player is controlling
            if (_isPlayerControlling)
            {
                _currentState = BotState.PlayerControlled;
                return;
            }
            
            // Find target periodically
            if (_targetCheckTimer <= 0)
            {
                FindTarget();
                _targetCheckTimer = _targetCheckInterval;
            }
            
            // Update the bot state based on conditions
            UpdateBotState();
            
            // Execute current state behavior
            ExecuteStateLogic();
            
            // If we're using direct control, handle it differently based on whether we're using network input injection
            if (_useDirectControl)
            {
                // Just update state and cache our target aim and movement
                // The actual input injection happens in NetworkUpdate
                if (_botInputData != null)
                {
                    // Just cache these values for use in NetworkUpdate
                    _autoAimAngle = _currentTarget != null ? CalculateAimAngle() : 0f;
                }
                else
                {
                    // LEGACY APPROACH - only works locally
                    // Apply weapon aim directly
                    if (_allowCombatControl && _weapon != null && _currentTarget != null)
                    {
                        _weapon.SetAimAngle(_autoAimAngle);
                    }
                }
            }
            
            // Update visual indicators
            UpdateVisuals();
        }
        
        private void UpdateTimers()
        {
            _targetCheckTimer -= Sandbox.FixedDeltaTime;
            _fireTimer -= Sandbox.FixedDeltaTime;
            _positionUpdateTimer -= Sandbox.FixedDeltaTime;
            
            if (_isPlayerControlling)
            {
                _timeSincePlayerInput += Sandbox.FixedDeltaTime;
            }
        }
        
        // Override NetworkUpdate to inject our inputs in the same place as LocalInputProvider
        public override void NetworkUpdate()
        {
            base.NetworkUpdate();
            
            // Only run on server, and only when the bot is actively controlling
            if (!IsServer || !_botEnabled) 
                return;
            
            // CRITICAL: If player is controlling, DO NOT modify their aim at all
            if (_isPlayerControlling)
                return;
            
            // If we have a target, we can take action ONLY when bot is in full control
            if (_currentTarget != null && _allowCombatControl && !_isPlayerControlling)
            {
                // Only take action if we are not resimulating
                if (!IsResimulating && _weapon != null)
                {
                    // CRITICAL: When in full bot mode, use direct weapon control methods instead of input
                    // Calculate raw angle to target - this is the exact angle needed to hit the target
                    Vector2 dirToTarget = (_currentTarget.position - transform.position);
                    
                    // Check if the direction vector is valid - avoid (0,0) direction
                    float rawAimAngle;
                    if (dirToTarget.sqrMagnitude < 0.001f) // Near zero check
                    {
                        // If direction is invalid, use current weapon angle instead of 0
                        rawAimAngle = _weapon.Degree;
                        
                        DebugLog($"NetworkUpdate: Target too close, maintaining current aim: {rawAimAngle:F1}°", LogType.Warning);
                    }
                    else
                    {
                        // Normal calculation when direction is valid
                        rawAimAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
                    }
                    
                    // DIRECT CONTROL: Set the weapon's aim angle using the direct method
                    _weapon.SetAimAngle(rawAimAngle);
                    
                    DebugLog($"NetworkUpdate: Direct weapon aim at angle {rawAimAngle:F1}°", LogType.Info);
                    
                    // DIRECT FIRE: Use the AutoFire method to fire directly
                    if (ShouldFire())
                    {
                        bool fired = _weapon.AutoFire(rawAimAngle);
                        if (fired)
                        {
                            DebugLog($"NetworkUpdate: Bot fired directly at angle {rawAimAngle:F1}°", LogType.Info);
                        }
                    }
                    
                    // We DON'T modify the input at all anymore, using direct weapon control instead
                }
            }
        }
        #endregion
        
        #region Player Control Detection
        
        private void CheckForPlayerControl()
        {
            bool hasPlayerInput = FetchInput(out PlayerCharacterInput input);
            
            // Not playing, clear state
            if (!hasPlayerInput)
            {
                _timeSincePlayerAimed += Sandbox.FixedDeltaTime;
                _timeSincePlayerMoved += Sandbox.FixedDeltaTime;
                return;
            }
            
            // Check for aim input
            bool isAiming = Mathf.Abs(input.LookDegree - _lastPlayerAimInput) > _playerAimThreshold;
            if (isAiming)
            {
                _lastPlayerAimInput = input.LookDegree;
                _timeSincePlayerAimed = 0f;
            }
            else
            {
                _timeSincePlayerAimed += Sandbox.FixedDeltaTime;
            }
            
            // Check for movement input (WASD)
            Vector2 moveInput = new Vector2(input.HorizontalMove, input.VerticalMove);
            bool isMoving = moveInput.magnitude > 0.1f;
            if (isMoving)
            {
                _lastPlayerMoveInput = moveInput;
                _timeSincePlayerMoved = 0f;
            }
            else
            {
                _timeSincePlayerMoved += Sandbox.FixedDeltaTime;
            }
            
            // Check for target position input (right-click movement)
            if (input.TargetPosition != Vector2.zero)
            {
                _timeSincePlayerMoved = 0f;
            }
            
            // Check for weapon firing
            if (input.IsFiring)
            {
                // Reset aim timer when firing to maintain continuity
                _timeSincePlayerAimed = 0f;
            }
            
            // Player is controlling if recently provided input
            bool isAimingRecently = _timeSincePlayerAimed < _playerInactivityTimeout;
            bool isMovingRecently = _timeSincePlayerMoved < _playerInactivityTimeout;
            
            // Determine control state based on which systems the bot is allowed to control
            if (_allowCombatControl && _allowMovementControl)
            {
                // Bot controls everything, so player controls if either input is active
                _isPlayerControlling = isAimingRecently || isMovingRecently;
            }
            else if (_allowCombatControl)
            {
                // Bot only controls combat, so player controls combat if aiming
                _isPlayerControlling = isAimingRecently;
            }
            else if (_allowMovementControl)
            {
                // Bot only controls movement, so player controls movement if moving
                _isPlayerControlling = isMovingRecently;
            }
            
            // When using direct control, we don't need to modify the player's input
            // We just track if the player is actively providing input or not
            
            if (_isPlayerControlling)
            {
                _timeSincePlayerInput = 0f;
                
                // If we're using direct control, we can still read what the player is doing
                // but we won't modify their input directly
                if (_useDirectControl)
                {
                    //DebugLog("Player temporarily has control - bot is waiting", LogType.Info);
                }
            }
            else if (_timeSincePlayerInput >= _playerInactivityTimeout)
            {
                // Player hasn't provided input for a while, bot can take over
                _timeSincePlayerInput = _playerInactivityTimeout;
                
                if (_useDirectControl)
                {
                    DebugLog("Bot taking control via direct methods after player inactivity", LogType.Info);
                }
            }
        }
        
        // Hook into the player's input system to provide aim assist
        // This method intentionally doesn't modify input anymore, using direct weapon control instead
        public bool ModifyInput(ref PlayerCharacterInput input)
        {
            if (!_botEnabled || !IsServer) return false;
            
            // Never modify the input, but still perform some actions based on input state
            
            // If player is in control, apply optional aim assist using direct methods
            if (_isPlayerControlling && _currentTarget != null && _rotateTowardsTarget && _aimAssistStrength > 0f)
            {
                // Apply aim assist using direct weapon control, not input modification
                ApplyAimAssist(ref input);
                return false; // Return false because we're not modifying input
            }
            
            // If bot is in control and we have a target
            if (!_isPlayerControlling && _currentTarget != null && _allowCombatControl)
            {
                // Calculate aim angle
                _autoAimAngle = CalculateAimAngle();
                
                // Apply direct aiming
                if (IsServer && _weapon != null) 
                {
                    _weapon.SetAimAngle(_autoAimAngle);
                    DebugLog($"BOT AIM via direct method: {_autoAimAngle:F1}°");
                }
                
                // Handle firing via direct method
                if (ShouldFire())
                {
                    if (IsServer && _weapon != null)
                    {
                        bool fired = _weapon.AutoFire(_autoAimAngle);
                        if (fired)
                        {
                            DebugLog($"BOT FIRING via direct method at angle {_autoAimAngle:F1}°");
                        }
                    }
                }
            }
            
            // We never modify the input anymore
            return false;
        }
        
        private bool ApplyAimAssist(ref PlayerCharacterInput input)
        {
            if (_currentTarget == null || _weapon == null) return false;
            
            // If player is not controlling (bot in full control), just set the aim directly
            if (!_isPlayerControlling)
            {
                // Calculate ideal auto-aim angle
                _autoAimAngle = CalculateAimAngle();
                
                // Directly control the weapon via the direct method, bypassing input completely
                if (IsServer) _weapon.SetAimAngle(_autoAimAngle);
                
                // DON'T modify the input here at all
                DebugLog($"Bot directly aiming at target via direct method: aim={_autoAimAngle:F1}°", LogType.Info);
                return false; // Return false because we're not modifying input
            }
            
            // When player is controlling, we ONLY apply aim assist if explicitly requested
            // and we do it via direct weapon control, not input modification
            if (_rotateTowardsTarget && _aimAssistStrength > 0f)
            {
                // Player's original aim
                float playerAim = input.LookDegree;
                
                // Calculate target aim
                _autoAimAngle = CalculateAimAngle();
                
                // Calculate aim difference
                float aimDifference = NormalizeAngleDifference(_autoAimAngle - playerAim);
                
                // Log diagnostic info
                DebugLog($"Aim assist calculation: Player aim={playerAim:F1}°, Target aim={_autoAimAngle:F1}°, Difference={aimDifference:F1}°", LogType.Info);
                
                // If difference is very small, no need for assist
                if (Mathf.Abs(aimDifference) < 5f)
                {
                    return false;
                }
                
                // Calculate assisted aim (blend between player and target aim)
                float assistStrength = Mathf.Clamp01(_aimAssistStrength);
                float assistedAngle = Mathf.Lerp(playerAim, _autoAimAngle, assistStrength * 0.3f); // Reduce strength to be more subtle
                
                // Use direct method to slightly nudge weapon aim
                if (IsServer) _weapon.SetAimAngle(assistedAngle);
                
                DebugLog($"Applied aim assist via direct weapon control: {playerAim:F1}° -> {assistedAngle:F1}° (Target={_autoAimAngle:F1}°)");
                
                // DON'T modify input at all - return false to indicate input wasn't changed
                return false;
            }
            
            return false;
        }
        #endregion
        
        #region Bot State Management
        
        private void UpdateBotState()
        {
            // Previous state for logging changes
            BotState oldState = _currentState;
            
            // Default to idle
            _currentState = BotState.Idle;
            
            // If we have a target, determine combat state
            if (_currentTarget != null)
            {
                float distanceToTarget = Vector2.Distance(transform.position, _currentTarget.position);
                
                // State selection based on behavior type and conditions
                switch (_behaviorType)
                {
                    case BotBehaviorType.Aggressive:
                        if (distanceToTarget > _idealCombatDistance)
                            _currentState = BotState.Pursuing;
                        else
                            _currentState = BotState.Attacking;
                        break;
                        
                    case BotBehaviorType.Defensive:
                        if (distanceToTarget < _minimumCombatDistance)
                            _currentState = BotState.Retreating;
                        else if (distanceToTarget < _idealCombatDistance)
                            _currentState = BotState.Strafing;
                        else
                            _currentState = BotState.Attacking;
                        break;
                        
                    case BotBehaviorType.Balanced:
                        // Balanced approach - attack but keep safe distance
                        if (distanceToTarget < _minimumCombatDistance)
                            _currentState = BotState.Retreating;
                        else if (distanceToTarget > _idealCombatDistance * 1.5f)
                            _currentState = BotState.Pursuing;
                        else
                            _currentState = RandomChance(0.7f) ? BotState.Attacking : BotState.Strafing;
                        break;
                        
                    case BotBehaviorType.Sniper:
                        // Sniper - maintain long range
                        if (distanceToTarget < _idealCombatDistance * 1.2f)
                            _currentState = BotState.Retreating;
                        else
                            _currentState = BotState.Attacking;
                        break;
                        
                    case BotBehaviorType.Berserker:
                        // Berserker - always attack
                        _currentState = distanceToTarget > _minimumCombatDistance * 0.5f ? 
                            BotState.Pursuing : BotState.Attacking;
                        break;
                }
            }
            else
            {
                // No target, wander around
                _currentState = BotState.Wandering;
            }
            
            // Log state changes
            if (oldState != _currentState)
            {
                DebugLog($"Bot state changed: {oldState} -> {_currentState}");
            }
        }
        
        private void ExecuteStateLogic()
        {
            switch (_currentState)
            {
                case BotState.Idle:
                    // Do nothing, just stand still
                    break;
                    
                case BotState.Wandering:
                    ExecuteWandering();
                    break;
                    
                case BotState.Pursuing:
                    ExecutePursuing();
                    break;
                    
                case BotState.Attacking:
                    ExecuteAttacking();
                    break;
                    
                case BotState.Retreating:
                    ExecuteRetreating();
                    break;
                    
                case BotState.Strafing:
                    ExecuteStrafing();
                    break;
                    
                case BotState.PlayerControlled:
                    // Player has control, do nothing
                    break;
            }
        }
        
        private void ExecuteWandering()
        {
            // Choose new position periodically
            if (_positionUpdateTimer <= 0f)
            {
                // Random position within movement radius of start
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(0f, _movementRadius);
                
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                _targetPosition = _startPosition + offset;
                _positionUpdateTimer = _positionUpdateInterval;
            }
            
            // Move toward the target position
            if (_allowMovementControl)
            {
                MoveToPosition(_targetPosition, _wanderMoveSpeed);
            }
        }
        
        private void ExecutePursuing()
        {
            if (_currentTarget == null) return;
            
            // Move toward the target
            if (_allowMovementControl)
            {
                MoveToPosition(_currentTarget.position, _combatMoveSpeed);
            }
            
            // Also aim and maybe shoot if close enough
            if (_allowCombatControl)
            {
                // Aim at target
                _autoAimAngle = CalculateAimAngle();
                _weapon.SetAimAngle(_autoAimAngle);
                
                // Try to shoot if we're getting close enough
                float distanceToTarget = Vector2.Distance(transform.position, _currentTarget.position);
                if (distanceToTarget < _idealCombatDistance * 1.3f && ShouldFire())
                {
                    FireAtTarget();
                }
            }
        }
        
        private void ExecuteAttacking()
        {
            if (_currentTarget == null) return;
            
            // Stand still or move very little
            if (_allowMovementControl && RandomChance(0.3f))
            {
                // Occasionally make small movement adjustments
                Vector2 smallOffset = UnityEngine.Random.insideUnitCircle * 1.5f;
                _targetPosition = (Vector2)transform.position + smallOffset;
                MoveToPosition(_targetPosition, _combatMoveSpeed * 0.5f);
            }
            
            // Focus on shooting
            if (_allowCombatControl)
            {
                // Aim at target with precision (less spread)
                _autoAimAngle = CalculateAimAngle(true);
                _weapon.SetAimAngle(_autoAimAngle);
                
                // Fire at target if conditions are right
                if (ShouldFire())
                {
                    FireAtTarget();
                }
            }
        }
        
        private void ExecuteRetreating()
        {
            if (_currentTarget == null) return;
            
            // Move away from target
            if (_allowMovementControl)
            {
                Vector2 awayDirection = ((Vector2)transform.position - (Vector2)_currentTarget.position).normalized;
                _targetPosition = (Vector2)transform.position + awayDirection * _idealCombatDistance;
                MoveToPosition(_targetPosition, _combatMoveSpeed * 1.2f);
            }
            
            // Still shoot while retreating
            if (_allowCombatControl)
            {
                // Aim at target
                _autoAimAngle = CalculateAimAngle();
                _weapon.SetAimAngle(_autoAimAngle);
                
                // Fire at reduced rate while retreating
                if (ShouldFire(0.7f))
                {
                    FireAtTarget();
                }
            }
        }
        
        private void ExecuteStrafing()
        {
            if (_currentTarget == null) return;
            
            // Move perpendicular to target
            if (_allowMovementControl)
            {
                // Direction to target
                Vector2 toTarget = ((Vector2)_currentTarget.position - (Vector2)transform.position).normalized;
                
                // Perpendicular direction (rotate 90 degrees)
                Vector2 strafeDir = new Vector2(-toTarget.y, toTarget.x);
                
                // Flip direction occasionally or if we're about to hit something
                if (_positionUpdateTimer <= 0f || _lastMovementDirection.magnitude < 0.1f)
                {
                    if (RandomChance(0.5f))
                    {
                        strafeDir = -strafeDir;
                    }
                    _positionUpdateTimer = _positionUpdateInterval;
                }
                
                _lastMovementDirection = strafeDir;
                _targetPosition = (Vector2)transform.position + strafeDir * 3f;
                
                MoveToPosition(_targetPosition, _combatMoveSpeed * 0.8f);
            }
            
            // Shoot while strafing
            if (_allowCombatControl)
            {
                // Aim at target
                _autoAimAngle = CalculateAimAngle();
                _weapon.SetAimAngle(_autoAimAngle);
                
                // Fire at slightly reduced accuracy while strafing
                if (ShouldFire())
                {
                    FireAtTarget();
                }
            }
        }
        #endregion
        
        #region Movement & Combat Methods
        
        private void MoveToPosition(Vector2 position, float speed)
        {
            // If direct control is enabled, bypass input system
            if (_useDirectControl)
            {
                DirectMoveToPosition(position, speed);
                return;
            }
            
            // Calculate direction to target position
            Vector2 direction = (position - (Vector2)transform.position).normalized;
            
            // If we're close enough, stop moving
            if (Vector2.Distance(transform.position, position) < 0.1f)
            {
                direction = Vector2.zero;
            }
            
            // Set the movement direction
            _lastMovementDirection = direction;
            
            // We don't directly move here - the ModifyInput method will handle it
        }
        
        // Direct movement control method that bypasses input system
        private void DirectMoveToPosition(Vector2 targetPosition, float speed)
        {
            if (_movement == null) return;
            
            // Calculate direction to target position
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            
            // If we're close enough, stop moving
            if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                direction = Vector2.zero;
            }
            
            try
            {
                // Direct access to the rigidbody
                var rbField = _movement.GetType().GetField("_rigidbody2D", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (rbField != null)
                {
                    Rigidbody2D rb = rbField.GetValue(_movement) as Rigidbody2D;
                    if (rb != null)
                    {
                        rb.linearVelocity = direction * speed;
                        return;
                    }
                }
                
                // Fallback: attempt to use reflection to find movement methods
                DebugLog("Using reflection to find movement methods - direct Rigidbody access failed", LogType.Warning);
            }
            catch (System.Exception ex)
            {
                DebugLog($"Error in direct movement control: {ex.Message}", LogType.Error);
            }
        }
        
        private Vector2 CalculateMoveDirection()
        {
            return _lastMovementDirection;
        }
        
        private bool ShouldFire(float probabilityMultiplier = 1f)
        {
            // Check if we can fire
            if (_fireTimer > 0) return false;
            
            // Check if we have enough energy
            if (_energySystem != null && !_energySystem.HasEnoughEnergy(_energyCostPerShot)) return false;
            
            // Apply probability modifier (used to reduce fire rate in some states)
            if (!RandomChance(probabilityMultiplier)) return false;
            
            return true;
        }
        
        private void FireAtTarget()
        {
            if (_weapon == null || _currentTarget == null) return;
            
            // Calculate the exact angle to the target with no spread
            Vector2 targetPos = _currentTarget.position;
            Vector2 myPos = transform.position;
            Vector2 direction = targetPos - myPos;
            float directAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            DebugLog($"Firing at target: {_currentTarget.name}, Direction: ({direction.x:F2}, {direction.y:F2}), Angle: {directAngle:F1}°", LogType.Info);
            
            if (_useDirectControl)
            {
                if (_botInputData != null)
                {
                    // NETWORK METHOD - We're using input injection in NetworkUpdate
                    // Just log for now - actual firing is handled via input system
                    DebugLog($"BOT FIRING at angle {directAngle:F1}° via network input", LogType.Info);
                }
                else
                {
                    // LEGACY DIRECT METHOD - only works locally
                    bool fired = _weapon.FireDirectly(directAngle, true);
                    
                    if (fired)
                    {
                        DebugLog($"BOT FIRED DIRECTLY at angle {directAngle:F1}°");
                        _fireTimer = _fireInterval;
                    }
                }
            }
            else
            {
                // Use RPC or indirect method
                bool fired = _weapon.FireDirectly(directAngle, true);
                
                if (fired)
                {
                    DebugLog($"BOT FIRED via indirect method at angle {directAngle:F1}°");
                    _fireTimer = _fireInterval;
                }
            }
        }
        #endregion
        
        #region Target Finding
        
        private void FindTarget()
        {
            // Reset current target
            _currentTarget = null;
            
            try
            {
                // Find all potential targets using Physics
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _detectionRadius, _targetLayerMask);
                
                VerboseLog($"Found {hitColliders.Length} colliders in detection radius");
                
                // List of potential targets with scoring
                List<TargetScore> potentialTargets = new List<TargetScore>();
                
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider == null || hitCollider.gameObject == null) continue;
                    
                    // Check if it has the target tag
                    if (!string.IsNullOrEmpty(_targetTag) && !hitCollider.CompareTag(_targetTag))
                    {
                        VerboseLog($"Object {hitCollider.name} has tag '{hitCollider.tag}', expected '{_targetTag}'");
                        continue;
                    }
                    
                    // Calculate distance and score
                    float distance = Vector2.Distance(transform.position, hitCollider.transform.position);
                    
                    // Check line of sight if required
                    if (_requireLineOfSight)
                    {
                        Vector2 direction = hitCollider.transform.position - transform.position;
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, distance, _targetLayerMask);
                        
                        // Skip if something is blocking the view
                        if (hit.collider != hitCollider)
                        {
                            DebugLog($"No line of sight to {hitCollider.name}");
                            continue;
                        }
                    }
                    
                    // Calculate target score (lower is better)
                    float score = CalculateTargetScore(hitCollider.transform, distance);
                    
                    // Add to potential targets
                    potentialTargets.Add(new TargetScore { 
                        Transform = hitCollider.transform,
                        Distance = distance,
                        Score = score
                    });
                }
                
                // Sort by score (lowest first)
                potentialTargets.Sort((a, b) => a.Score.CompareTo(b.Score));
                
                // Select the best target
                if (potentialTargets.Count > 0)
                {
                    _currentTarget = potentialTargets[0].Transform;
                    DebugLog($"New target acquired: {_currentTarget.name} at distance {potentialTargets[0].Distance:F2} with score {potentialTargets[0].Score:F2}");
                }
                else if (_wasTargeting)
                {
                    DebugLog("Lost target");
                }
                
                _wasTargeting = _currentTarget != null;
            }
            catch (Exception ex)
            {
                DebugLog($"Error finding target: {ex.Message}", LogType.Error);
            }
        }
        
        private float CalculateTargetScore(Transform target, float distance)
        {
            // Base score is distance
            float score = distance;
            
            // Add score modifiers based on target properties
            
            // Example: Prioritize already damaged targets
            // if (target.TryGetComponent<Health>(out var health))
            // {
            //     score -= (1 - health.CurrentHealth / health.MaxHealth) * 5f; // Lower score for damaged targets
            // }
            
            // Example: Prioritize targets attacking us
            // if (target.TryGetComponent<Weapon>(out var weapon))
            // {
            //     if (weapon.CurrentTarget == transform)
            //     {
            //         score -= 3f; // Prioritize targets attacking us
            //     }
            // }
            
            return score;
        }
        
        private float CalculateAimAngle(bool precise = false)
        {
            if (_currentTarget == null) 
            {
                // Don't reset to 0 when no target, return current weapon angle instead
                return _weapon != null ? _weapon.Degree : 0f;
            }
            
            // Calculate direction to target
            Vector2 direction = _currentTarget.position - transform.position;
            
            // Convert to angle (degrees)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Add random spread for more natural shooting
            float spread = precise ? _aimSpread * 0.5f : _aimSpread;
            angle += UnityEngine.Random.Range(-spread, spread);
            
            return angle;
        }
        
        // Simple struct to track potential targets
        private struct TargetScore
        {
            public Transform Transform;
            public float Distance;
            public float Score;
        }
        #endregion
        
        #region Utility Methods
        
        // Normalizes angle difference to the -180 to 180 range
        private float NormalizeAngleDifference(float angleDifference)
        {
            // Ensure the angle difference is in the -180 to 180 range
            while (angleDifference > 180f)
                angleDifference -= 360f;
            while (angleDifference < -180f)
                angleDifference += 360f;
            
            return angleDifference;
        }
        
        private bool RandomChance(float probability)
        {
            return UnityEngine.Random.value < probability;
        }
        
        private void CreateVisualIndicators()
        {
            try {
                // Check if the shader exists before attempting to create materials
                Shader defaultShader = Shader.Find("Sprites/Default");
                if (defaultShader == null)
                {
                    // Try to find any available shader as fallback
                    defaultShader = Shader.Find("Standard");
                    
                    if (defaultShader == null)
                    {
                        DebugLog("Could not find any suitable shader for debug visuals - disabling visuals", LogType.Warning);
                        _showGizmos = false;
                        return;
                    }
                }
                
                Material lineMaterial = null;
                try {
                    lineMaterial = new Material(defaultShader);
                }
                catch (System.Exception ex) {
                    DebugLog($"Failed to create line material: {ex.Message} - disabling visuals", LogType.Warning);
                    _showGizmos = false;
                    return;
                }
                
                // Check if aim line renderer already exists
                _aimLine = GetComponent<LineRenderer>();
                if (_aimLine == null)
                {
                    // Create aiming line only if it doesn't exist
                    _aimLine = gameObject.AddComponent<LineRenderer>();
                    if (_aimLine == null)
                    {
                        DebugLog("Failed to add LineRenderer component - disabling visuals", LogType.Warning);
                        _showGizmos = false;
                        return;
                    }
                    
                    _aimLine.startWidth = 0.05f;
                    _aimLine.endWidth = 0.05f;
                    
                    if (lineMaterial != null)
                    {
                        _aimLine.material = lineMaterial;
                    }
                    
                    _aimLine.startColor = Color.red;
                    _aimLine.endColor = Color.yellow;
                    _aimLine.positionCount = 2;
                }
                _aimLine.enabled = false;
                
                // Check if we need a second renderer or can reuse the first
                LineRenderer secondLine = null;
                LineRenderer[] existingRenderers = GetComponents<LineRenderer>();
                
                if (existingRenderers != null && existingRenderers.Length > 1)
                {
                    // Use an existing second renderer
                    secondLine = existingRenderers[1];
                    _movementLine = secondLine;
                }
                else
                {
                    // Skip creating a second LineRenderer if the platform doesn't support it
                    // or if we already have issues with the first one
                    if (existingRenderers == null || existingRenderers.Length == 0 || _aimLine == null)
                    {
                        DebugLog("Skipping second LineRenderer - platform might not support multiple renderers", LogType.Info);
                        return;
                    }
                    
                    // Carefully create the second line renderer
                    try {
                        // Create a separate GameObject for the second line renderer to avoid conflicts
                        GameObject lineObj = new GameObject("MovementLine");
                        lineObj.transform.SetParent(transform);
                        lineObj.transform.localPosition = Vector3.zero;
                        
                        // Create movement line
                        _movementLine = lineObj.AddComponent<LineRenderer>();
                        if (_movementLine == null)
                        {
                            DebugLog("Failed to add second LineRenderer component - proceeding with just one", LogType.Warning);
                        }
                        else
                        {
                            _movementLine.startWidth = 0.05f;
                            _movementLine.endWidth = 0.01f;
                            
                            if (lineMaterial != null)
                            {
                                _movementLine.material = lineMaterial;
                            }
                            
                            _movementLine.startColor = Color.blue;
                            _movementLine.endColor = Color.cyan;
                            _movementLine.positionCount = 2;
                            _movementLine.enabled = false;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        DebugLog($"Failed to set up movement line: {ex.Message} - visual will be limited", LogType.Warning);
                        _movementLine = null;
                    }
                }
                
                DebugLog("Visual indicators initialized successfully");
            }
            catch (System.Exception ex) {
                DebugLog("Error creating visual indicators: " + ex.Message, LogType.Error);
                // Disable visuals to prevent further exceptions
                _showGizmos = false;
            }
        }
        
        private void UpdateVisuals()
        {
            if (!_showGizmos) return;
            
            try
            {
                // Update aiming line
                if (_aimLine != null)
                {
                    if (_currentTarget != null)
                    {
                        _aimLine.enabled = true;
                        _aimLine.SetPosition(0, transform.position);
                        _aimLine.SetPosition(1, _currentTarget.position);
                    }
                    else
                    {
                        _aimLine.enabled = false;
                    }
                }
                
                // Update movement line
                if (_movementLine != null)
                {
                    if (_targetPosition != Vector2.zero && _lastMovementDirection.magnitude > 0.1f)
                    {
                        _movementLine.enabled = true;
                        _movementLine.SetPosition(0, transform.position);
                        _movementLine.SetPosition(1, _targetPosition);
                    }
                    else
                    {
                        _movementLine.enabled = false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                // If visualization fails, just disable it
                _showGizmos = false;
                DebugLog($"Error in visualization, disabling: {ex.Message}", LogType.Warning);
            }
        }
        
        private enum LogType
        {
            Info,
            Warning,
            Error
        }
        
        private void DebugLog(string message, LogType type = LogType.Info)
        {
            if (!_enableDebugLogs) return;
            
            string prefix = "[AutoShooter] ";
            
            switch (type)
            {
                case LogType.Info:
                    Debug.Log(prefix + message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(prefix + message);
                    break;
                case LogType.Error:
                    Debug.LogError(prefix + message);
                    break;
            }
        }
        
        private void VerboseLog(string message)
        {
            if (_enableDebugLogs && _verboseDebugLogs)
            {
                Debug.Log($"[AutoShooter] [VERBOSE] {message}");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            try {
                // Draw detection radius
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, _detectionRadius);
                
                // Draw movement radius around start position
                if (Application.isPlaying)
                {
                    // Only draw the start position if it's been initialized
                    if (_startPosition != Vector2.zero)
                    {
                        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
                        Gizmos.DrawWireSphere(_startPosition, _movementRadius);
                    }
                    
                    // Draw target position
                    if (_targetPosition != Vector2.zero)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(_targetPosition, 0.3f);
                    }
                }
                
                // Draw ideal combat distance
                if (_currentTarget != null)
                {
                    Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
                    Gizmos.DrawWireSphere(_currentTarget.position, _idealCombatDistance);
                    
                    Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.15f);
                    Gizmos.DrawWireSphere(_currentTarget.position, _minimumCombatDistance);
                }
            }
            catch (System.Exception)
            {
                // Silent fail for gizmos - they're just visual aids
                // Disable to prevent future errors
                _showGizmos = false;
            }
        }
        
        public override void NetworkDestroy()
        {
            base.NetworkDestroy();
            
            // Clean up any created visual objects
            if (_movementLine != null && _movementLine.gameObject != gameObject)
            {
                Destroy(_movementLine.gameObject);
            }
        }
        #endregion
    }
} 