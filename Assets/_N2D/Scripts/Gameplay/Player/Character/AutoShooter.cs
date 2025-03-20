using UnityEngine;
using Netick.Unity;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Weapon;
using System.Reflection;

namespace StinkySteak.N2D.Gameplay.Player.Character
{
    public class AutoShooter : NetworkBehaviour
    {
        [Header("Target Detection")]
        [SerializeField] private string _targetTag = "NPC";
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private LayerMask _targetLayerMask = -1;
        [SerializeField] private bool _requireLineOfSight = true;
        
        [Header("Shooting")]
        [SerializeField] private float _aimSpread = 5f;
        [SerializeField] private float _targetCheckInterval = 0.2f; // How often to look for targets (seconds)
        [SerializeField] private float _fireInterval = 0.5f; // Time between shots
        
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _directProcessShootingCall = true; // Directly call weapon's shooting method
        
        // Internal references
        private PlayerCharacterWeapon _weapon;
        private PlayerCharacterWeaponVisual _weaponVisual;
        private Transform _currentTarget;
        private float _targetCheckTimer;
        private float _fireTimer;
        
        // Reflection for direct access to weapon methods
        private MethodInfo _processShootingMethod;
        private MethodInfo _fetchInputMethod;
        private PropertyInfo _degreeProperty;
        
        // Visual indicator for aiming
        private LineRenderer _aimLine;
        private bool _wasTargeting = false;
        
        public override void NetworkStart()
        {
            base.NetworkStart();
            
            // Get the weapon component
            _weapon = GetComponent<PlayerCharacterWeapon>();
            if (_weapon == null)
            {
                DebugLog("No PlayerCharacterWeapon found on this object!");
                enabled = false;
                return;
            }
            
            // Get the weapon visual for debugging
            _weaponVisual = GetComponentInChildren<PlayerCharacterWeaponVisual>();
            
            // Use reflection to get private methods we need to call directly
            // This is a bit of a hack but necessary to ensure shooting works
            _degreeProperty = _weapon.GetType().GetProperty("Degree");
            
            if (_directProcessShootingCall)
            {
                _processShootingMethod = _weapon.GetType().GetMethod("ProcessShooting", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (_processShootingMethod == null)
                {
                    DebugLog("WARNING: Could not find ProcessShooting method - direct shooting will not work");
                }
                else
                {
                    DebugLog("Successfully found ProcessShooting method");
                }
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
            // Update timers
            _targetCheckTimer -= Sandbox.FixedDeltaTime;
            _fireTimer -= Sandbox.FixedDeltaTime;
            
            // Find target periodically to save performance
            if (_targetCheckTimer <= 0)
            {
                FindTarget();
                _targetCheckTimer = _targetCheckInterval;
            }
            
            // Auto-aim and fire
            UpdateAimAndFire();
        }
        
        private void FindTarget()
        {
            // Reset current target
            _currentTarget = null;
            
            try
            {
                // Find all potential targets using Physics
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _detectionRadius, _targetLayerMask);
                
                DebugLog($"Found {hitColliders.Length} colliders in detection radius");
                
                // Find the closest valid target
                float closestDistance = float.MaxValue;
                
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider == null || hitCollider.gameObject == null) continue;
                    
                    // Check if it has the target tag (if tag is specified)
                    if (!string.IsNullOrEmpty(_targetTag) && !hitCollider.CompareTag(_targetTag))
                    {
                        DebugLog($"Object {hitCollider.name} has tag '{hitCollider.tag}', expected '{_targetTag}'");
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
            catch (System.Exception ex)
            {
                DebugLog($"Error finding target: {ex.Message}");
            }
        }
        
        private void UpdateAimAndFire()
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
            
            // If no target, make sure we're not firing
            if (_currentTarget == null)
            {
                // Clear firing flag through input
                if (FetchInput(out PlayerCharacterInput input))
                {
                    input.IsFiring = false;
                }
                return;
            }
            
            try
            {
                // Calculate aim direction
                Vector2 direction = _currentTarget.position - transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                
                // Add randomness for natural shooting
                angle += Random.Range(-_aimSpread, _aimSpread);
                
                // Method 1: Set input values through Netick's input system
                if (FetchInput(out PlayerCharacterInput input))
                {
                    input.LookDegree = angle;
                    input.IsFiring = true;
                    DebugLog($"Aiming at angle: {angle:F1}°, Firing: true");
                }
                
                // Method 2: Direct weapon property setting (if allowed)
                if (Sandbox.IsServer && _degreeProperty != null && _degreeProperty.CanWrite)
                {
                    _degreeProperty.SetValue(_weapon, angle);
                }
                
                // Method 3: Direct ProcessShooting call (most reliable)
                if (_directProcessShootingCall && 
                    Sandbox.IsServer && 
                    _processShootingMethod != null && 
                    _fireTimer <= 0)
                {
                    _processShootingMethod.Invoke(_weapon, null);
                    _fireTimer = _fireInterval;
                    DebugLog("Directly called ProcessShooting method");
                }
            }
            catch (System.Exception ex)
            {
                DebugLog($"Error in UpdateAimAndFire: {ex.Message}");
            }
        }
        
        // Helper method to directly call weapon's ProcessShooting method
        private void ForceShoot()
        {
            if (_processShootingMethod != null)
            {
                try
                {
                    _processShootingMethod.Invoke(_weapon, null);
                    DebugLog("ForceShoot called successfully");
                }
                catch (System.Exception ex)
                {
                    DebugLog($"Error in ForceShoot: {ex.Message}");
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
    }
} 