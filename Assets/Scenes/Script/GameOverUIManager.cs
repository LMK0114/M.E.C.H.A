// GameOverUIManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameOverUIManager : MonoBehaviour
{
    [Header("Winner Announcement")]
    public TextMeshProUGUI winnerText; // Put this at the top of the screen!

    [Header("Red Player Stats")]
    public TextMeshProUGUI redHideText;
    public TextMeshProUGUI redFindText;
    public TextMeshProUGUI redTotalText;

    [Header("Blue Player Stats")]
    public TextMeshProUGUI blueHideText;
    public TextMeshProUGUI blueFindText;
    public TextMeshProUGUI blueTotalText;

    [Header("Controls")]
    public Button mainMenuButton;

    [Header("Lobby Teleport Positions")]
    [Tooltip("Drag an empty GameObject here for the Host's lobby spot")]
    public Transform hostLobbySpawn;
    [Tooltip("Drag an empty GameObject here for the Client's lobby spot")]
    public Transform clientLobbySpawn;

    private void Start()
    {
        // 1. Populate Red Stats
        if (redHideText != null) redHideText.text = GameOverData.RedHide.ToString();
        if (redFindText != null) redFindText.text = GameOverData.RedFind.ToString();
        if (redTotalText != null) redTotalText.text = "$" + GameOverData.RedTotalMoney.ToString();

        // 2. Populate Blue Stats
        if (blueHideText != null) blueHideText.text = GameOverData.BlueHide.ToString();
        if (blueFindText != null) blueFindText.text = GameOverData.BlueFind.ToString();
        if (blueTotalText != null) blueTotalText.text = "$" + GameOverData.BlueTotalMoney.ToString();

        // 3. Determine Winner
        if (winnerText != null)
        {
            if (GameOverData.RedTotalMoney > GameOverData.BlueTotalMoney)
            {
                winnerText.text = "P1 WINS!";
                winnerText.color = Color.red;
            }
            else if (GameOverData.BlueTotalMoney > GameOverData.RedTotalMoney)
            {
                winnerText.text = "P2 WINS!";
                winnerText.color = Color.blue;
            }
            else
            {
                winnerText.text = "IT'S A TIE!";
                winnerText.color = Color.white;
            }
        }

        // 4. Setup Button
        if (mainMenuButton != null)
        {
            // If this player is NOT the Host, they can't click the button.
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                mainMenuButton.interactable = false;

                // Optional: Change the button text so the client knows what is happening
                TextMeshProUGUI btnText = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Wait...";
            }
            else
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }
    }

    private void ReturnToMainMenu()
    {
        // Only the Server (Host) is allowed to move players
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // Only the Server (Host) is allowed to change scenes while connected
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                Debug.Log("SERVER: Loading the Lobby Scene for all players...");

                // This forces ALL connected clients to load the MainMenu scene with the Host.
                // Replace "MainMenu" with the exact spelling of your lobby scene!
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }
}