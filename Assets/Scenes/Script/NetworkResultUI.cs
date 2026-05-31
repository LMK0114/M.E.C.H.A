using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class NetworkResultUI : NetworkBehaviour {
    [Header("UI References")]
    [Tooltip("Drag the Panel that holds the result stuff here")]
    public RectTransform resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Animation Settings")]
    [Tooltip("Positive Y means it hides ABOVE the screen!")]
    public Vector2 offScreenPos = new Vector2(0, 1500);
    public Vector2 onScreenPos = new Vector2(0, 0);
    public float slideDuration = 0.5f;

    private PlayerMovement localPlayer;

    public override void OnNetworkSpawn() {
        if (!IsOwner) {
            gameObject.SetActive(false);
            return;
        }

        localPlayer = GetComponentInParent<PlayerMovement>();

        // Snap the panel off-screen immediately
        if (resultPanel != null) resultPanel.anchoredPosition = offScreenPos;

        // --- Listen to the Match Manager's 5-second sequence! ---
        NetworkMatchManager.OnResultPhaseStarted += ShowResult;
        NetworkMatchManager.OnShopPhaseStarted += HideResult;
    }

    public override void OnNetworkDespawn() {
        // Clean up events to prevent memory leaks
        NetworkMatchManager.OnResultPhaseStarted -= ShowResult;
        NetworkMatchManager.OnShopPhaseStarted -= HideResult;
    }

    private void ShowResult() {
        UpdateResultText();

        // Slide the results down from the top!
        StopAllCoroutines();
        StartCoroutine(SlidePanel(onScreenPos));
    }

    private void HideResult() {
        // Slide the results back up to the ceiling!
        StopAllCoroutines();
        StartCoroutine(SlidePanel(offScreenPos));
    }

    private void UpdateResultText() {
        if (resultText == null || localPlayer == null) return;

        // You can expand this later to show a detailed breakdown, 
        // but for now, we just want to guarantee the player sees their updated bank!
        resultText.text = $"ROUND OVER!\n\nYour Total Bank: ${localPlayer.totalMoney.Value}";
    }

    private IEnumerator SlidePanel(Vector2 targetPos) {
        if (resultPanel == null) yield break;

        Vector2 startPos = resultPanel.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < slideDuration) {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / slideDuration);
            resultPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        resultPanel.anchoredPosition = targetPos;
    }
}