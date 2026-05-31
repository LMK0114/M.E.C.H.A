using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour {
    public enum PlayerRole {
        Owner,
        Thief
    }

    [Header("Role")]
    public NetworkVariable<PlayerRole> currentRole = new NetworkVariable<PlayerRole>(PlayerRole.Owner, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Inventory (Backend)")]
    // Valuables
    public NetworkVariable<int> valuablesToHide = new NetworkVariable<int>(5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> valuablesStolen = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Tools
    public NetworkVariable<int> empTrapsLeft = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> lockpicksLeft = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Economy")]
    public NetworkVariable<int> totalMoney = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- ADD THESE TWO LINES ---
    public NetworkVariable<int> lifetimeHidden = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> lifetimeFound = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Components
    private Rigidbody rb;
    private PlayerInput playerInput;

    // Movement cache
    private Vector3 movementInput;
    private float speedMultiplier = 1f;
    private Coroutine slowCoroutine;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn() {
        if (!IsOwner) {
            // Disable input for other players
            if (playerInput != null) playerInput.enabled = false;
            return;
        }

        if (playerInput != null) playerInput.enabled = true;
    }

    private void Update() {
        // If not spawned or not the owner, do nothing
        if (!IsSpawned || !IsOwner) return;

        Vector2 move = playerInput.actions["Move"].ReadValue<Vector2>();
        movementInput = new Vector3(move.x, 0f, move.y);

        if (movementInput.sqrMagnitude > 1f)
            movementInput.Normalize();
    }

    private void FixedUpdate() {
        // Only move if we own this player
        if (!IsOwner) return;

        // --- PHASE BLOCKER ---
        NetworkMatchManager matchManager = FindFirstObjectByType<NetworkMatchManager>();
        if (matchManager != null &&
           (matchManager.currentPhase.Value == NetworkMatchManager.GamePhase.Waiting ||
            matchManager.currentPhase.Value == NetworkMatchManager.GamePhase.RoundEnd)) {
            // Force the player to stop sliding
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (movementInput.sqrMagnitude > 0.01f) {
            // 1. Position Movement
            Vector3 newPosition = rb.position + movementInput * (moveSpeed * speedMultiplier * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);

            // 2. Rotation Logic
            Quaternion targetRotation = Quaternion.LookRotation(movementInput);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 15f));
        }
    }

    public void ApplySlowdown(float duration, float multiplier) {
        // Server triggers slowdown on all clients
        if (!IsServer) return;

        ApplySlowdownClientRpc(duration, multiplier);
    }

    [ClientRpc]
    private void ApplySlowdownClientRpc(float duration, float multiplier) {
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowdownRoutine(duration, multiplier));
    }

    private System.Collections.IEnumerator SlowdownRoutine(float duration, float multiplier) {
        speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
        slowCoroutine = null;
    }

    [ClientRpc]
    public void SetRoleClientRpc(PlayerRole newRole) {
        // Server sets the role via NetworkVariable
        if (IsServer) {
            currentRole.Value = newRole;
        }
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, Quaternion rotation) {
        // Teleport on all clients
        transform.position = position;
        transform.rotation = rotation;

        if (rb != null) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SetReadyServerRpc(bool readyState) {
        isReady.Value = readyState;
    }
}