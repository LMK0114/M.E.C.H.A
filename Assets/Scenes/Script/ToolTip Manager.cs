using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    public TextMeshProUGUI tooltipText;

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        gameObject.SetActive(false);

        // Force correct settings for free movement
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 1);        // Top-Left
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            FollowMouse();
        }
    }

    private void FollowMouse()
    {
        if (canvas == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            mousePos,
            canvas.worldCamera,
            out Vector2 localPoint);

        // Top-right from cursor (adjust numbers if needed)
        rectTransform.anchoredPosition = localPoint + new Vector2(960, 670); // here x, y
    }

    public void SetAndShowToolTip(string message)
    {
        tooltipText.text = message;
        gameObject.SetActive(true);
    }

    public void HideToolTip()
    {
        gameObject.SetActive(false);
    }
}