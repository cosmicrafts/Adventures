using Netick;
using Netick.Unity;
using UnityEngine;
using System.Collections;

public class Portal : NetworkBehaviour
{
    [Header("Portal Settings")]
    public int portalId;
    public int destinationPortalId;
    public Color portalColor = Color.blue;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer _portalRenderer;
    
    // Default value for IsActive when not initialized through network
    private bool _defaultActiveState = true;
    
    [Networked] public NetworkBool IsActive { get; set; }
    
    private bool _hasRegistered = false;
    private bool _isNetworkInitialized = false;
    private float _lastRegistrationAttempt = 0f;
    private const float REGISTRATION_RETRY_INTERVAL = 0.5f;
    
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
        
        // Initialize the IsActive property on the server
        if (IsServer)
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
        
        Debug.Log($"[Portal {portalId}] NetworkStart called. IsServer: {IsServer}, IsActive: {GetIsActive()}");
        UpdateVisuals();
        
        // Start trying to register with the manager
        StartCoroutine(RegisterWithManager());
    }
    
    private IEnumerator RegisterWithManager()
    {
        Debug.Log($"[Portal {portalId}] Starting registration attempts with PortalManager");
        
        int attempts = 0;
        // Keep trying until we successfully register
        while (!_hasRegistered && IsServer)
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
        if (IsServer && !_hasRegistered && Time.time > _lastRegistrationAttempt + REGISTRATION_RETRY_INTERVAL)
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
            if (gameObject.CompareTag("NPC") || gameObject.CompareTag("Enemy"))
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
            
            bool isInputSource = false;
            try
            {
                isInputSource = netObj.IsInputSource;
            }
            catch (System.NullReferenceException)
            {
                Debug.LogWarning($"[Portal {portalId}] Could not access IsInputSource property");
            }
            
            Debug.Log($"[Portal {portalId}] NetworkObject detected with ID: {netObj.Id}, IsInputSource: {isInputSource}");
            
            // Use the singleton instead of an inspector reference
            if (PortalManager.Instance != null)
            {
                bool isServer = false;
                try
                {
                    isServer = IsServer;
                }
                catch (System.NullReferenceException)
                {
                    Debug.LogWarning($"[Portal {portalId}] Could not access IsServer property");
                }
                
                if (isServer)
                {
                    Debug.Log($"[Portal {portalId}] Server is handling teleportation to portal {destinationPortalId}");
                    // On server, directly handle the teleportation
                    PortalManager.Instance.HandlePortalEntry(netObj, destinationPortalId);
                }
                else if (isInputSource)
                {
                    Debug.Log($"[Portal {portalId}] Client is requesting teleportation to portal {destinationPortalId}");
                    // On client, request teleport from server
                    PortalManager.Instance.RPC_RequestTeleport(destinationPortalId);
                }
                else
                {
                    Debug.Log($"[Portal {portalId}] Object is not the input source, ignoring on client");
                }
            }
            else
            {
                Debug.LogWarning($"[Portal {portalId}] Can't find PortalManager instance during collision handling!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Portal {portalId}] Error in HandleCollision: {ex.Message}\n{ex.StackTrace}");
        }
    }
}