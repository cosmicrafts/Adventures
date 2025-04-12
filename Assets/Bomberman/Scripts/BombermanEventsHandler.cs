using System.Collections.Generic;
using UnityEngine;
using Netick;
using Netick.Unity;

namespace Netick.Samples.Bomberman
{
    public class BombermanEventsHandler : NetworkBehaviour
    {
        public List<BombermanController>       Players          = new(4);
        public List<BombermanController>       AlivePlayers     = new(4);
       
        private GameObject                     _playerPrefab;
        private Vector3[]                      _spawnPositions  = new Vector3[4]  {  new Vector3(11, 9, 0),  new Vector3(11, 1, 0),  new Vector3(1,  9, 0), new Vector3(1,  1, 0) };
        private Queue<Vector3>                 _freePositions   = new(4);
     
     public GameObject PlayerPrefab;
public GameObject BombPrefab;
public GameObject DestroyableBlockPrefab;
        public GameObject PowerUpPrefab;


public override void NetworkStart()
{
    // Set _playerPrefab to the inspector-assigned PlayerPrefab
    _playerPrefab = PlayerPrefab;

    // Temporary direct instantiation for testing
    var testPlayerInstance = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
    Debug.Log(testPlayerInstance != null ? "Direct instantiation successful" : "Direct instantiation failed");

    var testBombInstance = Instantiate(BombPrefab, Vector3.zero, Quaternion.identity);
    Debug.Log(testBombInstance != null ? "Direct bomb instantiation successful" : "Direct bomb instantiation failed");

    Sandbox.Events.OnInputRead += OnInput;
    Sandbox.Events.OnConnectRequest += OnConnectRequest;
    Sandbox.Events.OnPlayerConnected += OnPlayerConnected;
    Sandbox.Events.OnPlayerDisconnected += OnPlayerDisconnected;

    Sandbox.InitializePool(BombPrefab, 5);
    Sandbox.InitializePool(PlayerPrefab, 4);

    for (int i = 0; i < 4; i++)
        _freePositions.Enqueue(_spawnPositions[i]);

    if (IsServer)
        RestartGame();
}


        public void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
        {
            if (Sandbox.ConnectedPlayers.Count >= 4)
                request.Refuse();
        }

        // This is called on the server when a playerObj has connected.
        public void OnPlayerConnected(NetworkSandbox sandbox, NetworkPlayer player)
        {
            var playerObj       = sandbox.NetworkInstantiate(_playerPrefab, _spawnPositions[Sandbox.ConnectedPlayers.Count], Quaternion.identity, player).GetComponent<BombermanController>();
            player.PlayerObject = playerObj.gameObject;
            AlivePlayers. Add(playerObj);
            Players.      Add(playerObj);
        }

        // This is called on the server when a client has disconnected.
        public void OnPlayerDisconnected(NetworkSandbox sandbox, Netick.NetworkPlayer player, TransportDisconnectReason reason)
        {
            _freePositions.Enqueue(((GameObject)player.PlayerObject).GetComponent<BombermanController>().SpawnPos);
            Players.       Remove (((GameObject)player.PlayerObject).GetComponent<BombermanController>());
        }

