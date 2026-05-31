using UnityEngine;
using Unity.Netcode;

public class NetworkEmpTrap : NetworkBehaviour {
    [Header("Trap Settings")]
    [SerializeField] private float slowDuration = 4f;
    [SerializeField] private float slowMultiplier = 0.5f; // Cuts speed in half

    [Header("Visual Cues")]
    [SerializeField] private GameObject glowingLight; // A light that pulses in the dark
    [SerializeField] private ParticleSystem explosionVFX;

    private bool hasTriggered = false;

    // Use Unity's physics engine to detect the Mecha walking in
    private void OnTriggerEnter(Collider other) {
        // Only the Server should do the math for traps to prevent cheating
        if (!IsServer || hasTriggered) return;

        // Check if the object that walked in is a Player
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null) {
            // Check if they are the Thief (Mecha)
            if (player.currentRole.Value == PlayerMovement.PlayerRole.Thief) {
                TriggerTrap(player);
            }
        }
    }

    private void TriggerTrap(PlayerMovement targetPlayer) {
        hasTriggered = true;

        // 1. Slow the player down (using Programmer B's existing code!)
        targetPlayer.ApplySlowdown(slowDuration, slowMultiplier);

        Debug.Log("SERVER: EMP Trap Triggered! Mecha slowed down.");

        // 2. Tell all clients to play the explosion visual and destroy the trap
        TriggerTrapEffectsRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void TriggerTrapEffectsRpc() {
        if (explosionVFX != null) {
            // Ensure the shockwave is flat on the ground
            explosionVFX.transform.SetParent(null);
            explosionVFX.transform.rotation = Quaternion.Euler(-90, 0, 0);
            explosionVFX.Play();

            // Add a screen shake or UI glitch effect here later!
            Destroy(explosionVFX.gameObject, 2f);
        }
        Destroy(gameObject);
    }

}