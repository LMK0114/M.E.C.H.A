using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    void Awake()
    {
        // Auto-find spawn points by tag if not assigned in Inspector
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            FindSpawnPointsByTag();
        }

        // Debug: Log all found spawn points
        Debug.Log($"Total spawn points available: {spawnPoints?.Length ?? 0}");
        for (int i = 0; i < spawnPoints?.Length; i++)
        {
            if (spawnPoints[i] != null)
                Debug.Log($"SpawnPoint {i}: {spawnPoints[i].name} at position {spawnPoints[i].position}");
            else
                Debug.LogError($"SpawnPoint {i} is NULL!");
        }
    }

    private void FindSpawnPointsByTag()
    {
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag(spawnPointTag);

        if (spawnPointObjects.Length > 0)
        {
            spawnPoints = spawnPointObjects.Select(go => go.transform)
                                          .OrderBy(t => t.name)
                                          .ToArray();

            Debug.Log($"Found {spawnPoints.Length} spawn points with tag '{spawnPointTag}':");
            foreach (var point in spawnPoints)
            {
                Debug.Log($"  - {point.name} at {point.position}");
            }
        }
        else
        {
            Debug.LogError($"No GameObjects with tag '{spawnPointTag}' found in the scene!");
        }
    }

    void Start()
    {
        // Delay spawning to ensure network is ready
        Invoke(nameof(TrySpawnPlayers), 0.2f);
    }

    private void TrySpawnPlayers()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Not server or NetworkManager missing");
            return;
        }

        // DESTROY auto-spawned players first
        var existingPlayers = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None)
            .Where(obj => obj.IsPlayerObject).ToList();

        if (existingPlayers.Count > 0)
        {
            Debug.Log($"Destroying {existingPlayers.Count} auto-spawned players");
            foreach (var player in existingPlayers)
            {
                if (player != null)
                {
                    Debug.Log($"Destroying player for client {player.OwnerClientId} at {player.transform.position}");
                    player.Despawn(true);
                }
            }
        }

        // Now spawn players at correct positions
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points available!");
            return;
        }

        // Sort client IDs to ensure consistent ordering between host and clients
        var sortedClients = NetworkManager.Singleton.ConnectedClientsIds.OrderBy(id => id).ToList();

        Debug.Log($"Spawning {sortedClients.Count} players at spawn points...");

        for (int i = 0; i < sortedClients.Count; i++)
        {
            ulong clientId = sortedClients[i];
            Transform spawnPoint = GetSpawnPoint(i);

            if (spawnPoint == null)
            {
                Debug.LogError($"Spawn point {i} is null!");
                return;
            }

            Debug.Log($"Spawning player for client {clientId} (index {i}) at {spawnPoint.position}");

            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // Get the NetworkObject component
            NetworkObject networkObject = player.GetComponent<NetworkObject>();

            // Set the owner client ID before spawning (optional but good practice)
            networkObject.SpawnAsPlayerObject(clientId);

            // Verify correct ownership
            Debug.Log($"Spawned player for client {clientId}. NetworkObject OwnerClientId: {networkObject.OwnerClientId}");
        }
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            return spawnPoints[spawnIndex];
        }
        return transform;
    }
}