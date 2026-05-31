using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraSetup : NetworkBehaviour
{
    [SerializeField] private bool isLocalTest = true;
    private CinemachineCamera virtualCamera;
    private bool isAttached = false;
    private string currentSceneName = "";

    public override void OnNetworkSpawn()
    {
        // Only setup camera for the local player
        if (!IsOwner) return;

        // For testing in local mode
        if (!isLocalTest && !IsOwner) return;

        // Start searching for camera
        StartSearchingForCamera();
    }

    private void StartSearchingForCamera()
    {
        // Get current scene name
        currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"CameraSetup: Starting search in scene: {currentSceneName}");

        // Reset attachment flag when scene changes
        isAttached = false;
        virtualCamera = null;

        // Start searching continuously
        InvokeRepeating(nameof(FindAndAttachCamera), 0.1f, 0.2f);
    }

    private void FindAndAttachCamera()
    {
        // Check if scene has changed
        string newSceneName = SceneManager.GetActiveScene().name;
        if (newSceneName != currentSceneName)
        {
            Debug.Log($"Scene changed from {currentSceneName} to {newSceneName}, restarting search");
            currentSceneName = newSceneName;
            isAttached = false;
            virtualCamera = null;
        }

        // If already attached, stop searching
        if (isAttached && virtualCamera != null && virtualCamera.Follow == transform)
        {
            CancelInvoke(nameof(FindAndAttachCamera));
            return;
        }

        // Find camera based on scene
        if (currentSceneName == "LobbyScene")
        {
            FindAndAttachLobbyCamera();
        }
        else if (currentSceneName == "GameScene")
        {
            FindAndAttachGameCamera();
        }
        else
        {
            // Fallback: try to find any camera
            virtualCamera = FindAnyObjectByType<CinemachineCamera>();
            if (virtualCamera != null)
            {
                AttachCamera();
            }
        }
    }

    private void FindAndAttachLobbyCamera()
    {
        // Find camera in lobby scene
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            // For lobby: Top-down view with high Y offset
            var composer = virtualCamera.GetComponent<CinemachinePositionComposer>();

            AttachCamera();
            Debug.Log($"Lobby camera attached to player at position {transform.position}");
        }
        else
        {
            Debug.Log("Waiting for Lobby camera to be found...");
        }
    }

    private void FindAndAttachGameCamera()
    {
        // Find camera in game scene
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            // For game: Third-person or top-down view
            var composer = virtualCamera.GetComponent<CinemachinePositionComposer>();
            AttachCamera();
            Debug.Log($"Game camera attached to player at position {transform.position}");
        }
        else
        {
            Debug.Log("Waiting for Game camera to be found...");
        }
    }

    private void AttachCamera()
    {
        // Ensure we ONLY do this for the player we actually own
        if (IsOwner && virtualCamera != null)
        {
            virtualCamera.Follow = transform; // 'transform' here is the Player's transform
            virtualCamera.LookAt = transform;
            isAttached = true;
            CancelInvoke(nameof(FindAndAttachCamera));
        }
    }

    private void LateUpdate()
    {
        // Check if scene changed during gameplay
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != currentSceneName)
        {
            Debug.Log($"Scene change detected from {currentSceneName} to {currentScene}");
            currentSceneName = currentScene;
            isAttached = false;
            virtualCamera = null;
            StartSearchingForCamera();
            return;
        }

        // Emergency re-attach if camera gets lost
        if (IsOwner && !isAttached && virtualCamera != null)
        {
            AttachCamera();
        }
    }

    public override void OnDestroy() {
        // 1. Let Programmer B's Network system do its secret cleanup first!
        base.OnDestroy();

        // 2. Do your camera cleanup
        CancelInvoke(nameof(FindAndAttachCamera));

        if (IsOwner && virtualCamera != null && virtualCamera.Follow == transform) {
            virtualCamera.Follow = null;
            virtualCamera.LookAt = null;
        }
    }
}