        // This is called to read inputs.
        public void OnInput(NetworkSandbox sandbox)
        {
            var input        = sandbox.GetInput<BombermanInput>();
            input.Movement   = GetMovementDir();
            input.PlantBomb |= Input.GetKeyDown(KeyCode.Space);
            sandbox.SetInput(input);
        }

public void RestartGame()
        {
            Debug.Log("Restarting game and destroying all blocks and bombs...");

            // Destroy existing blocks
            foreach (var block in Sandbox.FindObjectsOfType<Block>())
            {
                if (block != null && block.Object != null)
                {
                    Debug.Log($"Destroying block: {block.Object.name}");
                    Sandbox.Destroy(block.Object);
                }
                else
                {
                    Debug.LogWarning("Block or Block.Object is null, skipping destroy.");
                }
            }

            // Destroy existing bombs
            foreach (var bomb in Sandbox.FindObjectsOfType<Bomb>())
            {
                if (bomb != null && bomb.Object != null)
                {
                    Debug.Log($"Destroying bomb: {bomb.Object.name}");
                    Sandbox.Destroy(bomb.Object);
                }
                else
                {
                    Debug.LogWarning("Bomb or Bomb.Object is null, skipping destroy.");
                }
            }

            Debug.Log("Level cleared. Now creating new blocks and power-ups...");

            // Ensure prefabs are assigned in the Inspector
            if (DestroyableBlockPrefab == null) Debug.LogError("DestroyableBlockPrefab is not assigned in the Inspector!");
            if (PowerUpPrefab == null) Debug.LogError("PowerUpPrefab is not assigned in the Inspector!");

            // Create new blocks
            var takenPositions = new List<Vector3>();
            var maxX = 11;
            var maxY = 9;

            for (int x = 1; x <= maxX; x++)
            {
                for (int y = 1; y <= maxY; y++)
                {
                    var pos = new Vector3(x, y);
                    var shouldSpawn = Random.value > 0.5f;

                    if (shouldSpawn && IsValidPos(pos))
                    {
                        var instantiatedBlock = Sandbox.NetworkInstantiate(DestroyableBlockPrefab, pos, Quaternion.identity);
                        if (instantiatedBlock != null)
                        {
                            Debug.Log($"Block instantiated at position {pos}");
                            takenPositions.Add(pos);
                        }
                        else
                        {
                            Debug.LogError($"Failed to instantiate block at position {pos}");
                        }
                    }
                }
            }

            // Create power-ups
            var numberOfBoosters = Random.Range(2, 5);
            while (numberOfBoosters > 0)
            {
                var randomPos = new Vector3(Random.Range(1, maxX + 1), Random.Range(1, maxY + 1), 0);
                var type = (Random.value > 0.5f) ? PowerUpType.Speed : PowerUpType.IncreaseBombs;

                if (!takenPositions.Contains(randomPos) && IsValidPos(randomPos))
                {
                    var booster = Sandbox.NetworkInstantiate(PowerUpPrefab, randomPos, Quaternion.identity)?.GetComponent<PowerUp>();
                    if (booster != null)
                    {
                        booster.Type = type;
                        Debug.Log($"Power-up instantiated at {randomPos} with type {type}");
                        numberOfBoosters--;
                    }
                    else
                    {
                        Debug.LogError($"Failed to instantiate power-up at {randomPos}");
                    }
                }
            }

            // Reset players
            foreach (var player in Players)
            {
                if (player != null)
                {
                    player.Respawn();
                    Debug.Log("Player respawned.");
                }
            }
        }

        private bool IsValidPos(Vector3 pos)
        {
            // if the pos is the position of a static block, we ignore it.
            if ((pos.x >= 2 && pos.x <= 10) && (pos.y >= 2 && pos.y <= 8))
                if (pos.x % 2 == 0 && pos.y % 2 == 0)
                    return false;

            // if the pos is near the position of the spawn locations of the players, we ignore it.
            foreach (var loc in _spawnPositions)
            {
                if (pos == loc) return false;
                if (pos == loc + Vector3.up   || pos == loc + Vector3.down) return false;
                if (pos == loc + Vector3.left || pos == loc + Vector3.right) return false;
            }

            return true;
        }

        public void KillPlayer(BombermanController bomber)
        {
            AlivePlayers.Remove(bomber);

            if (AlivePlayers.Count == 1)
            {
                AlivePlayers[0].Score++;
                RestartGame();
            }

            else if (AlivePlayers.Count < 1)
                RestartGame();
        }

        public void RespawnPlayer(BombermanController bomber)
        {
            if (!AlivePlayers.Contains(bomber))
                AlivePlayers.Add(bomber);
        }
        private Vector2 GetMovementDir()
        {
            if      (Input.GetKey(KeyCode.W)) return Vector2.up;
            else if (Input.GetKey(KeyCode.D)) return Vector2.right;
            else if (Input.GetKey(KeyCode.S)) return Vector2.down;
            else if (Input.GetKey(KeyCode.A)) return Vector2.left;
            else                              return Vector2.zero;
        }
    }
}