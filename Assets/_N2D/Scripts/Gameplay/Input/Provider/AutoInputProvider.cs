using Netick.Unity;
using StinkySteak.N2D.Gameplay.Player.Character;
using StinkySteak.N2D.Gameplay.PlayerManager.LocalPlayer;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace StinkySteak.N2D.Gameplay.PlayerInput
{
    /// <summary>
    /// Automated input provider that simulates player behavior without human input
    /// </summary>
    public class AutoInputProvider : NetworkEventsListener
    {
        [Header("Auto Player Settings")]
        [SerializeField] private string _targetTag = "NPC";
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private LayerMask _targetLayerMask = -1;
        [SerializeField] private bool _requireLineOfSight = true;

        [Header("Combat Settings")]
        [SerializeField] private float _fireInterval = 0.3f;
        [SerializeField] private float _aimSpread = 2f;
        
        [Header("Movement Settings")]
        // Reserved for future use
        // [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _wanderRadius = 10f;
        [SerializeField] private float _wanderTimer = 2f;
        [SerializeField] private float _strafingChance = 0.3f;
        [SerializeField] private float _idealCombatDistance = 5f;
        
        [Header("Shield/Skill Settings")]
        [SerializeField] private float _shieldActivationChance = 0.1f;
        [SerializeField] private float _skillCooldown = 5f;
        
        // Player references
        private PlayerCharacter _localPlayer;
        private Transform _currentTarget;
        
        // Timers and state
        private float _fireTimer;
        private float _targetSearchTimer = 0.2f;
        private float _wanderChangeTimer;
        private float _shieldTimer;
        private Vector2 _moveDirection = Vector2.zero;
        private Vector2 _targetWanderPosition;
        private bool _isStrafing = false;
        
        // Debug
        private bool _debug = true;

        public override void OnStartup(NetworkSandbox sandbox)
        {
            if (sandbox.TryGetComponent(out LocalPlayerManager localPlayerManager))
            {
                localPlayerManager.OnCharacterSpawned += OnLocalPlayerSpawned;
            }
            
            // Initialize state
            _wanderChangeTimer = _wanderTimer;
            _targetWanderPosition = Random.insideUnitCircle * _wanderRadius;
            _shieldTimer = _skillCooldown;
            
            if (_debug) Debug.Log("[AutoInputProvider] Started - will automatically control player");
        }

        private void OnLocalPlayerSpawned(PlayerCharacter playerCharacter)
        {
            _localPlayer = playerCharacter;
            if (_debug) Debug.Log("[AutoInputProvider] Player character registered: " + _localPlayer.name);
        }

        public override void OnInput(NetworkSandbox sandbox)
        {
            // Create new input instance
            PlayerCharacterInput input = new PlayerCharacterInput();
            
            // If we don't have a player reference, just submit empty input
            if (_localPlayer == null)
            {
                sandbox.SetInput(input);
                return;
            }

            // Update timers
            UpdateTimers(sandbox.DeltaTime);
            
            // Find and update target
            if (_targetSearchTimer <= 0)
            {
                FindTarget();
                _targetSearchTimer = 0.2f;
            }
            
            // Compute movement
            CalculateMovement(ref input);
            
            // Calculate aim and firing
            CalculateAimAndFiring(ref input);
            
            // Decide on skills
            DecideSpecialActions(ref input);
            
            // Submit the input
            sandbox.SetInput(input);
        }
        
        private void UpdateTimers(float deltaTime)
        {
            _fireTimer -= deltaTime;
            _targetSearchTimer -= deltaTime;
            _wanderChangeTimer -= deltaTime;
            _shieldTimer -= deltaTime;
            
            // Change direction periodically when wandering
            if (_wanderChangeTimer <= 0)
            {
                _wanderChangeTimer = _wanderTimer;
                _targetWanderPosition = (Vector2)_localPlayer.transform.position + (Random.insideUnitCircle * _wanderRadius);
                _isStrafing = (Random.value < _strafingChance);
                if (_debug) Debug.Log($"[AutoInputProvider] New wander target: {_targetWanderPosition}");
            }
        }
        
        private void FindTarget()
        {
            // Cache the previous target
            Transform previousTarget = _currentTarget;
            _currentTarget = null;
            
            // If player doesn't exist yet, skip target finding
            if (_localPlayer == null) return;
            
            try
            {
                // Find all potential targets in radius
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(_localPlayer.transform.position, _detectionRadius, _targetLayerMask);
                if (_debug) Debug.Log($"[AutoInputProvider] Found {hitColliders.Length} potential targets");
                
                // Simple target scoring list
                List<TargetScore> potentialTargets = new List<TargetScore>();
                
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider == null || hitCollider.gameObject == null) continue;
                    
                    string objName = hitCollider.name;
                    string objTag = hitCollider.tag;
                    
                    // Check if it has the right tag or name
                    bool isValidTarget = string.IsNullOrEmpty(_targetTag) || 
                                        hitCollider.CompareTag(_targetTag) ||
                                        objName.Contains("Enemy") ||
                                        objName.Contains("NPC") ||
                                        objTag.Contains("Enemy") ||
                                        objTag.Contains("NPC");
                    
                    if (!isValidTarget) continue;
                    
                    // Distance check
                    float distance = Vector2.Distance(_localPlayer.transform.position, hitCollider.transform.position);
                    
                    // Line of sight check if required
                    if (_requireLineOfSight)
                    {
                        Vector2 direction = hitCollider.transform.position - _localPlayer.transform.position;
                        RaycastHit2D hit = Physics2D.Raycast(_localPlayer.transform.position, direction.normalized, distance, _targetLayerMask);
                        
                        if (hit.collider != hitCollider) continue;
                    }
                    
                    // Add to potential targets with simple distance score
                    potentialTargets.Add(new TargetScore {
                        Transform = hitCollider.transform,
                        Distance = distance,
                        Score = distance // Simple scoring just based on distance
                    });
                }
                
                // Find the best target based on score
                if (potentialTargets.Count > 0)
                {
                    // Sort by score (lowest is best)
                    potentialTargets.Sort((a, b) => a.Score.CompareTo(b.Score));
                    _currentTarget = potentialTargets[0].Transform;
                    
                    if (_currentTarget != previousTarget && _debug)
                    {
                        Debug.Log($"[AutoInputProvider] New target acquired: {_currentTarget.name} at distance {potentialTargets[0].Distance:F2}");
                    }
                }
                else if (previousTarget != null && _debug)
                {
                    Debug.Log("[AutoInputProvider] Lost target, no valid targets found");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoInputProvider] Error finding target: {ex.Message}");
            }
        }
        
        private void CalculateMovement(ref PlayerCharacterInput input)
        {
            if (_currentTarget != null)
            {
                // We have a target, so do combat movement
                Vector2 dirToTarget = (_currentTarget.position - _localPlayer.transform.position);
                float distanceToTarget = dirToTarget.magnitude;
                
                if (_isStrafing)
                {
                    // Strafe perpendicular to target
                    Vector2 perpDir = new Vector2(-dirToTarget.y, dirToTarget.x).normalized;
                    if (Random.value < 0.5f) perpDir = -perpDir; // Randomly flip direction
                    
                    input.HorizontalMove = perpDir.x;
                    input.VerticalMove = perpDir.y;
                }
                else if (distanceToTarget < _idealCombatDistance - 1f)
                {
                    // Too close, back up
                    input.HorizontalMove = -dirToTarget.normalized.x;
                    input.VerticalMove = -dirToTarget.normalized.y;
                }
                else if (distanceToTarget > _idealCombatDistance + 1f)
                {
                    // Too far, move closer
                    input.HorizontalMove = dirToTarget.normalized.x;
                    input.VerticalMove = dirToTarget.normalized.y;
                }
                else
                {
                    // At good distance, just make small movements
                    Vector2 smallMove = Random.insideUnitCircle * 0.3f;
                    input.HorizontalMove = smallMove.x;
                    input.VerticalMove = smallMove.y;
                }
            }
            else
            {
                // No target, wander around
                Vector2 wanderDir = (_targetWanderPosition - (Vector2)_localPlayer.transform.position);
                float distToWanderTarget = wanderDir.magnitude;
                
                if (distToWanderTarget > 0.5f)
                {
                    // Move toward wander position
                    wanderDir.Normalize();
                    input.HorizontalMove = wanderDir.x;
                    input.VerticalMove = wanderDir.y;
                }
                else
                {
                    // Reached wander position, get a new one
                    _wanderChangeTimer = 0f;
                    input.HorizontalMove = 0;
                    input.VerticalMove = 0;
                }
            }
        }
        
        private void CalculateAimAndFiring(ref PlayerCharacterInput input)
        {
            if (_currentTarget != null)
            {
                // Calculate direction to target
                Vector2 dirToTarget = (_currentTarget.position - _localPlayer.transform.position);
                
                // Convert to angle (degrees)
                float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
                
                // Add some randomness for more human-like aim
                float randomSpread = Random.Range(-_aimSpread, _aimSpread);
                angle += randomSpread;
                
                // Set the aim
                input.LookDegree = angle;
                
                // Decide on firing
                if (_fireTimer <= 0)
                {
                    input.IsFiring = true;
                    _fireTimer = _fireInterval;
                }
                else
                {
                    input.IsFiring = false;
                }
            }
            else
            {
                // No target, look in movement direction or randomly
                Vector2 lookDir = new Vector2(input.HorizontalMove, input.VerticalMove);
                
                if (lookDir.magnitude > 0.1f)
                {
                    // Look in movement direction
                    input.LookDegree = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                }
                else
                {
                    // Look in random direction
                    input.LookDegree = Random.Range(0, 360f);
                }
                
                // Don't fire without a target
                input.IsFiring = false;
            }
        }
        
        private void DecideSpecialActions(ref PlayerCharacterInput input)
        {
            // Jump/dash randomly or to escape
            if (_currentTarget != null)
            {
                Vector2 dirToTarget = (_currentTarget.position - _localPlayer.transform.position);
                float distanceToTarget = dirToTarget.magnitude;
                
                if (distanceToTarget < 3f && Random.value < 0.2f)
                {
                    // Too close, dash to escape
                    input.Jump = true;
                }
                else
                {
                    // Normal dash behavior, occasional random dash
                    input.Jump = Random.value < 0.05f;
                }
            }
            else
            {
                // Occasional dash when wandering
                input.Jump = Random.value < 0.02f;
            }
            
            // Shield activation
            if (_shieldTimer <= 0 && Random.value < _shieldActivationChance)
            {
                input.ActivateRegenerativeShield = true;
                _shieldTimer = _skillCooldown;
            }
            else
            {
                input.ActivateRegenerativeShield = false;
            }
            
            // Sometimes use laser ability
            input.ActivateLaser = _currentTarget != null && Random.value < 0.05f;
            
            // Sometimes set target position for right-click movement
            if (Random.value < 0.01f)
            {
                Vector2 randomPos = (Vector2)_localPlayer.transform.position + (Random.insideUnitCircle * 5f);
                input.TargetPosition = randomPos;
            }
            else
            {
                input.TargetPosition = Vector2.zero;
            }
        }
        
        // Utility struct for target tracking
        private struct TargetScore
        {
            public Transform Transform;
            public float Distance;
            public float Score;
        }
    }
} 