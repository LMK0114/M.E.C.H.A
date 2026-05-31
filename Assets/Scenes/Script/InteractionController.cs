using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI; // --- NEW: Required for the Image component ---

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
public class InteractionController : NetworkBehaviour {
    [Header("Detection Settings")]
    [SerializeField] private float highlightRadius = 2.5f;
    [SerializeField] private LayerMask interactableLayerMask = -1;

    [Header("Thief Search Settings")]
    [SerializeField] private float searchDuration = 3f;
    private float searchTimer;
    private bool isSearching;
    private BaseInteractable searchTarget;

    [Header("Looting UI References")]
    [Tooltip("Drag the LootProgressBarBg here")]
    [SerializeField] private GameObject lootingUIContainer;
    [Tooltip("Drag the LootProgressFill image here")]
    [SerializeField] private Image lootingProgressBar;

    private PlayerMovement playerMovement;
    private PlayerInput playerInput;
    private BaseInteractable currentTarget;

    // We use a small timer to prevent the debug logs from spamming the console 60 times a second
    private float debugLogTimer = 0f;

    private void Awake() {
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        lootingUIContainer.SetActive(false);
    }

    private void Update() {
        if (!IsSpawned || !IsOwner) return;

        // --- PHASE BLOCKER ---
        NetworkMatchManager matchManager = FindFirstObjectByType<NetworkMatchManager>();
        if (matchManager != null &&
           (matchManager.currentPhase.Value == NetworkMatchManager.GamePhase.Waiting ||
            matchManager.currentPhase.Value == NetworkMatchManager.GamePhase.RoundEnd)) {

            // If the phase changes while we are looting, cancel everything and hide the UI
            if (isSearching) CancelSearch();

            if (currentTarget != null) {
                currentTarget.SetHighlight(false);
                currentTarget = null;
            }
            return;
        }

        DetectNearbyInteractable();
        HandleInteractionInput();
    }

    private void HandleInteractionInput() {
        var interactAction = playerInput.actions["Interact"];

        if (playerMovement.currentRole.Value == PlayerMovement.PlayerRole.Owner) {
            if (interactAction.WasPressedThisFrame() && currentTarget != null) {
                Debug.Log("CLIENT: Owner instantly interacting...");
                currentTarget.OnInteract(playerMovement);
            }
        } else if (playerMovement.currentRole.Value == PlayerMovement.PlayerRole.Thief) {

            // --- 1. START SEARCHING ---
            if (interactAction.WasPressedThisFrame() && currentTarget != null && !isSearching) {
                // Instant Lockpick Check
                if (currentTarget is NetworkDoor && playerMovement.lockpicksLeft.Value > 0) {
                    UseLockpickServerRpc();
                    currentTarget.OnInteract(playerMovement);
                    return;
                }

                isSearching = true;
                searchTimer = 0f;
                searchTarget = currentTarget;

                // Turn on the UI!
                if (lootingUIContainer != null) lootingUIContainer.SetActive(true);
                if (lootingProgressBar != null) lootingProgressBar.fillAmount = 0f;

                Debug.Log($"CLIENT: Thief started interacting with {searchTarget.gameObject.name}. Don't move!");
            }

            // --- 2. SEARCH LOOP ---
            if (isSearching) {
                // If they walk away from the target, cancel it!
                if (currentTarget != searchTarget) {
                    CancelSearch();
                    Debug.Log("CLIENT: Interaction cancelled! You walked away.");
                    return;
                }

                searchTimer += Time.deltaTime;

                // --- NEW: Update the visual progress bar! ---
                if (lootingProgressBar != null) {
                    lootingProgressBar.fillAmount = searchTimer / searchDuration;
                }

                // --- 3. SUCCESS ---
                if (searchTimer >= searchDuration) {
                    Debug.Log("CLIENT: 3 Seconds complete! Sending request to Server...");
                    searchTarget.OnInteract(playerMovement);
                    CancelSearch(); // This safely resets variables and hides the UI
                }
            }
        }
    }

    // --- NEW HELPER: Safely wipes the search state and hides UI ---
    private void CancelSearch() {
        isSearching = false;
        searchTarget = null;
        searchTimer = 0f;
        if (lootingUIContainer != null) lootingUIContainer.SetActive(false);
    }

    private void DetectNearbyInteractable() {
        Collider[] hits = Physics.OverlapSphere(transform.position, highlightRadius, interactableLayerMask);

        bool shouldLogThisFrame = false;
        debugLogTimer -= Time.deltaTime;
        if (debugLogTimer <= 0f) {
            shouldLogThisFrame = true;
            debugLogTimer = 1f;
        }

        BaseInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in hits) {
            BaseInteractable interactable = col.GetComponent<BaseInteractable>();

            if (interactable == null) {
                interactable = col.GetComponentInParent<BaseInteractable>();
            }

            if (interactable != null) {
                Vector3 closestPoint = col.ClosestPoint(transform.position);
                float dist = Vector3.Distance(transform.position, closestPoint);
                if (dist < closestDistance) {
                    closestDistance = dist;
                    closest = interactable;
                }
            }
        }

        if (currentTarget != closest) {
            if (currentTarget != null) currentTarget.SetHighlight(false);
            currentTarget = closest;
            if (currentTarget != null) {
                currentTarget.SetHighlight(true);
            }
        }
    }

    public bool IsTargetingInteractable() {
        return currentTarget != null;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, highlightRadius);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void UseLockpickServerRpc() {
        if (playerMovement.lockpicksLeft.Value > 0) {
            playerMovement.lockpicksLeft.Value--;
        }
    }
}