using System.Collections.Generic;
using Netick;
using Netick.Unity;
using UnityEngine;

public class NPCProceduralGenerator : NetworkBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public float tileWorldSize = 16f;
    public int renderDistance = 4;
    public int enemiesPerChunk = 2;
    public float enemySpawnDistance = 10f;

    private Transform player;
    private Vector2 playerPosition;
    private Dictionary<Vector2, List<NetworkObject>> enemyChunks = new Dictionary<Vector2, List<NetworkObject>>();
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

    public override void NetworkStart()
    {
        if (IsServer)
        {
            player = Camera.main.transform;

            // Initialize the pool for enemy prefab
            Sandbox.InitializePool(enemyPrefab, renderDistance * renderDistance * enemiesPerChunk);

            GenerateInitialEnemyChunks();
        }
    }

    public override void NetworkFixedUpdate()
    {
        if (IsServer)
        {
            UpdateEnemyChunksAroundPlayer();
            RemoveDistantChunks();
        }
    }

    void GenerateInitialEnemyChunks()
    {
        playerPosition = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                GenerateEnemyChunk(new Vector2(playerPosition.x + x, playerPosition.y + y));
            }
        }
    }

    void GenerateEnemyChunk(Vector2 chunkCoord)
    {
        if (enemyChunks.ContainsKey(chunkCoord)) return;

        List<NetworkObject> enemiesInChunk = new List<NetworkObject>();

        for (int i = 0; i < enemiesPerChunk; i++)
        {
            Vector3 enemyPosition = GetRandomPositionAroundPlayer(chunkCoord);
            if (enemyPosition != Vector3.zero)
            {
                NetworkObject enemy = SpawnEnemy(enemyPosition);
                if (enemy != null)
                {
                    enemiesInChunk.Add(enemy);
                }
            }
        }

        enemyChunks.Add(chunkCoord, enemiesInChunk);
    }

    Vector3 GetRandomPositionAroundPlayer(Vector2 chunkCoord)
    {
        float xOffset = Random.Range(-tileWorldSize / 2, tileWorldSize / 2);
        float yOffset = Random.Range(-tileWorldSize / 2, tileWorldSize / 2);

        Vector3 enemyPosition = new Vector3(
            chunkCoord.x * tileWorldSize + xOffset,
            chunkCoord.y * tileWorldSize + yOffset,
            0f
        );

        if (Vector3.Distance(enemyPosition, player.position) > enemySpawnDistance && !occupiedPositions.Contains(enemyPosition))
        {
            occupiedPositions.Add(enemyPosition);
            return enemyPosition;
        }

        return Vector3.zero;
    }

    NetworkObject SpawnEnemy(Vector3 position)
    {
        // Use NetworkInstantiate to spawn networked enemies
        NetworkObject enemy = Sandbox.NetworkInstantiate(enemyPrefab, position, Quaternion.identity)?.GetComponent<NetworkObject>();

        if (enemy == null)
        {
            Debug.LogError("Enemy prefab is missing a NetworkObject component.");
            return null;
        }

        // Add random movement
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }

        return enemy;
    }

    void UpdateEnemyChunksAroundPlayer()
    {
        Vector2 newPlayerPos = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        if (newPlayerPos != playerPosition)
        {
            playerPosition = newPlayerPos;

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int y = -renderDistance; y <= renderDistance; y++)
                {
                    Vector2 chunkCoord = new Vector2(playerPosition.x + x, playerPosition.y + y);
                    if (!enemyChunks.ContainsKey(chunkCoord))
                    {
                        GenerateEnemyChunk(chunkCoord);
                    }
                }
            }
        }
    }

    void RemoveDistantChunks()
    {
        List<Vector2> chunksToRemove = new List<Vector2>();

        foreach (var chunk in enemyChunks)
        {
            float distanceToPlayer = Vector2.Distance(playerPosition, chunk.Key);
            if (distanceToPlayer > renderDistance + 1)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            foreach (var enemy in enemyChunks[chunkCoord])
            {
                if (enemy != null)
                {
                    Sandbox.Destroy(enemy);
                }
            }
            enemyChunks.Remove(chunkCoord);
        }
    }
}
