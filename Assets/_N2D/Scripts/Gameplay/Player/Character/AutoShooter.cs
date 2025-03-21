using UnityEngine;
using Netick.Unity;
using Netick;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Weapon;
using System;
using System.Reflection;
using StinkySteak.Netick.Timer;  // Add this for TickTimer

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
        
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _verboseDebugLogs = false; // More detailed logs
        
        [Header("Auto-Aim Settings")]
        [SerializeField] private bool _rotateTowardsTarget = true;
        [SerializeField] private float _aimAssistStrength = 0.5f; // 0 = no assist, 1 = full auto-aim
        [SerializeField] private float _playerAimThreshold = 0.1f; // How much player input is considered "actively aiming"
        [SerializeField] private float _aimSpeed = 5f; // How fast to rotate towards target
        
        // Internal references
        private PlayerCharacterWeapon _weapon;
        private Transform _currentTarget;
        private float _targetCheckTimer;
        private float _fireTimer;  // Simple non-networked timer
        private bool _wasTargeting = false;
        
        // Visual indicator for aiming
        private LineRenderer _aimLine;
        
        // Store auto shooting state
        private float _autoAimAngle = 0f; // Store aim direction
        
        // Track original inputs
        private bool _playerIsAlreadyFiring = false;
        
        // Track recent player input for aim assistance
        private float _lastPlayerAimInput = 0f;
        private float _timeSincePlayerAimed = 0f;
        
        public override void NetworkStart()
        {
            base.NetworkStart();
            
            // Get the weapon component
            _weapon = GetComponent<PlayerCharacterWeapon>();
            if (_weapon == null)
            {
                DebugLog("No PlayerCharacterWeapon found on this object! AutoShooter will not work.");
                enabled = false;
                return;
            }
            
            // Create aiming line indicator if debug is enabled
            if (_showGizmos)
            {
                _aimLine = gameObject.AddComponent<LineRenderer>();
                _aimLine.startWidth = 0.05f;
                _aimLine.endWidth = 0.05f;
                _aimLine.material = new Material(Shader.Find("Sprites/Default"));
                _aimLine.startColor = Color.red;
                _aimLine.endColor = Color.yellow;
                _aimLine.positionCount = 2;
                _aimLine.enabled = false;
            }
            
            DebugLog("AutoShooter initialized successfully");
        }
        
        public override void NetworkFixedUpdate()
        {
            // Only process on server
            if (!IsServer) return;
            
            // Now we only need to update the target check timer
            _targetCheckTimer -= Sandbox.FixedDeltaTime;
            
            // Find target periodically to save performance
            if (_targetCheckTimer <= 0)
            {
                FindTarget();
                _targetCheckTimer = _targetCheckInterval;
            }
            
            // Try to fire directly at the target if we have one
            if (_currentTarget != null)
            {
                // Create a private firing timer to control fire rate
                if (_fireTimer <= 0)
                {
                    DirectFire();
                    _fireTimer = _fireInterval;
                }
                else
                {
                    _fireTimer -= Sandbox.FixedDeltaTime;
                }
            }
            
            // Update the debug line
            UpdateVisuals();
        }
        
        private void FindTarget()
        {
            // Reset current target
            _currentTarget = null;
            
            try
            {
                // Find all potential targets using Physics
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _detectionRadius, _targetLayerMask);
                
                VerboseLog($"Found {hitColliders.Length} colliders in detection radius");
                
                // Find the closest valid target
                float closestDistance = float.MaxValue;
                
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider == null || hitCollider.gameObject == null) continue;
                    
                    // Check if it has the target tag (if tag is specified)
                    if (!string.IsNullOrEmpty(_targetTag) && !hitCollider.CompareTag(_targetTag))
                    {
                        VerboseLog($"Object {hitCollider.name} has tag '{hitCollider.tag}', expected '{_targetTag}'");
                        continue;
                    }
                    
                    // Calculate distance
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
                    
                    // If this target is closer than the current closest, select it
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        _currentTarget = hitCollider.transform;
                    }
                }
                
                if (_currentTarget != null)
                {
                    DebugLog($"New target acquired: {_currentTarget.name} at distance {closestDistance:F2}");
                }
                else if (_wasTargeting)
                {
                    DebugLog("Lost target");
                }
                
                _wasTargeting = _currentTarget != null;
            }
            catch (Exception ex)
            {
                DebugLog($"Error finding target: {ex.Message}");
            }
        }
        
        // Hook into the player's input system in a non-intrusive way
        // This method will be called by the player's input system
        public bool ModifyInput(ref PlayerCharacterInput input)
        {
            // We should only modify input on the server for consistency
            if (!IsServer) return false;
            
            // Store original input
            _playerIsAlreadyFiring = input.IsFiring;
            bool wasModified = false;
            
            // No target, no auto-shooting
            if (_currentTarget == null) return false;
            
            // Calculate ideal auto-aim angle
            _autoAimAngle = CalculateAimAngle();
            
            // Determine if player is actively aiming
            bool isPlayerAiming = Mathf.Abs(input.LookDegree - _lastPlayerAimInput) > _playerAimThreshold;
            
            if (isPlayerAiming)
            {
                // Player is actively providing aim input, update tracking
                _lastPlayerAimInput = input.LookDegree;
                _timeSincePlayerAimed = 0f;
                
                if (_rotateTowardsTarget && _aimAssistStrength > 0f)
                {
                    float originalAngle = input.LookDegree;
                    
                    // Apply subtle aim assist - gently pull toward the target
                    // Calculate the difference between player aim and auto aim
                    float aimDifference = NormalizeAngleDifference(_autoAimAngle - input.LookDegree);
                    
                    // Apply a small pull toward the target based on aim assist strength
                    float assistedAngle = input.LookDegree + (aimDifference * _aimAssistStrength * Sandbox.FixedDeltaTime * _aimSpeed);
                    input.LookDegree = assistedAngle;
                    wasModified = true;
                    
                    DebugLog($"Applied aim assist: Player={originalAngle:F1}° -> Assisted={assistedAngle:F1}° (Target={_autoAimAngle:F1}°)");
                }
            }
            else
            {
                // Player isn't actively aiming
                _timeSincePlayerAimed += Sandbox.FixedDeltaTime;
                
                if (_rotateTowardsTarget)
                {
                    float originalAngle = input.LookDegree;
                    
                    // Player isn't actively aiming, use full auto-aim
                    // Just set directly to the target angle
                    input.LookDegree = _autoAimAngle;
                    wasModified = true;
                    
                    DebugLog($"FULL AUTO-AIM: {originalAngle:F1}° -> {_autoAimAngle:F1}°");
                }
            }
            
            // If player is not already firing, auto-fire
            if (!input.IsFiring)
            {
                // Always set IsFiring = true when we have a target
                // The weapon's own fire rate control will handle how fast it can actually fire
                input.IsFiring = true;
                wasModified = true;
                
                DebugLog($"Auto-firing at angle {input.LookDegree:F1}°");
            }
            
            return wasModified;
        }
        
        private float CalculateAimAngle()
        {
            if (_currentTarget == null) return 0f;
            
            // Calculate direction to target
            Vector2 direction = _currentTarget.position - transform.position;
            
            // Convert to angle (degrees)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Add random spread for more natural shooting
            angle += UnityEngine.Random.Range(-_aimSpread, _aimSpread);
            
            return angle;
        }
        
        private void UpdateVisuals()
        {
            // Update aiming line if debug is enabled
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
        }
        
        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[AutoShooter] {message}");
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
            
            // Draw detection radius
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
            
            // Draw line to current target
            if (Application.isPlaying && _currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
        
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
        
        // Direct firing method that bypasses normal weapon firing
        private void DirectFire()
        {
            if (_currentTarget == null || _weapon == null || !IsServer) return;
            
            try
            {
                // Calculate aim direction to target
                Vector2 targetDirection = (_currentTarget.position - transform.position).normalized;
                float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                
                // Add random spread for more natural shooting
                targetAngle += UnityEngine.Random.Range(-_aimSpread, _aimSpread);
                
                DebugLog($"DIRECT FIRING at angle {targetAngle:F1}°, direction: {targetDirection}");
                
                // Calculate origin point (same logic as in PlayerCharacterWeapon)
                float weaponOffset = 1.5f; // Default value, try to get the real one if possible
                try
                {
                    // Try to get the weapon's offset value via reflection
                    var offsetField = _weapon.GetType().GetField("_weaponOriginPointOffset", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (offsetField != null)
                    {
                        weaponOffset = (float)offsetField.GetValue(_weapon);
                    }
                }
                catch (Exception) { /* Ignore errors and use default */ }
                
                Vector2 originPoint = (Vector2)transform.position + (targetDirection * weaponOffset);
                
                // Perform the raycast directly
                RaycastHit2D hit = Physics2D.Raycast(originPoint, targetDirection, 50f, _targetLayerMask);
                
                if (hit.collider != null)
                {
                    DebugLog($"DIRECT HIT on {hit.collider.name} at {hit.point}");
                    
                    // If it hit a player, apply damage
                    if (hit.collider.TryGetComponent<Health.PlayerCharacterHealth>(out var health))
                    {
                        // Try to get the damage value from the weapon
                        int damage = 10; // Default damage
                        try
                        {
                            var damageField = _weapon.GetType().GetField("_damage", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (damageField != null)
                            {
                                damage = (int)damageField.GetValue(_weapon);
                            }
                        }
                        catch (Exception) { /* Use default damage */ }
                        
                        health.DeductShieldAndHealth(damage, transform);
                        DebugLog($"Applied {damage} damage to {hit.collider.name}");
                    }
                    
                    // Update the weapon's ProjectileHit for visual effects
                    var hitField = _weapon.GetType().GetField("_lastProjectileHit", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (hitField != null)
                    {
                        // Create a ProjectileHit struct
                        // We need to use reflection to create it since it's not directly accessible
                        var hitType = hitField.FieldType;
                        var hitObj = Activator.CreateInstance(hitType);
                        
                        // Set the properties
                        var tickProp = hitType.GetProperty("Tick");
                        var hitPosProp = hitType.GetProperty("HitPosition");
                        var originPosProp = hitType.GetProperty("OriginPosition");
                        var isHitPlayerProp = hitType.GetProperty("IsHitPlayer");
                        
                        if (tickProp != null) tickProp.SetValue(hitObj, Sandbox.Tick.TickValue);
                        if (hitPosProp != null) hitPosProp.SetValue(hitObj, hit.point);
                        if (originPosProp != null) originPosProp.SetValue(hitObj, originPoint);
                        if (isHitPlayerProp != null) isHitPlayerProp.SetValue(hitObj, health != null);
                        
                        // Set the field
                        hitField.SetValue(_weapon, hitObj);
                        
                        // Try to invoke the OnLastProjectileHitChanged event
                        var eventField = _weapon.GetType().GetField("OnLastProjectileHitChanged", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (eventField != null)
                        {
                            var eventObj = eventField.GetValue(_weapon) as Action;
                            eventObj?.Invoke();
                        }
                    }
                }
                else
                {
                    // Missed, create a fake hit point
                    Vector2 fakeHitPosition = originPoint + (targetDirection * 1000f);
                    DebugLog($"DIRECT MISS, fake hit point at {fakeHitPosition}");
                    
                    // Same code as above to update the weapon's ProjectileHit
                    var hitField = _weapon.GetType().GetField("_lastProjectileHit", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (hitField != null)
                    {
                        var hitType = hitField.FieldType;
                        var hitObj = Activator.CreateInstance(hitType);
                        
                        var tickProp = hitType.GetProperty("Tick");
                        var hitPosProp = hitType.GetProperty("HitPosition");
                        var originPosProp = hitType.GetProperty("OriginPosition");
                        var isHitPlayerProp = hitType.GetProperty("IsHitPlayer");
                        
                        if (tickProp != null) tickProp.SetValue(hitObj, Sandbox.Tick.TickValue);
                        if (hitPosProp != null) hitPosProp.SetValue(hitObj, fakeHitPosition);
                        if (originPosProp != null) originPosProp.SetValue(hitObj, originPoint);
                        if (isHitPlayerProp != null) isHitPlayerProp.SetValue(hitObj, false);
                        
                        hitField.SetValue(_weapon, hitObj);
                        
                        var eventField = _weapon.GetType().GetField("OnLastProjectileHitChanged", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (eventField != null)
                        {
                            var eventObj = eventField.GetValue(_weapon) as Action;
                            eventObj?.Invoke();
                        }
                    }
                }
                
                // Deduct energy
                try
                {
                    var energySystemField = _weapon.GetType().GetField("_energySystem", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (energySystemField != null)
                    {
                        var energySystem = energySystemField.GetValue(_weapon);
                        if (energySystem != null)
                        {
                            float energyCost = 10f; // Default cost
                            var costField = _weapon.GetType().GetField("_energyCostPerShot", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (costField != null)
                            {
                                energyCost = (float)costField.GetValue(_weapon);
                            }
                            
                            var deductMethod = energySystem.GetType().GetMethod("DeductEnergy");
                            if (deductMethod != null)
                            {
                                deductMethod.Invoke(energySystem, new object[] { energyCost });
                                DebugLog($"Deducted {energyCost} energy");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Error deducting energy: {ex.Message}");
                }
                
                // Reset the weapon's fire timer
                try
                {
                    var timerField = _weapon.GetType().GetField("_timerFireRate", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (timerField != null)
                    {
                        float fireRate = 0.5f; // Default rate
                        var rateField = _weapon.GetType().GetField("_fireRate", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (rateField != null)
                        {
                            fireRate = (float)rateField.GetValue(_weapon);
                        }
                        
                        var timer = TickTimer.CreateFromSeconds(_weapon.Sandbox, fireRate);
                        timerField.SetValue(_weapon, timer);
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Error resetting fire timer: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error in DirectFire: {ex.Message}");
            }
        }
    }
} 