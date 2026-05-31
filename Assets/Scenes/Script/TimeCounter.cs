using UnityEngine;
using TMPro;

public class TimeCounter : MonoBehaviour
{
    public float timeValue = 9999f;
    private TextMeshProUGUI textMesh;

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (timeValue > 0)
        {
            timeValue -= Time.deltaTime;
            textMesh.text = "Time: " + Mathf.FloorToInt(timeValue);
        }
    }
}