using Netick;
using Netick.Unity;
using System.Collections.Generic;
using UnityEngine;
using StinkySteak.N2D.Finder;
using System.Linq;
using StinkySteak.N2D.Gameplay.Player.Character;

public class PortalManager : NetworkBehaviour
{
    // Static instance accessible from anywhere without inspector references
    public static PortalManager Instance { get; private set; }

    [Header("Portal System")]
    [SerializeField] private float teleportCooldown = 2f;
    
    private Dictionary<int, Portal> portalRegistry = new Dictionary<int, Portal>();
    private Dictionary<int, float> objectLastTeleportTime = new Dictionary<int, float>();
    
    // Track all player-related objects to avoid scene searches
    private Dictionary<int, NetworkObject> trackedPlayerObjects = new Dictionary<int, NetworkObject>();
    private List<NetworkObject> playerSessions = new List<NetworkObject>();
    private List<NetworkObject> playerCharacters = new List<NetworkObject>();
    
    private void Awake()
    {
        // Set up the singleton instance as early as possible
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[PortalManager] Singleton instance set up during Awake");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[PortalManager] Multiple instances detected. Destroying duplicate.");
            Destroy(this);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up the singleton instance when destroyed
        if (Instance == this)
        {
            Debug.Log("[PortalManager] Singleton instance cleared on destroy");
            Instance = null;
        }
    }
    
    public override void NetworkStart()
    {
        // Double-check the singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        
        Debug.Log("[PortalManager] NetworkStart called. IsServer: " + IsServer);
        
        if (IsServer)
        {
            portalRegistry.Clear();
            
            // Scan for existing portals on scene load using ObjectFinder
            List<Portal> existingPortals = ObjectFinder.FindFast<Portal>();
            Debug.Log($"[PortalManager] Found {existingPortals.Count} existing portals in the scene");
            
            foreach (var portal in existingPortals)
            {
                RegisterPortal(portal);
            }
            
            // Pre-scan for player objects to avoid searching later
            try
            {
                List<NetworkObject> networkObjects = ObjectFinder.FindFast<NetworkObject>();
                foreach (var netObj in networkObjects)
                {
                    if (netObj == null) continue;
                    
                    try
                    {
                        // Track player-related objects
                        if (netObj.name.Contains("PlayerSession") || 
                            netObj.name.Contains("Player") || 
                            netObj.GetComponent<PlayerCharacter>() != null)
                        {
                            RegisterPlayerObject(netObj);
                        }
                    }
                    catch {}
                }
                Debug.Log($"[PortalManager] Pre-tracked {trackedPlayerObjects.Count} player objects");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PortalManager] Error pre-scanning for players: {ex.Message}");
            }
        }
    }
    
    // Register a portal manually during runtime
    public void RegisterPortal(Portal portal)
    {
        if (!IsServer) 
        {
            Debug.Log($"[PortalManager] Cannot register portal {portal.portalId} - not on server");
            return;
        }
        
        if (portal == null)
        {
            Debug.LogWarning("[PortalManager] Attempted to register null portal");
            return;
        }
        
        if (!portalRegistry.ContainsKey(portal.portalId))
        {
            portalRegistry[portal.portalId] = portal;
            Debug.Log($"[PortalManager] Registered portal {portal.portalId} targeting portal {portal.destinationPortalId}");
        }
        else
        {
            Debug.Log($"[PortalManager] Portal {portal.portalId} already registered");
        }
    }
    
    // Register a player object for direct reference
    public void RegisterPlayerObject(NetworkObject obj)
    {
        if (obj == null) return;
        
        try
        {
            int id = obj.Id;
            
            // Add to tracked objects dictionary
            if (!trackedPlayerObjects.ContainsKey(id))
            {
                trackedPlayerObjects[id] = obj;
                Debug.Log($"[PortalManager] Registered player object: {obj.name} (ID: {id})");
                
                // Categorize the object
                if (obj.name.Contains("PlayerSession"))
                {
                    playerSessions.Add(obj);
                }
                else if (obj.GetComponent<PlayerCharacter>() != null)
                {
                    playerCharacters.Add(obj);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PortalManager] Error registering player object: {ex.Message}");
        }
    }
    
    // Remove a player object reference when destroyed
    public void UnregisterPlayerObject(NetworkObject obj)
    {
        if (obj == null) return;
        
        try
        {
            int id = obj.Id;
            
            if (trackedPlayerObjects.ContainsKey(id))
            {
                trackedPlayerObjects.Remove(id);
                playerSessions.Remove(obj);
                playerCharacters.Remove(obj);
                Debug.Log($"[PortalManager] Unregistered player object: {obj.name} (ID: {id})");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PortalManager] Error unregistering player object: {ex.Message}");
        }
    }
    
    // Called directly from portal when an object enters
    public void HandlePortalEntry(NetworkObject networkObject, int destinationPortalId)
    {
        try
        {
            if (!IsServer)
            {
                Debug.Log("[PortalManager] HandlePortalEntry called on client, ignoring");
                return;
            }
            
            if (networkObject == null)
            {
                Debug.LogWarning("[PortalManager] HandlePortalEntry received null object");
                return;
            }
            
            // Register the object for future direct access
            RegisterPlayerObject(networkObject);
            
            // Basic object info
            bool isPlayerCharacter = networkObject.GetComponent<PlayerCharacter>() != null;
            bool isPlayerSession = networkObject.name.Contains("PlayerSession");
            bool isInputSource = false;
            try { isInputSource = networkObject.IsInputSource; } catch { /* ignore errors */ }
            
            Debug.Log($"[PortalManager] HandlePortalEntry processing object {networkObject.Id} (name: {networkObject.name}, isPlayerCharacter: {isPlayerCharacter}, isPlayerSession: {isPlayerSession}, isInputSource: {isInputSource}) to portal {destinationPortalId}");
            
            // Check cooldown
            int objectId = networkObject.Id;
            float currentTime = Time.time;
            if (objectLastTeleportTime.TryGetValue(objectId, out float lastTeleportTime))
            {
                if (currentTime - lastTeleportTime < teleportCooldown)
                {
                    Debug.Log($"[PortalManager] Object {objectId} on cooldown, {teleportCooldown - (currentTime - lastTeleportTime):F1}s remaining");
                    return; // Still on cooldown
                }
            }
            
            // Find destination portal
            if (!portalRegistry.TryGetValue(destinationPortalId, out Portal destinationPortal))
            {
                Debug.LogWarning($"[PortalManager] Destination portal {destinationPortalId} not found in registry. Available portals: {string.Join(", ", portalRegistry.Keys)}");
                return;
            }
            
            // Set cooldown
            objectLastTeleportTime[objectId] = currentTime;
            
            // Teleport this object
            TeleportObject(networkObject, destinationPortal.transform.position);
            
            // If this is a player session, directly teleport its related character
            if (isPlayerSession)
            {
                int playerId = networkObject.Entity.InputSourcePlayerId;
                Debug.Log($"[PortalManager] Player session teleported, looking for character with InputSourcePlayerId: {playerId}");
                
                // Look for the player character with matching InputSourcePlayerId
                foreach (var character in playerCharacters)
                {
                    try
                    {
                        if (character != null && character.Entity.InputSourcePlayerId == playerId)
                        {
                            TeleportObject(character, destinationPortal.transform.position);
                            Debug.Log($"[PortalManager] Teleported associated character: {character.name} (ID: {character.Id})");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[PortalManager] Error checking character: {ex.Message}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PortalManager] Error in HandlePortalEntry: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    // Direct teleport method for local fallback teleportation
    public void DirectTeleport(NetworkObject netObj, int destinationPortalId)
    {
        try
        {
            Debug.Log($"[PortalManager] DirectTeleport called for object {netObj.Id} to portal {destinationPortalId}");
            
            if (!portalRegistry.TryGetValue(destinationPortalId, out Portal destinationPortal))
            {
                Debug.LogWarning($"[PortalManager] DirectTeleport: Destination portal {destinationPortalId} not found");
                return;
            }
            
            Vector3 oldPosition = netObj.transform.position;
            netObj.transform.position = destinationPortal.transform.position;
            Debug.Log($"[PortalManager] DirectTeleport: Object {netObj.Id} teleported from {oldPosition} to {destinationPortal.transform.position} (portal {destinationPortalId})");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PortalManager] Error in DirectTeleport: {ex.Message}");
        }
    }
    
    // Allow external access to get a portal by ID
    public bool TryGetPortal(int portalId, out Portal portal)
    {
        return portalRegistry.TryGetValue(portalId, out portal);
    }
    
    // Ultra-simple teleport method that just works
    private void TeleportPlayerById(int playerId, int destinationPortalId)
    {
        try
        {
            // Step 1: Find the target portal position
            Vector3 targetPosition = Vector3.zero;
            Portal destPortal = null;
            
            try
            {
                if (portalRegistry != null && portalRegistry.TryGetValue(destinationPortalId, out destPortal) && destPortal != null)
                {
                    targetPosition = destPortal.transform.position;
                    Debug.Log($"[PortalManager] Target position: {targetPosition}");
                }
                else
                {
                    Debug.LogError($"[PortalManager] Can't find portal with ID {destinationPortalId}");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PortalManager] Portal lookup error: {ex.Message}");
                return;
            }
            
            // Step 2: Directly find and teleport everything that could possibly be player-related
            bool teleportedAnything = false;
            
            try
            {
                // First try to teleport by direct reference
                if (trackedPlayerObjects.TryGetValue(playerId, out NetworkObject directMatch))
                {
                    TeleportObject(directMatch, targetPosition);
                    teleportedAnything = true;
                    Debug.Log($"[PortalManager] Teleported direct match: {directMatch.name} (ID: {playerId})");
                }
                
                // If we have a direct match, also teleport the associated character 
                if (teleportedAnything)
                {
                    // Look for player character objects with specific component types
                    foreach (NetworkObject netObj in trackedPlayerObjects.Values)
                    {
                        if (netObj != null && netObj.Id == playerId)
                        {
                            TeleportObject(netObj, targetPosition);
                            Debug.Log($"[PortalManager] Teleported player object: {netObj.name}");
                            teleportedAnything = true;
                        }
                    }
                }
                
                // FALLBACK: If our direct references failed, use a more expensive search
                if (!teleportedAnything)
                {
                    Debug.Log("[PortalManager] No tracked object found, using scene search fallback");
                    
                    // BRUTE FORCE: Get all GameObjects in the scene
                    var objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    Debug.Log($"[PortalManager] Found {objects.Length} total scene objects");
                    
                    // First teleport the exact match for player ID
                    foreach (var obj in objects)
                    {
                        if (obj != null)
                        {
                            try
                            {
                                var netObj = obj.GetComponent<NetworkObject>();
                                if (netObj != null && netObj.Id == playerId)
                                {
                                    TeleportObject(netObj, targetPosition);
                                    RegisterPlayerObject(netObj); // Add to tracking
                                    teleportedAnything = true;
                                }
                            }
                            catch {}
                        }
                    }
                    
                    // Teleport all player sessions
                    foreach (var obj in objects)
                    {
                        if (obj != null)
                        {
                            try
                            {
                                if (obj.name.Contains("PlayerSession"))
                                {
                                    var netObj = obj.GetComponent<NetworkObject>();
                                    if (netObj != null)
                                    {
                                        TeleportObject(netObj, targetPosition);
                                        RegisterPlayerObject(netObj); // Add to tracking
                                        Debug.Log($"[PortalManager] Teleported session: {obj.name}");
                                        teleportedAnything = true;
                                    }
                                }
                            }
                            catch {}
                        }
                    }
                    
                    // Teleport anything with "Player" in the name
                    foreach (var obj in objects)
                    {
                        if (obj != null)
                        {
                            try
                            {
                                if (obj.name.Contains("Player") || obj.name.Contains("Character"))
                                {
                                    var netObj = obj.GetComponent<NetworkObject>();
                                    if (netObj != null)
                                    {
                                        TeleportObject(netObj, targetPosition);
                                        RegisterPlayerObject(netObj); // Add to tracking
                                        Debug.Log($"[PortalManager] Teleported player object: {obj.name}");
                                        teleportedAnything = true;
                                    }
                                }
                            }
                            catch {}
                        }
                    }
                }
                
                if (!teleportedAnything)
                {
                    Debug.LogWarning("[PortalManager] Couldn't find any objects to teleport!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PortalManager] Error in object search: {ex.Message}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PortalManager] Master teleport error: {ex.Message}");
        }
    }
    
    // Ultra-safe teleport that can't possibly fail
    private void TeleportObject(NetworkObject obj, Vector3 position)
    {
        if (obj == null) return;
        
        try
        {
            // Make extra sure Transform exists
            if (obj != null && obj.transform != null)
            {
                Vector3 oldPos = obj.transform.position;
                
                // Add a larger offset in a random direction from the portal to avoid immediate re-triggering
                float offsetDistance = 4f; // Increase from 2f to 4f for more safety
                float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 randomOffset = new Vector3(
                    Mathf.Cos(randomAngle) * offsetDistance,
                    Mathf.Sin(randomAngle) * offsetDistance,
                    0f
                );
                
                // Apply the offset to the position
                Vector3 offsetPosition = position + randomOffset;
                
                obj.transform.position = offsetPosition;
                
                // Only set cooldown if we successfully teleported
                try
                {
                    int id = obj.Id;
                    objectLastTeleportTime[id] = Time.time;
                }
                catch {}
                
                Debug.Log($"[PortalManager] Teleported {obj.name} (ID: {obj.Id}) from {oldPos} to {offsetPosition} (with offset from portal)");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PortalManager] Failed to teleport {obj?.name}: {ex.Message}");
        }
    }

    [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Owner, isReliable: true)]
    public void RPC_RequestTeleport(int destinationPortalId, int playerNetworkId = -1)
    {
        try
        {
            Debug.Log($"[PortalManager] RPC_RequestTeleport received for destination {destinationPortalId}, player ID: {playerNetworkId}");
            
            // If specific player ID was provided, use it to find the object
            if (playerNetworkId != -1)
            {
                // First try our cached references
                if (trackedPlayerObjects.TryGetValue(playerNetworkId, out NetworkObject playerObject))
                {
                    // We have the direct reference, use it
                    Debug.Log($"[PortalManager] Found cached player object {playerObject.name} (ID: {playerNetworkId})");
                    HandlePortalEntry(playerObject, destinationPortalId);
                    return;
                }
            }
            
            // If we get here, we need to find the local player's input source
            List<NetworkObject> networkObjects = null;
            try
            {
                networkObjects = ObjectFinder.FindFast<NetworkObject>();
                Debug.Log($"[PortalManager] Looking for input source among {networkObjects.Count} network objects");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PortalManager] Error finding network objects: {ex.Message}");
                return;
            }
            
            // Find any input source to teleport
            foreach (NetworkObject netObj in networkObjects)
            {
                if (netObj == null) continue;
                
                try
                {
                    bool isInputSource = false;
                    try { isInputSource = netObj.IsInputSource; } catch { continue; }
                    
                    if (isInputSource)
                    {
                        // We found an input source, use it
                        Debug.Log($"[PortalManager] Found input source: {netObj.name} (ID: {netObj.Id})");
                        HandlePortalEntry(netObj, destinationPortalId);
                        return;
                    }
                }
                catch {}
            }
            
            Debug.LogWarning("[PortalManager] Could not find input source to teleport");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PortalManager] Error in RPC_RequestTeleport: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Proxies, isReliable: true)]
    public void RPC_Respawn()
    {
        // RPC implementation
    }
}