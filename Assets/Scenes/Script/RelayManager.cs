using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    [Header("UI Groups")]
    [SerializeField] GameObject mainMenuView; // The group with Start/Shutdown
    [SerializeField] GameObject relayView;    // The group with Host/Join
    [SerializeField] GameObject NewCredit;

    [Header("Main Menu Buttons")]
    [SerializeField] GameObject Title;
    [SerializeField] Button openRelayButton;  // Your "START" button
    [SerializeField] Button shutdownButton;
    [SerializeField] Button creditsButton;

    [Header("Relay Setup")]
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] Button startButton;
    [SerializeField] Button RelaybackMenuButton;

    [Header("Credits UI")]
    [SerializeField] Button closeCreditsButton;

    [SerializeField] GameObject codetextInLobby;
    [SerializeField] TextMeshProUGUI codetextInLobbyText;

    async void Start()
    {
        // 1. Initial UI State
        mainMenuView.SetActive(true);
        relayView.SetActive(false);
        startButton.gameObject.SetActive(false);
        codetextInLobby.SetActive(false);
        NewCredit.SetActive(false);

        // 2. Initialize Services
        // 2. Initialize Services (SAFELY)
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // 3. Listeners
        openRelayButton.onClick.AddListener(ShowRelayMenu);
        shutdownButton.onClick.AddListener(QuitGame);
        RelaybackMenuButton.onClick.AddListener(BackMenuButton);

        creditsButton.onClick.AddListener(ShowCredits);
        closeCreditsButton.onClick.AddListener(HideCredits);

        hostButton.onClick.AddListener(CreateRelay);
        joinButton.onClick.AddListener(() => JoinRelay(joinInput.text));
        startButton.onClick.AddListener(StartGame); // Add this

        // --- NEW: ARE WE RETURNING FROM A MATCH? ---
        // If the NetworkManager is already running, skip the setup and jump straight to the lobby!
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsConnectedClient))
        {
            mainMenuView.SetActive(false);
            relayView.SetActive(false);
            Title.SetActive(false);

            if (NetworkManager.Singleton.IsServer)
            {
                // Only the Host gets the button to start the next game
                startButton.gameObject.SetActive(true);
            }
        }

    }

    void BackMenuButton()
    {
        mainMenuView?.SetActive(true);
        relayView.SetActive(false);
    }
    void ShowCredits()
    {
        mainMenuView.SetActive(false); 
        NewCredit.SetActive(true);
    }

    void HideCredits()
    {
        NewCredit.SetActive(false);
        mainMenuView.SetActive(true);  
    }

    void ShowRelayMenu()
    {
        mainMenuView.SetActive(false);
        relayView.SetActive(true);
    }

    void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game is shutting down...");
    }

    async void CreateRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();
        startButton.gameObject.SetActive(true);

        relayView.SetActive(false);
        Title.SetActive(false);
        
        codetextInLobbyText.text = "Code: " + joinCode;
        codetextInLobby.SetActive(true);
    }

    async void JoinRelay(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();

        // Hide the UI or show a "Waiting for Host" message
        relayView.SetActive(false);
        Title.SetActive(false);
    }

    void StartGame()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }
}
