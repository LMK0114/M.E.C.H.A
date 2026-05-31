using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class NetworkPlayerUI : NetworkBehaviour {

    [Header("--- Main UI Container ---")]
    public GameObject mainUIContainer;

    [Header("--- Tool Slot (Trap / Lockpick) ---")]
    public TextMeshProUGUI trapText;
    public Image trapHighlightImage;
    [Tooltip("Drag the actual Icon Image object here (child of the Highlight Panel)")]
    public Image toolIconImage;
    [Tooltip("Drag your Trap Sprite here")]
    public Sprite trapSprite;
    [Tooltip("Drag your Lockpick Sprite here")]
    public Sprite lockpickSprite;

    [Header("--- Valuables UI References ---")]
    public TextMeshProUGUI itemText;
    public GameObject[] itemIconBoxes;

    [Header("--- Visual Settings ---")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.red;

    private PlayerMovement localPlayer;
    private NetworkMatchManager matchManager;

    public bool isTrapSelected = false;

    public override void OnNetworkSpawn() {
        if (!IsOwner) {
            gameObject.SetActive(false);
            return;
        }

        localPlayer = GetComponentInParent<PlayerMovement>();
        matchManager = FindFirstObjectByType<NetworkMatchManager>();

        // Subscribe to Server Updates
        localPlayer.currentRole.OnValueChanged += OnRoleChanged;
        localPlayer.empTrapsLeft.OnValueChanged += OnTrapsChanged;
        localPlayer.lockpicksLeft.OnValueChanged += OnLockpicksChanged;
        localPlayer.valuablesToHide.OnValueChanged += OnValuablesChanged;
        localPlayer.valuablesStolen.OnValueChanged += OnValuablesStolenChanged;

        if (matchManager != null) {
            matchManager.currentPhase.OnValueChanged += OnPhaseChanged;
            OnPhaseChanged(matchManager.currentPhase.Value, matchManager.currentPhase.Value);
        }

        // Initialize the UI for whatever role we spawned as
        OnRoleChanged(localPlayer.currentRole.Value, localPlayer.currentRole.Value);
        OnValuablesChanged(0, localPlayer.valuablesToHide.Value);
        UpdateTrapHighlight();
    }

    public override void OnNetworkDespawn() {
        if (localPlayer != null) {
            localPlayer.currentRole.OnValueChanged -= OnRoleChanged;
            localPlayer.empTrapsLeft.OnValueChanged -= OnTrapsChanged;
            localPlayer.lockpicksLeft.OnValueChanged -= OnLockpicksChanged;
            localPlayer.valuablesToHide.OnValueChanged -= OnValuablesChanged;
            localPlayer.valuablesStolen.OnValueChanged -= OnValuablesStolenChanged;
        }
        if (matchManager != null) {
            matchManager.currentPhase.OnValueChanged -= OnPhaseChanged;
        }
    }

    // --- PHASE HIDING LOGIC ---
    private void OnPhaseChanged(NetworkMatchManager.GamePhase oldPhase, NetworkMatchManager.GamePhase newPhase) {
        if (mainUIContainer == null) return;

        if (newPhase == NetworkMatchManager.GamePhase.Waiting) {
            mainUIContainer.SetActive(false);
            if (isTrapSelected) {
                isTrapSelected = false;
                UpdateTrapHighlight();
            }
        } else {
            mainUIContainer.SetActive(true);
        }
    }

    // --- PUBLIC INPUT LOGIC ---
    public void ToggleTrapSelection() {
        if (mainUIContainer != null && !mainUIContainer.activeSelf) return;

        // Check inventory depending on who we are playing as
        int currentToolCount = localPlayer.currentRole.Value == PlayerMovement.PlayerRole.Owner
                               ? localPlayer.empTrapsLeft.Value
                               : localPlayer.lockpicksLeft.Value;

        if (currentToolCount <= 0) return;

        isTrapSelected = !isTrapSelected;
        UpdateTrapHighlight();
    }

    public void UpdateTrapHighlight() {
        if (trapHighlightImage != null) {
            trapHighlightImage.color = isTrapSelected ? highlightColor : normalColor;
            trapHighlightImage.transform.localScale = isTrapSelected ? new Vector3(1.1f, 1.1f, 1f) : Vector3.one;
        }
    }

    // --- EVENT LISTENERS ---

    // Swaps the icon and text dynamically if the server changes our role
    private void OnRoleChanged(PlayerMovement.PlayerRole oldRole, PlayerMovement.PlayerRole newRole) {
        if (newRole == PlayerMovement.PlayerRole.Owner) {
            if (toolIconImage != null && trapSprite != null) toolIconImage.sprite = trapSprite;
            OnTrapsChanged(0, localPlayer.empTrapsLeft.Value);
            OnValuablesChanged(0, localPlayer.valuablesToHide.Value); // Force Owner UI
        } else {
            if (toolIconImage != null && lockpickSprite != null) toolIconImage.sprite = lockpickSprite;
            OnLockpicksChanged(0, localPlayer.lockpicksLeft.Value);
            OnValuablesStolenChanged(0, localPlayer.valuablesStolen.Value); // Force Thief UI
        }

        if (isTrapSelected) {
            isTrapSelected = false;
            UpdateTrapHighlight();
        }
    }

    private void OnTrapsChanged(int previousValue, int newValue) {
        if (localPlayer.currentRole.Value != PlayerMovement.PlayerRole.Owner) return; // Ignore if we are the Thief

        if (trapText != null) trapText.text = newValue + " traps left";
        if (newValue <= 0 && isTrapSelected) {
            isTrapSelected = false;
            UpdateTrapHighlight();
        }
    }

    private void OnLockpicksChanged(int previousValue, int newValue) {
        if (localPlayer.currentRole.Value != PlayerMovement.PlayerRole.Thief) return; // Ignore if we are the Owner

        if (trapText != null) trapText.text = newValue + " lockpicks left";
        if (newValue <= 0 && isTrapSelected) {
            isTrapSelected = false;
            UpdateTrapHighlight();
        }
    }

    private void OnValuablesChanged(int previousValue, int newValue) {
        if (localPlayer.currentRole.Value != PlayerMovement.PlayerRole.Owner) return;

        if (itemText != null) itemText.text = newValue + " items left to hide";

        for (int i = 0; i < itemIconBoxes.Length; i++) {
            if (itemIconBoxes[i] != null) itemIconBoxes[i].SetActive(i < newValue);
        }
    }

    // --- ADDED THIS FOR THE THIEF ---
    private void OnValuablesStolenChanged(int previousValue, int newValue) {
        if (localPlayer.currentRole.Value != PlayerMovement.PlayerRole.Thief) return;

        if (itemText != null) itemText.text = newValue + " valuables stolen!";

        // Thief starts with 0. As newValue goes up (1, 2, 3), icons turn ON!
        for (int i = 0; i < itemIconBoxes.Length; i++) {
            if (itemIconBoxes[i] != null) itemIconBoxes[i].SetActive(i < newValue);
        }
    }
}