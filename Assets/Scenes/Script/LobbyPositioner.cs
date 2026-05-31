using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class LobbyPositioner : NetworkBehaviour
{
    [Header("Lobby Spawn Points")]
    public Transform hostSpot;
    public Transform clientSpot;

    private bool hasTeleported = false;

    private void Update()
    {
        // Only the server should do the teleporting. If we already did it, stop running this code.
        if (!IsServer || hasTeleported) return;

        // Constantly check how many players exist in the scene right now
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        // DO NOT teleport until BOTH players have successfully finished loading!
        if (allPlayers.Length == 2)
        {
            hasTeleported = true; // Lock the door so we don't teleport them every frame
            MovePlayersToLobbySpots(allPlayers);
        }
    }

    private void MovePlayersToLobbySpots(PlayerMovement[] allPlayers)
    {
        // Sort them so Host is always [0] and Client is [1]
        var sortedPlayers = allPlayers.OrderBy(p => p.OwnerClientId).ToArray();

        // Move the Host
        if (sortedPlayers.Length > 0 && hostSpot != null)
        {
            sortedPlayers[0].TeleportClientRpc(hostSpot.position, hostSpot.rotation);
        }

        // Move the Client
        if (sortedPlayers.Length > 1 && clientSpot != null)
        {
            sortedPlayers[1].TeleportClientRpc(clientSpot.position, clientSpot.rotation);
        }

        Debug.Log("SERVER: Both players successfully loaded the Lobby! Teleporting them now.");
    }
}