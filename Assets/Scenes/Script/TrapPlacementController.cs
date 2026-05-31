using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
public class TrapPlacementController : NetworkBehaviour {
    [Header("Preview Settings")]
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private float placementDistance = 2f;
    [SerializeField] private float trapRadius = 0.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Dependencies")]
    [SerializeField] private GameObject realTrapPrefab;

    [Header("Fine-Tuning")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    private PlayerMovement playerMovement;
    private PlayerInput playerInput;
    private NetworkPlayerUI playerUI;
    private NetworkMatchManager matchManager;

    private GameObject currentPreview;
    private bool isPreviewActive = false;
    private bool canPlaceHere = false;

    // --- ADDED: This remembers which way you last pressed WASD ---
    private Vector3 lastLookDirection = Vector3.forward;

    private void Awake() {
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
        playerUI = GetComponentInChildren<NetworkPlayerUI>(true);
    }

    private void Update() {
        if (!IsOwner || !IsSpawned) return;

        if (matchManager == null) {
            matchManager = FindFirstObjectByType<NetworkMatchManager>();
            if (matchManager == null) {
                if (isPreviewActive) TogglePreview();
                return;
            }
        }

        if (matchManager.currentPhase.Value == NetworkMatchManager.GamePhase.Waiting) {
            if (isPreviewActive) TogglePreview();
            return;
        }

        // We track WASD regardless of role now!
        Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f) {
            lastLookDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        // NOTE: We REMOVED the hard block here so the Thief can press '1'
        HandleInput();

        if (isPreviewActive) {
            UpdatePreviewPosition();
        }
    }

    private void HandleInput() {
        // 1. PRESS '1' - Works for BOTH roles now
        if (playerInput.actions["ToggleTrap"].WasPressedThisFrame()) {
            // Check inventory based on role
            int toolCount = playerMovement.currentRole.Value == PlayerMovement.PlayerRole.Owner
                            ? playerMovement.empTrapsLeft.Value
                            : playerMovement.lockpicksLeft.Value;

            if (toolCount > 0) {
                // Both roles get the UI highlight
                if (playerUI != null) playerUI.ToggleTrapSelection();

                // ONLY the Owner gets to see the 3D Ghost Preview trap
                if (playerMovement.currentRole.Value == PlayerMovement.PlayerRole.Owner) {
                    TogglePreview();
                }
            }
        }

        // 2. PRESS 'SPACEBAR' - ONLY works if Preview is active (which only Owner gets)
        if (isPreviewActive && playerInput.actions["PlaceTrap"].WasPressedThisFrame()) {
            if (canPlaceHere) {
                PlaceTrapServerRpc(currentPreview.transform.position, currentPreview.transform.rotation);
                TogglePreview();
                if (playerUI != null && playerUI.isTrapSelected) playerUI.ToggleTrapSelection();
            }
        }
    }

    private void TogglePreview() {
        isPreviewActive = !isPreviewActive;

        if (isPreviewActive) {
            if (previewPrefab != null && currentPreview == null)
                currentPreview = Instantiate(previewPrefab);
        } else {
            if (currentPreview != null) Destroy(currentPreview);
        }
    }

    private void UpdatePreviewPosition() {
        if (currentPreview == null) return;

        Vector3 rayStart = transform.position + Vector3.up * 0.5f;

        // --- CHANGED: Use WASD direction instead of player body direction ---
        Vector3 forwardDir = lastLookDirection;

        Vector3 targetPos = rayStart + (forwardDir * placementDistance);
        targetPos.y = transform.position.y;

        // WALL CLIPPING FIX
        if (Physics.Raycast(rayStart, forwardDir, out RaycastHit hit, placementDistance, obstacleLayer)) {
            targetPos = hit.point - (forwardDir * trapRadius);
        }

        currentPreview.transform.position = targetPos;

        // --- CHANGED: Rotate the trap to face the WASD direction ---
        Quaternion targetRotation = Quaternion.LookRotation(lastLookDirection);
        Quaternion manualOffset = Quaternion.Euler(rotationOffset);
        currentPreview.transform.rotation = targetRotation * manualOffset;

        // OVERLAP FIX
        Collider[] overlaps = Physics.OverlapSphere(targetPos, trapRadius, obstacleLayer);
        canPlaceHere = (overlaps.Length == 0);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PlaceTrapServerRpc(Vector3 spawnPosition, Quaternion spawnRotation) {
        if (playerMovement.empTrapsLeft.Value > 0) {
            playerMovement.empTrapsLeft.Value--;

            if (realTrapPrefab != null) {
                GameObject newTrap = Instantiate(realTrapPrefab, spawnPosition, spawnRotation);
                NetworkObject netObj = newTrap.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Spawn();
            }
        }
    }
}