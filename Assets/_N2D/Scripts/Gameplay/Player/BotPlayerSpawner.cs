using UnityEngine;
using Netick.Unity;
using Netick;
using StinkySteak.N2D.Gameplay.Player.Session;
using StinkySteak.N2D.Launcher.Prototype;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.PlayerController;
using System.Collections.Generic;

namespace StinkySteak.N2D.Gameplay.Player.Bot
{
    /// <summary>
    /// Spawns AI-controlled bot players that behave like human players
    /// </summary>
    public class BotPlayerSpawner : NetworkBehaviour
    {
        [Header("Bot Settings")]
        [SerializeField] private int maxBotPlayers = 3;
        [SerializeField] private bool spawnBotsOnStart = true;
        [SerializeField] private float botSpawnDelay = 1f;
        [SerializeField] private string[] botNames = new string[] { "Bot_Alpha", "Bot_Beta", "Bot_Charlie", "Bot_Delta", "Bot_Echo" };
        
        [Header("References")]
        [SerializeField] private GameObject playerSessionPrefab;
        
        // Track spawned bots
        private List<NetworkObject> spawnedBotSessions = new List<NetworkObject>();
        private float spawnTimer;
        
        // MatchManager reference
        private MatchManager matchManager;
        
        public override void NetworkStart()
        {
            if (!IsServer) return;
            
            // Get the MatchManager reference
            matchManager = Sandbox.GetComponent<MatchManager>();
            if (matchManager == null)
            {
                Debug.LogError("[BotPlayerSpawner] MatchManager not found in the scene!");
                return;
            }
            
            // Start spawning bots
            if (spawnBotsOnStart)
            {
                spawnTimer = botSpawnDelay;
            }
            
            Debug.Log("[BotPlayerSpawner] Started - will spawn up to " + maxBotPlayers + " bots");
        }
        
        public override void NetworkFixedUpdate()
        {
            if (!IsServer) return;
            
            // Handle automatic bot spawning
            if (spawnTimer > 0)
            {
                spawnTimer -= Sandbox.FixedDeltaTime;
                if (spawnTimer <= 0 && spawnedBotSessions.Count < maxBotPlayers)
                {
                    SpawnBot();
                    spawnTimer = botSpawnDelay;
                }
            }
            
            // Clean up any destroyed bot references
            for (int i = spawnedBotSessions.Count - 1; i >= 0; i--)
            {
                if (spawnedBotSessions[i] == null)
                {
                    spawnedBotSessions.RemoveAt(i);
                }
            }
        }
        
        // Command to spawn a single bot player
        [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Owner, isReliable: true)]
        public void RPC_SpawnBot()
        {
            if (!IsServer) return;
            SpawnBot();
        }
        
        // Command to remove all bot players
        [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Owner, isReliable: true)]
        public void RPC_ClearBots()
        {
            if (!IsServer) return;
            ClearAllBots();
        }
        
        private void SpawnBot()
        {
            if (!IsServer) return;
            
            if (playerSessionPrefab == null)
            {
                Debug.LogError("[BotPlayerSpawner] No player session prefab assigned!");
                return;
            }
            
            if (spawnedBotSessions.Count >= maxBotPlayers)
            {
                Debug.Log("[BotPlayerSpawner] Max bot count reached (" + maxBotPlayers + ")");
                return;
            }
            
            // Generate a bot name
            string botName = GetRandomBotName();
            
            // Spawn the player session for the bot
            NetworkObject botSession = Sandbox.NetworkInstantiate(playerSessionPrefab, Vector3.zero, Quaternion.identity);
            if (botSession != null)
            {
                // Set bot name
                PlayerSession playerSession = botSession.GetComponent<PlayerSession>();
                if (playerSession != null)
                {
                    playerSession.SetNickname(botName);
                }
                
                // Add to our list of bot sessions
                spawnedBotSessions.Add(botSession);
                
                // Spawn a character for this bot
                if (playerSession != null)
                {
                    playerSession.RPC_Respawn();
                    Debug.Log($"[BotPlayerSpawner] Called RPC_Respawn for bot '{botName}'");
                }
                
                // Add BotInputProvider and PlayerControllerSwitcher components to the session
                GameObject sessionObj = botSession.gameObject;
                
                // Add controller switcher if needed
                if (!sessionObj.GetComponent<PlayerControllerSwitcher>())
                {
                    PlayerControllerSwitcher switcher = sessionObj.AddComponent<PlayerControllerSwitcher>();
                    switcher.SetBotControl(true); // Force bot control
                }
                
                // Add AutoInputProvider if needed
                if (!sessionObj.GetComponent<AutoInputProvider>())
                {
                    AutoInputProvider botInput = sessionObj.AddComponent<AutoInputProvider>();
                }
                
                Debug.Log($"[BotPlayerSpawner] Spawned bot player '{botName}' with session ID {botSession.Id}");
            }
            else
            {
                Debug.LogError("[BotPlayerSpawner] Failed to instantiate bot player session!");
            }
        }
        
        private void ClearAllBots()
        {
            if (!IsServer) return;
            
            Debug.Log($"[BotPlayerSpawner] Clearing {spawnedBotSessions.Count} bots");
            
            foreach (var botSession in spawnedBotSessions)
            {
                if (botSession != null)
                {
                    Sandbox.Destroy(botSession);
                }
            }
            
            spawnedBotSessions.Clear();
        }
        
        private string GetRandomBotName()
        {
            if (botNames == null || botNames.Length == 0)
            {
                return "Bot_" + Random.Range(1000, 9999);
            }
            
            string baseName = botNames[Random.Range(0, botNames.Length)];
            
            // Add a random number to ensure uniqueness
            return baseName + "_" + Random.Range(10, 99);
        }
    }
} 