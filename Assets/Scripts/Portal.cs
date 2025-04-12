using Netick;
using Netick.Unity;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StinkySteak.N2D.Finder;

public class Portal : NetworkBehaviour
{
    [Header("Portal Settings")]
    public int portalId;
    public int destinationPortalId;
    public Color portalColor = Color.blue;
    [SerializeField] private float cooldownDuration = 2f; // Time in seconds before an object can teleport again
    
    [Header("References")]
    [SerializeField] private SpriteRenderer _portalRenderer;
    
    // Default value for IsActive when not initialized through network
    private bool _defaultActiveState = true;
    
    // Static dictionary to track cooldowns across all portals (object ID -> time when cooldown expires)
    private static Dictionary<int, float> portalCooldowns = new Dictionary<int, float>();
    
    [Networked] public NetworkBool IsActive { get; set; }
    
    private bool _hasRegistered = false;
    private bool _isNetworkInitialized = false;
    private bool _isServer = false; // Cache the server state
    private float _lastRegistrationAttempt = 0f;
    private const float REGISTRATION_RETRY_INTERVAL = 0.5f;
    
    // Check if an object is on cooldown
    private bool IsObjectOnCooldown(int objectId)
    {
        if (portalCooldowns.TryGetValue(objectId, out float cooldownEndTime))
        {
            if (Time.time < cooldownEndTime)
            {
                float remainingTime = cooldownEndTime - Time.time;
                Debug.Log($"[Portal {portalId}] Object {objectId} is on cooldown for another {remainingTime:F1} seconds");
                return true;
            }
        }
        return false;
    }
    
    // Set cooldown for an object
    private void SetObjectCooldown(int objectId)
    {
        portalCooldowns[objectId] = Time.time + cooldownDuration;
        Debug.Log($"[Portal {portalId}] Set cooldown for object {objectId} for {cooldownDuration} seconds");
    }
    
    // Safe accessor for IsActive that handles null reference cases
    private bool GetIsActive()
    {
        if (!_isNetworkInitialized)
        {
            return _defaultActiveState;
        }
        
        try
        {
            return IsActive;
        }
        catch (System.NullReferenceException)
        {
            Debug.LogWarning($"[Portal {portalId}] IsActive property not initialized properly, using default");
            return _defaultActiveState;
        }
    }
    
    private void Start()
    {
        // Make sure we have a collider set up properly
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError($"[Portal {portalId}] No Collider2D component found! Adding a CircleCollider2D.");
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 1.0f;
            circleCollider.isTrigger = true;
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning($"[Portal {portalId}] Collider is not set as a trigger! Setting isTrigger = true.");
            collider.isTrigger = true;
        }
        
