using UnityEngine;
using Unity.Netcode;

public class NetworkLootContainer : BaseInteractable
{
    [Header("Networked State")]
    // Synced automatically to all players. Only the Server can change it.
    public NetworkVariable<bool> hasValuable = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Whenever the server changes 'hasValuable', run this function on all clients
        hasValuable.OnValueChanged += OnValuableStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        hasValuable.OnValueChanged -= OnValuableStateChanged;
    }

    // 1. LOCAL ACTION: The player presses 'E' and triggers this
    public override void OnInteract(PlayerMovement interactingPlayer)
    {
        // Immediately ask the server to handle the logic
        // We pass the player's Network ID so the server knows who clicked it
        InteractServerRpc(interactingPlayer.OwnerClientId);
    }

    // 2. SERVER ACTION: The server receives the request
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractServerRpc(ulong clientId)
    {
        // FIX: Your teammate's code overwrote this! We MUST use ConnectedClients, not SpawnedObjects.
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            Debug.LogError($"SERVER: Could not find Client ID {clientId}");
            return;
        }

        // Get the PlayerObject attached to that client
        PlayerMovement player = client.PlayerObject.GetComponent<PlayerMovement>();

        // --- SERVER LOGIC ---
        if (player.currentRole.Value == PlayerMovement.PlayerRole.Owner)
        {
            if (!hasValuable.Value && player.valuablesToHide.Value > 0)
            {
                hasValuable.Value = true; // Syncs to everyone

                player.valuablesToHide.Value--;

                Debug.Log($"SERVER: Owner stashed a valuable. Left: {player.valuablesToHide.Value}");
            }
            else
            {
                Debug.Log("SERVER: Container already full OR Owner has no valuables left.");
            }
        }
        else if (player.currentRole.Value == PlayerMovement.PlayerRole.Thief)
        {
            if (hasValuable.Value)
            {
                hasValuable.Value = false; // Syncs to everyone

                // BONUS: Give the thief their point in the new inventory backend!
                player.valuablesStolen.Value++;

                Debug.Log($"SERVER: Thief stole a valuable! Total Stolen: {player.valuablesStolen.Value}");
            }
        }
    }

    // 3. CLIENT REACTION: What happens when the state changes?
    private void OnValuableStateChanged(bool previousState, bool newState)
    {
        // Optional: Play a sound effect or change the sofa's color here!
        if (newState == true)
        {
            Debug.Log("CLIENT: A valuable was placed in this container.");
        }
        else
        {
            Debug.Log("CLIENT: A valuable was removed from this container.");
        }
    }
}