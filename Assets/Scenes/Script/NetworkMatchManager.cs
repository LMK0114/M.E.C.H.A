using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public class NetworkMatchManager : NetworkBehaviour
{
    public enum GamePhase { Waiting, Setup, Action, RoundEnd }

    [Header("Networked State")]
    public NetworkVariable<GamePhase> currentPhase = new NetworkVariable<GamePhase>(GamePhase.Waiting);
    public NetworkVariable<int> timerSeconds = new NetworkVariable<int>(0);

    // --- NEW: Loop Tracking ---
    public NetworkVariable<int> currentRound = new NetworkVariable<int>(1);
    private bool isResetting = false;

    [Header("Phase Durations")]
    [SerializeField] private float setupDuration = 30f;
    [SerializeField] private float actionDuration = 60f;

    [Header("Spawn Points")]
    [SerializeField] private Transform p1HouseSpawn;
    [SerializeField] private Transform p2HouseSpawn;

    [Header("Scene UI")]
    [SerializeField] private TMPro.TextMeshProUGUI bigCenterText;
    private Coroutine popupCoroutine;

    public float serverInternalTimer { get; private set; }

    public static event Action OnResultPhaseStarted;
    public static event Action OnShopPhaseStarted;

    public override void OnNetworkSpawn()
    {
        currentPhase.OnValueChanged += OnPhaseChanged;
        timerSeconds.OnValueChanged += OnTimerChanged;

        if (IsServer)
        {
            Invoke(nameof(StartSetupPhase), 2f);
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // --- NEW: The Janitor Watcher ---
        if (currentPhase.Value == GamePhase.RoundEnd && !isResetting)
        {
            CheckAllPlayersReady();
        }

        if (currentPhase.Value == GamePhase.Waiting || currentPhase.Value == GamePhase.RoundEnd)
            return;

        serverInternalTimer -= Time.deltaTime;

        int secondsLeft = Mathf.CeilToInt(serverInternalTimer);
        if (timerSeconds.Value != secondsLeft && secondsLeft >= 0)
        {
            timerSeconds.Value = secondsLeft;
        }

        if (serverInternalTimer <= 0)
        {
            AdvancePhase();
        }
    }

    private void StartSetupPhase()
    {
        currentPhase.Value = GamePhase.Setup;
        serverInternalTimer = setupDuration;
        AssignRolesAndTeleport(PlayerMovement.PlayerRole.Owner);

        // Reset everything for the new round
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            player.empTrapsLeft.Value = 3;
            player.lockpicksLeft.Value = 3;
            player.valuablesToHide.Value = 5;
            player.valuablesStolen.Value = 0;
            player.isReady.Value = false; // Reset the shop button!
        }

        NetworkDoor[] allDoors = FindObjectsByType<NetworkDoor>(FindObjectsSortMode.None);
        foreach (var door in allDoors)
        {
            if (door.IsSpawned)
            {
                door.SetPhaseVisibilityRpc(false);
                door.isLocked.Value = true;
            }
        }

        ShowPopupClientRpc($"ROUND {currentRound.Value}\nHIDE YOUR OBJECTS!");
        Debug.Log($"SERVER: Round {currentRound.Value} Setup Started!");
    }

    private void StartActionPhase()
    {
        currentPhase.Value = GamePhase.Action;
        serverInternalTimer = actionDuration;
        AssignRolesAndTeleport(PlayerMovement.PlayerRole.Thief);

        NetworkDoor[] allDoors = FindObjectsByType<NetworkDoor>(FindObjectsSortMode.None);
        foreach (var door in allDoors)
        {
            if (door.IsSpawned) door.SetPhaseVisibilityRpc(true);
        }

        ShowPopupClientRpc($"ROUND {currentRound.Value}\nLOOT THE ITEMS!");
        Debug.Log($"SERVER: Round {currentRound.Value} Action Started!");
    }

    private void AdvancePhase()
    {
        if (currentPhase.Value == GamePhase.Setup) StartActionPhase();
        else if (currentPhase.Value == GamePhase.Action)
        {
            currentPhase.Value = GamePhase.RoundEnd;
            CalculateEndRoundMoney();
            StartCoroutine(RoundEndUISequence());
        }
    }

    // --- NEW: THE BOARD RESET SEQUENCE ---
    private void CheckAllPlayersReady()
    {
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        if (allPlayers.Length < 2) return;

        foreach (var player in allPlayers)
        {
            if (!player.isReady.Value) return; // If anyone is false, cancel.
        }

        // Everyone hit ready! Let's clean the board.
        isResetting = true;
        StartCoroutine(CleanBoardAndNextRound());
    }

    private System.Collections.IEnumerator CleanBoardAndNextRound()
    {
        Debug.Log("SERVER: Both players ready! Sweeping the board...");

        // Changing to waiting forces the Shop UI to slide away instantly
        currentPhase.Value = GamePhase.Waiting;

        // --- CHECK IF GAME IS OVER ---
        if (currentRound.Value >= 3)
        {
            // 1. Gather the final stats on the server
            PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            var sorted = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(allPlayers, p => p.OwnerClientId));

            if (sorted.Length >= 2)
            {
                // 2. Send the exact final numbers to ALL computers
                PrepareForGameOverClientRpc(
                    sorted[0].lifetimeHidden.Value, sorted[0].lifetimeFound.Value, sorted[0].totalMoney.Value,
                    sorted[1].lifetimeHidden.Value, sorted[1].lifetimeFound.Value, sorted[1].totalMoney.Value
                );
            }

            // 3. WAIT half a second for the internet to deliver the message before destroying the scene!
            yield return new WaitForSeconds(0.5f);

            // 4. Tell Netcode to load the Game Over scene for everyone
            NetworkManager.Singleton.SceneManager.LoadScene("GameOver", UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield break; // Stop this coroutine
        }

        // 1. DELETE TRAPS
        GameObject[] traps = GameObject.FindGameObjectsWithTag("Trap");
        foreach (var trap in traps)
        {
            if (trap.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }

        // 2. EMPTY CONTAINERS
        NetworkLootContainer[] containers = FindObjectsByType<NetworkLootContainer>(FindObjectsSortMode.None);
        foreach (var c in containers) { c.hasValuable.Value = false; }

        currentRound.Value++;
        isResetting = false;

        yield return new WaitForSeconds(0.5f); // Give network a half-second to delete traps

        // 3. START THE NEXT ROUND!
        StartSetupPhase();
    }
    // -------------------------------------



    private System.Collections.IEnumerator RoundEndUISequence()
    {
        TriggerResultUIClientRpc();
        yield return new WaitForSeconds(5f);
        TriggerShopUIClientRpc();
    }

    [ClientRpc] private void TriggerResultUIClientRpc() { OnResultPhaseStarted?.Invoke(); }
    [ClientRpc] private void TriggerShopUIClientRpc() { OnShopPhaseStarted?.Invoke(); }

    private void AssignRolesAndTeleport(PlayerMovement.PlayerRole newRole)
    {
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerMovement player = allPlayers[i];
            player.SetRoleClientRpc(newRole);

            Transform targetSpawn = (newRole == PlayerMovement.PlayerRole.Owner)
                ? ((i == 0) ? p1HouseSpawn : p2HouseSpawn)
                : ((i == 0) ? p2HouseSpawn : p1HouseSpawn);

            if (targetSpawn != null)
            {
                player.TeleportClientRpc(targetSpawn.position, targetSpawn.rotation);
            }
        }
    }

    private void CalculateEndRoundMoney()
    {
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        if (allPlayers.Length == 2)
        {
            // Sort by ClientId: 0 is Host (Blue), 1 is Client (Red)
            var sortedPlayers = allPlayers.OrderBy(p => p.OwnerClientId).ToArray();
            PlayerMovement p1_Blue = sortedPlayers[0];
            PlayerMovement p2_Red = sortedPlayers[1];

            // 1. Calculate Money
            int p1Survived = Mathf.Max(0, (5 - p1_Blue.valuablesToHide.Value) - p2_Red.valuablesStolen.Value);
            p1_Blue.totalMoney.Value += (p1Survived * 100) + (p1_Blue.valuablesStolen.Value * 200);

            int p2Survived = Mathf.Max(0, (5 - p2_Red.valuablesToHide.Value) - p1_Blue.valuablesStolen.Value);
            p2_Red.totalMoney.Value += (p2Survived * 100) + (p2_Red.valuablesStolen.Value * 200);

            // 2. Add to Lifetime Stats for the Game Over Screen!
            // Items hidden is (5 - whatever is left to hide)
            p1_Blue.lifetimeHidden.Value += (5 - p1_Blue.valuablesToHide.Value);
            p1_Blue.lifetimeFound.Value += p1_Blue.valuablesStolen.Value;

            p2_Red.lifetimeHidden.Value += (5 - p2_Red.valuablesToHide.Value);
            p2_Red.lifetimeFound.Value += p2_Red.valuablesStolen.Value;
        }
    }

    // --- POPUP LOGIC ---
    [ClientRpc]
    private void ShowPopupClientRpc(string message)
    {
        if (bigCenterText != null)
        {
            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            popupCoroutine = StartCoroutine(FlashTextRoutine(message));
        }
    }

    // --- NEW HELPER METHOD ---
    [ClientRpc]
    private void PrepareForGameOverClientRpc(int bHide, int bFind, int bMoney, int rHide, int rFind, int rMoney)
    {
        GameOverData.BlueHide = bHide;
        GameOverData.BlueFind = bFind;
        GameOverData.BlueTotalMoney = bMoney;

        GameOverData.RedHide = rHide;
        GameOverData.RedFind = rFind;
        GameOverData.RedTotalMoney = rMoney;

        Debug.Log("CLIENT: Received final game over stats from server!");
    }

    private System.Collections.IEnumerator FlashTextRoutine(string message)
    {
        bigCenterText.text = message;
        yield return new WaitForSeconds(3f);
        bigCenterText.text = "";
        popupCoroutine = null;
    }

    private void OnTimerChanged(int previousValue, int newValue)
    {
        if (newValue % 5 == 0 && newValue > 0) Debug.Log($"Time Remaining: {newValue}");
    }

    private void OnPhaseChanged(GamePhase previousValue, GamePhase newValue)
    {
        Debug.Log($"CLIENT: Phase changed to {newValue}");
    }

    public override void OnNetworkDespawn()
    {
        currentPhase.OnValueChanged -= OnPhaseChanged;
        timerSeconds.OnValueChanged -= OnTimerChanged;
    }
}