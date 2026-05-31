using UnityEngine;
using Unity.Netcode;

public abstract class BaseInteractable : NetworkBehaviour
{
    [Header("Visual Feedback")]
    [Tooltip("Drag the child 'Selected' object here")]
    [SerializeField] protected GameObject selectionHighlight;

    public virtual void Start()
    {
        SetHighlight(false);
    }

    public abstract void OnInteract(PlayerMovement interactingPlayer);

    public virtual void SetHighlight(bool active)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(active);
        }
    }
}