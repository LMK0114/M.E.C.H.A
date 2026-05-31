using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class NetworkShopUI : NetworkBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the Panel that holds all the shop stuff here")]
    public RectTransform shopPanel;
    public TextMeshProUGUI bankBalanceText;
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;

    [Header("Animation Settings")]
    public Vector2 offScreenPos = new Vector2(0, -1500); // Hides below the screen
    public Vector2 onScreenPos = new Vector2(0, 0);      // Center of the screen
    public float slideDuration = 0.5f;

    private PlayerMovement localPlayer;
    private NetworkMatchManager matchManager;
    private bool hasClickedReady = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        localPlayer = GetComponentInParent<PlayerMovement>();
        matchManager = FindFirstObjectByType<NetworkMatchManager>();

        // Snap the panel off-screen immediately
        if (shopPanel != null) shopPanel.anchoredPosition = offScreenPos;

        // Listen for the Ready Button click
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);

        // Listen to Phase changes (to know when to slide DOWN)
        if (matchManager != null) matchManager.currentPhase.OnValueChanged += OnPhaseChanged;

        localPlayer.totalMoney.OnValueChanged += OnMoneyChanged;
        OnMoneyChanged(0, localPlayer.totalMoney.Value); // Force initial update

        // --- NEW: Listen to the Match Manager's 5-second delay signal! ---
        NetworkMatchManager.OnShopPhaseStarted += ShowShop;
    }

    public override void OnNetworkDespawn()
    {
        if (matchManager != null) matchManager.currentPhase.OnValueChanged -= OnPhaseChanged;
        if (localPlayer != null) localPlayer.totalMoney.OnValueChanged -= OnMoneyChanged;

        // Clean up the static event so we don't cause memory leaks
        NetworkMatchManager.OnShopPhaseStarted -= ShowShop;
    }

    private void OnMoneyChanged(int previousValue, int newValue)
    {
        if (bankBalanceText != null)
            bankBalanceText.text = $"Bank: ${newValue}";
    }

    // --- NEW: This only triggers AFTER the 5-second Result Screen finishes ---
    private void ShowShop()
    {
        // Reset the ready button for the new round
        hasClickedReady = false;
        if (readyButtonText != null) readyButtonText.text = "Ready";
        if (readyButton != null) readyButton.interactable = true;

        // Slide the shop up!
        StopAllCoroutines();
        StartCoroutine(SlidePanel(onScreenPos));
    }

    private void OnPhaseChanged(NetworkMatchManager.GamePhase oldPhase, NetworkMatchManager.GamePhase newPhase)
    {
        // We only care about sliding the shop AWAY when the round fully resets
        if (oldPhase == NetworkMatchManager.GamePhase.RoundEnd && newPhase != NetworkMatchManager.GamePhase.RoundEnd)
        {
            StopAllCoroutines();
            StartCoroutine(SlidePanel(offScreenPos));
        }
    }

    private void OnReadyClicked()
    {
        if (hasClickedReady || localPlayer == null) return;

        hasClickedReady = true;
        if (readyButtonText != null) readyButtonText.text = "Waiting for other player...";
        if (readyButton != null) readyButton.interactable = false;

        // Tell the server we are ready!
        localPlayer.SetReadyServerRpc(true);
    }

    private IEnumerator SlidePanel(Vector2 targetPos)
    {
        if (shopPanel == null) yield break;

        Vector2 startPos = shopPanel.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            // SmoothStep makes the slide start fast and slow down at the end smoothly
            float t = Mathf.SmoothStep(0, 1, elapsedTime / slideDuration);
            shopPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        shopPanel.anchoredPosition = targetPos;
    }
}