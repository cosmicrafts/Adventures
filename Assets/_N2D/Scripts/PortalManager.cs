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
        
        Debug.Log($"[PortalManager] HandlePortalEntry processing object {networkObject.Id} to portal {destinationPortalId}");
        
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
            
            // Log the teleport
            Debug.Log($"[PortalManager] Object {objectId} teleported from {oldPosition} to {destinationPortal.transform.position} (portal {destinationPortalId})");
        }
        else
        {
            Debug.LogWarning($"[PortalManager] Destination portal {destinationPortalId} not found in registry. Available portals: {string.Join(", ", portalRegistry.Keys)}");
        }
    }
    
    [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Proxies, isReliable: true)]
    public void RPC_RequestTeleport(int destinationPortalId)
    {
        Debug.Log($"[PortalManager] RPC_RequestTeleport received for destination {destinationPortalId}");
        
        if (!IsServer)
        {
            Debug.Log("[PortalManager] RPC_RequestTeleport called on client, ignoring");
            return;
        }
        
        // Find input source object using ObjectFinder for better performance
        List<NetworkObject> networkObjects = ObjectFinder.FindFast<NetworkObject>();
        Debug.Log($"[PortalManager] Found {networkObjects.Count} network objects in the scene");
        
        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj.IsInputSource)
            {
                Debug.Log($"[PortalManager] Found input source object: {netObj.name}");
                HandlePortalEntry(netObj, destinationPortalId);
                return;
            }
        }
        
        Debug.LogWarning("[PortalManager] Could not find input source object");
    }

    [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Proxies, isReliable: true)]
    public void RPC_Respawn()
    {
        // RPC implementation
    }
}