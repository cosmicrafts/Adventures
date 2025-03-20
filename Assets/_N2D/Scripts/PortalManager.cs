using Netick;
using Netick.Unity;
using System.Collections.Generic;
using UnityEngine;
using StinkySteak.N2D.Finder;

public class PortalManager : NetworkBehaviour
{
    // Static instance accessible from anywhere without inspector references
    public static PortalManager Instance { get; private set; }

    [Header("Portal System")]
    [SerializeField] private float teleportCooldown = 2f;
    
    private Dictionary<int, Portal> portalRegistry = new Dictionary<int, Portal>();
    private Dictionary<int, float> objectLastTeleportTime = new Dictionary<int, float>();
    
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
    
    // Called directly from portal when an object enters
    public void HandlePortalEntry(NetworkObject networkObject, int destinationPortalId)
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
        
        bool isPlayerCharacter = networkObject.GetComponent<StinkySteak.N2D.Gameplay.Player.Character.PlayerCharacter>() != null;
        bool isPlayerSession = networkObject.name.Contains("PlayerSession");
        bool isInputSource = networkObject.IsInputSource;
        
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
        if (portalRegistry.TryGetValue(destinationPortalId, out Portal destinationPortal))
        {
            // Set cooldown
            objectLastTeleportTime[objectId] = currentTime;
            
            Vector3 oldPosition = networkObject.transform.position;
            
            // Teleport object
            networkObject.transform.position = destinationPortal.transform.position;
            
            // If this is a player session but not a character, try to find and teleport their character too
            if (isPlayerSession && !isPlayerCharacter)
            {
                int playerId = objectId;
                List<NetworkObject> networkObjects = ObjectFinder.FindFast<NetworkObject>();
                
                foreach (NetworkObject netObj in networkObjects)
                {
                    if (netObj == null) continue;
                    
                    var playerCharacter = netObj.GetComponent<StinkySteak.N2D.Gameplay.Player.Character.PlayerCharacter>();
                    if (playerCharacter != null)
                    {
                        // Check if this character belongs to the player
                        if (playerCharacter.InputSourcePlayerId == playerId)
                        {
                            Debug.Log($"[PortalManager] Also teleporting player character {netObj.name} (ID: {netObj.Id}) for player {playerId}");
                            
                            // Teleport character directly
                            netObj.transform.position = destinationPortal.transform.position;
                            
                            // Set cooldown for character too
                            objectLastTeleportTime[netObj.Id] = currentTime;
                        }
                    }
                }
            }
            
            // Log the teleport
            Debug.Log($"[PortalManager] Object {objectId} teleported from {oldPosition} to {destinationPortal.transform.position} (portal {destinationPortalId})");
        }
        else
        {
            Debug.LogWarning($"[PortalManager] Destination portal {destinationPortalId} not found in registry. Available portals: {string.Join(", ", portalRegistry.Keys)}");
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
    
    [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Owner, isReliable: true)]
    public void RPC_RequestTeleport(int destinationPortalId)
    {
        try
        {
            Debug.Log($"[PortalManager] RPC_RequestTeleport received for destination {destinationPortalId}");
            
            // Find input source object using ObjectFinder for better performance
            List<NetworkObject> networkObjects = null;
            try
            {
                networkObjects = ObjectFinder.FindFast<NetworkObject>();
                Debug.Log($"[PortalManager] Found {networkObjects.Count} network objects in the scene");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PortalManager] Error finding network objects: {ex.Message}");
                return;
            }
            
            if (networkObjects == null || networkObjects.Count == 0)
            {
                Debug.LogWarning("[PortalManager] No network objects found in the scene");
                return;
            }
            
            // First, find the player's character (likely with PlayerCharacter component)
            NetworkObject playerCharacter = null;
            foreach (NetworkObject netObj in networkObjects)
            {
                if (netObj == null) continue;
                
                try
                {
                    // Check for actual player character by looking for specific components
                    if (netObj.GetComponent<StinkySteak.N2D.Gameplay.Player.Character.PlayerCharacter>() != null)
                    {
                        if (netObj.IsInputSource)
                        {
                            playerCharacter = netObj;
                            Debug.Log($"[PortalManager] Found player character: {netObj.name} with ID: {netObj.Id}");
                            break;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PortalManager] Error checking network object for player character: {ex.Message}");
                }
            }
            
            // If player character not found, fallback to any input source
            if (playerCharacter == null)
            {
                foreach (NetworkObject netObj in networkObjects)
                {
                    if (netObj == null) continue;
                    
                    try
                    {
                        bool isInputSource = netObj.IsInputSource;
                        Debug.Log($"[PortalManager] Checking network object: {netObj.name}, ID: {netObj.Id}, IsInputSource: {isInputSource}");
                        
                        if (isInputSource)
                        {
                            playerCharacter = netObj;
                            Debug.Log($"[PortalManager] Found input source object (fallback): {netObj.name} with ID: {netObj.Id}");
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[PortalManager] Error checking network object: {ex.Message}");
                    }
                }
            }
            
            if (playerCharacter != null)
            {
                Debug.Log($"[PortalManager] Calling HandlePortalEntry for {playerCharacter.name} to portal {destinationPortalId}");
                HandlePortalEntry(playerCharacter, destinationPortalId);
            }
            else
            {
                Debug.LogWarning("[PortalManager] Could not find player character or input source object");
            }
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