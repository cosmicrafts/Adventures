using System.Collections.Generic;
using UnityEngine;

public class NPCProceduralGenerator : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;        // Prefab for enemies
    public float tileWorldSize = 16f;     // Size of each chunk tile
    public int renderDistance = 4;        // Number of chunks to render around the player
    public int enemiesPerChunk = 2;       // Number of enemies per chunk
    public float enemySpawnDistance = 10f; // Minimum distance from player to spawn enemies

    private Transform player;
    private Vector2 playerPosition;
    private Dictionary<Vector2, List<GameObject>> enemyChunks = new Dictionary<Vector2, List<GameObject>>();
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

    private void Start()
    {
        player = Camera.main.transform;
        GenerateInitialEnemyChunks();
    }

    private void Update()
    {
        UpdateEnemyChunksAroundPlayer();
        RemoveDistantChunks();
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

        List<GameObject> enemiesInChunk = new List<GameObject>();

        for (int i = 0; i < enemiesPerChunk; i++)
        {
            Vector3 enemyPosition = GetRandomPositionAroundPlayer(chunkCoord);
            if (enemyPosition != Vector3.zero)
            {
                GameObject enemy = SpawnEnemy(enemyPosition);
                enemiesInChunk.Add(enemy);
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

        return Vector3.zero; // Return invalid position if too close to player
    }

    GameObject SpawnEnemy(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
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
                Destroy(enemy);
            }
            enemyChunks.Remove(chunkCoord);
        }
    }
}