        Debug.Log($"[Portal {portalId}] Initialized with destination: {destinationPortalId}");
    }
    
    public override void NetworkStart()
    {
        base.NetworkStart();
        
        _isNetworkInitialized = true;
        
        // Check if this is running on the server
        _isServer = Sandbox.IsServer;
        Debug.Log($"[Portal {portalId}] Direct server check: Sandbox.IsServer={Sandbox.IsServer}");
        
        // Initialize the IsActive property on the server
        if (_isServer)
        {
            try
            {
                IsActive = _defaultActiveState;
                Debug.Log($"[Portal {portalId}] Set IsActive to {_defaultActiveState}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Portal {portalId}] Error setting IsActive: {ex.Message}");
            }
        }
        
        Debug.Log($"[Portal {portalId}] NetworkStart called. IsServer: {_isServer}, IsActive: {GetIsActive()}");
        UpdateVisuals();
        
        // Start trying to register with the manager
        StartCoroutine(RegisterWithManager());
    }
    
    private IEnumerator RegisterWithManager()
    {
        Debug.Log($"[Portal {portalId}] Starting registration attempts with PortalManager");
        
        int attempts = 0;
        // Keep trying until we successfully register
        while (!_hasRegistered && _isServer)
        {
            attempts++;
            
            // Check if the manager is available
            if (PortalManager.Instance != null)
            {
                PortalManager.Instance.RegisterPortal(this);
                _hasRegistered = true;
                Debug.Log($"[Portal {portalId}] Successfully registered with manager after {attempts} attempts");
            }
            else
            {
                Debug.Log($"[Portal {portalId}] PortalManager singleton not found (attempt {attempts}). Retrying in {REGISTRATION_RETRY_INTERVAL}s");
                // Wait a bit before trying again
                yield return new WaitForSeconds(REGISTRATION_RETRY_INTERVAL);
            }
        }
    }
    
    // Try manual registration in fixed update as a backup
    public override void NetworkFixedUpdate()
    {
        if (_isServer && !_hasRegistered && Time.time > _lastRegistrationAttempt + REGISTRATION_RETRY_INTERVAL)
        {
            _lastRegistrationAttempt = Time.time;
            
            if (PortalManager.Instance != null)
            {
                PortalManager.Instance.RegisterPortal(this);
                _hasRegistered = true;
                Debug.Log($"[Portal {portalId}] Successfully registered with manager during FixedUpdate");
            }
        }
    }
    
    [OnChanged(nameof(IsActive))]
    private void OnActiveStateChanged(OnChangedData data)
    {
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (_portalRenderer != null)
        {
            _portalRenderer.color = GetIsActive() ? portalColor : Color.gray;
        }
        else
        {
            Debug.LogWarning($"[Portal {portalId}] No renderer assigned to show portal state");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        try
        {
            // Log all collisions with the portal
            string objectName = other?.gameObject?.name ?? "Unknown";
            Debug.Log($"[Portal {portalId}] OnTriggerEnter2D: Object entered portal trigger: {objectName}");
            
            if (other?.gameObject != null)
            {
                HandleCollision(other.gameObject);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Portal {portalId}] Error in OnTriggerEnter2D: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    // Backup collision detection
    private void OnTriggerStay2D(Collider2D other)
    {
        try
        {
            // Only check occasionally to avoid spam
            if (Time.frameCount % 30 == 0 && other?.gameObject != null)
            {
                Debug.Log($"[Portal {portalId}] OnTriggerStay2D: Object staying in portal trigger: {other.gameObject.name}");
                HandleCollision(other.gameObject);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Portal {portalId}] Error in OnTriggerStay2D: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    // Even more backup collision detection
    private void OnCollisionEnter2D(Collision2D collision)
    {
        try
        {
            if (collision?.gameObject != null) 
            {
                Debug.Log($"[Portal {portalId}] OnCollisionEnter2D: Object collided with portal: {collision.gameObject.name}");
                HandleCollision(collision.gameObject);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Portal {portalId}] Error in OnCollisionEnter2D: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private bool IsNPCOrEnemy(GameObject gameObject)
    {
        // Check by tag if possible
        try
        {
            if (gameObject.tag == "NPC" || gameObject.tag == "Enemy")
                return true;
        }
        catch (System.Exception) { }
        
        // Also check by name pattern if tags are not set
        string objName = gameObject.name.ToLower();
        return objName.Contains("npc") || 
               objName.Contains("enemy") || 
               objName.Contains("monster") || 
               objName.Contains("creature");
    }
    
    private void HandleCollision(GameObject collidingObject)
    {
        if (collidingObject == null)
        {
            Debug.LogWarning($"[Portal {portalId}] Received null colliding object");
            return;
        }
        
        // Check if the portal is active using the safe accessor
        if (!GetIsActive())
        {
            Debug.Log($"[Portal {portalId}] Portal is inactive, ignoring collision with {collidingObject.name}");
            return;
        }
        
        // Skip NPC/Enemy objects 
        if (IsNPCOrEnemy(collidingObject))
        {
            Debug.Log($"[Portal {portalId}] Ignoring NPC/Enemy object: {collidingObject.name}");
            return;
        }
        
        try
        {
            // Try to get NetworkObject directly from the object
            NetworkObject netObj = collidingObject.GetComponent<NetworkObject>();
            
            // If not found, check if this might be a child object
            if (netObj == null)
            {
                netObj = collidingObject.GetComponentInParent<NetworkObject>();
                if (netObj != null)
                {
                    Debug.Log($"[Portal {portalId}] Found NetworkObject in parent of {collidingObject.name}");
                }
            }
            
            if (netObj == null)
            {
                Debug.Log($"[Portal {portalId}] Object doesn't have a NetworkObject component: {collidingObject.name}");
                return;
            }
            
            // Check if object is on teleport cooldown
            if (IsObjectOnCooldown(netObj.Id))
            {
                // Object is on cooldown, skip teleportation
                return;
            }
            
            bool isInputSource = false;
            bool isPlayerCharacter = false;
            
            try
            {
                isInputSource = netObj.IsInputSource;
                isPlayerCharacter = netObj.GetComponent<StinkySteak.N2D.Gameplay.Player.Character.PlayerCharacter>() != null;
                
                // Additional debug info
                Debug.Log($"[Portal {portalId}] Collision object details: ID={netObj.Id}, Name={netObj.name}, " +
                          $"IsInputSource={isInputSource}, IsPlayerCharacter={isPlayerCharacter}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Portal {portalId}] Error getting object properties: {ex.Message}");
            }
            
            // Use the singleton instead of an inspector reference
            if (PortalManager.Instance == null)
            {
                Debug.LogError($"[Portal {portalId}] Can't find PortalManager instance during collision handling!");
                return;
            }
            
            // IMPORTANT: Use server-authority handling when on server
            if (_isServer)
            {
                Debug.Log($"[Portal {portalId}] Server is handling teleportation to portal {destinationPortalId}");
                
                // Special handling for player character
                if (isPlayerCharacter)
                {
                    Debug.Log($"[Portal {portalId}] Teleporting player character {netObj.name} (ID: {netObj.Id})");
                }
                
                // Set cooldown before teleporting
                SetObjectCooldown(netObj.Id);
                
                // On server, directly handle the teleportation for any NetworkObject
                PortalManager.Instance.HandlePortalEntry(netObj, destinationPortalId);
                return;
            }
            
            // Client-side handling
            // First try with RPC, then fallback to direct if needed
            bool shouldTryTeleport = isInputSource || isPlayerCharacter;
            
            if (shouldTryTeleport)
            {
                Debug.Log($"[Portal {portalId}] Client is requesting teleportation for {netObj.name} to portal {destinationPortalId}");
                
                // Set cooldown before teleporting
                SetObjectCooldown(netObj.Id);
                
                // Try RPC first - IMPORTANT: Pass the actual network object ID that needs teleporting
                bool rpcSuccess = false;
                try
                {
                    PortalManager.Instance.RPC_RequestTeleport(destinationPortalId, netObj.Id);
                    rpcSuccess = true;
                    Debug.Log($"[Portal {portalId}] RPC_RequestTeleport called successfully for {netObj.name}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Portal {portalId}] Failed to call RPC_RequestTeleport: {ex.Message}");
                }
                
                // If RPC failed, try direct teleport
                if (!rpcSuccess)
                {
                    Debug.LogWarning($"[Portal {portalId}] RPC failed, attempting direct teleport as fallback");
                    
                    try
                    {
                        Debug.Log($"[Portal {portalId}] Looking for destination portal {destinationPortalId}");
                        
                        if (PortalManager.Instance.TryGetPortal(destinationPortalId, out Portal destPortal))
                        {
                            Debug.Log($"[Portal {portalId}] Found destination portal at {destPortal.transform.position}");
                            
                            // First try to get the local player character
                            var playerChar = FindLocalPlayerCharacter();
                            if (playerChar != null)
                            {
                                // Try moving the player character
                                Vector3 oldPos = playerChar.transform.position;
                                playerChar.transform.position = destPortal.transform.position;
                                Debug.Log($"[Portal {portalId}] Direct teleport: Player character from {oldPos} to {destPortal.transform.position}");
                            }
                            else
                            {
                                // Fallback to the original network object
                                Vector3 oldPos = netObj.transform.position;
                                netObj.transform.position = destPortal.transform.position;
                                Debug.Log($"[Portal {portalId}] Direct teleport: Object {netObj.name} from {oldPos} to {destPortal.transform.position}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[Portal {portalId}] Couldn't find destination portal {destinationPortalId} for fallback teleport");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Portal {portalId}] Error in direct teleport fallback: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.Log($"[Portal {portalId}] Object is not player-controlled, ignoring on client");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Portal {portalId}] Error in HandleCollision: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    // Helper method to find the local player character
    private NetworkObject FindLocalPlayerCharacter()
    {
        try
        {
            // Try to find all network objects
            List<NetworkObject> networkObjects = ObjectFinder.FindFast<NetworkObject>();
            Debug.Log($"[Portal {portalId}] Looking for player character among {networkObjects.Count} network objects");
            
            // First look for player character with component
            foreach (var netObj in networkObjects)
            {
                if (netObj == null) continue;
                
                try
                {
                    var playerCharacter = netObj.GetComponent<StinkySteak.N2D.Gameplay.Player.Character.PlayerCharacter>();
                    if (playerCharacter != null)
                    {
                        bool isInputSource = false;
                        try { isInputSource = netObj.IsInputSource; } catch {}
                        
                        Debug.Log($"[Portal {portalId}] Found player character: {netObj.name}, IsInputSource: {isInputSource}");
                        return netObj;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Portal {portalId}] Error checking for PlayerCharacter: {ex.Message}");
                }
            }
            
            // If not found, look for input source
            foreach (var netObj in networkObjects)
            {
                if (netObj == null) continue;
                
                try
                {
                    bool isInputSource = false;
                    try { isInputSource = netObj.IsInputSource; } catch { continue; }
                    
                    if (isInputSource)
                    {
                        Debug.Log($"[Portal {portalId}] Found input source: {netObj.name}");
                        return netObj;
                    }
                }
                catch {}
            }
            
            Debug.LogWarning($"[Portal {portalId}] Could not find player character");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Portal {portalId}] Error finding player character: {ex.Message}");
            return null;
        }
    }
}