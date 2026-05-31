using UnityEngine;
using Unity.Netcode;

public class NetworkDoor : BaseInteractable
{
    [Header("Door Components")]
    [SerializeField] private GameObject doorVisuals;
    [SerializeField] private Collider doorCollider;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorOpenSFX;

    [Header("Networked State")]
    public NetworkVariable<bool> isLocked = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // --- The Ghost Door Fix ---
    public override void SetHighlight(bool active)
    {
        // If the door is unlocked, or if it is currently hidden, REFUSE TO GLOW!
        if (!isLocked.Value || (doorVisuals != null && !doorVisuals.activeSelf))
        {
            base.SetHighlight(false);
            return;
        }

        base.SetHighlight(active);
    }

    // 1. PHASE CONTROL
    [Rpc(SendTo.Everyone)]
    public void SetPhaseVisibilityRpc(bool isVisible)
    {
        if (doorVisuals != null) doorVisuals.SetActive(isVisible);
        if (doorCollider != null) doorCollider.enabled = isVisible;

        if (!isVisible)
        {
            base.SetHighlight(false);
        }
    }

    // 2. INTERACTION
    public override void OnInteract(PlayerMovement interactingPlayer)
    {
        if (interactingPlayer.currentRole.Value == PlayerMovement.PlayerRole.Thief && isLocked.Value)
        {
            UnlockDoorServerRpc();
        }
    }

    // 3. SERVER LOGIC
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void UnlockDoorServerRpc()
    {
        if (isLocked.Value)
        {
            isLocked.Value = false;
            Debug.Log("SERVER: Door was lockpicked!");

            // Tell ALL players' computers to play the door sound!
            PlayDoorSoundRpc();

            // Tell all clients to visually open/hide the door directly
            SetPhaseVisibilityRpc(false);
        }
    }

    // 4. AUDIO SYNC
    [Rpc(SendTo.Everyone)]
    private void PlayDoorSoundRpc()
    {
        if (audioSource != null && doorOpenSFX != null)
        {
            // PlayOneShot is great because it plays the sound fully even if the door visuals turn off
            audioSource.PlayOneShot(doorOpenSFX);
        }
    }
}