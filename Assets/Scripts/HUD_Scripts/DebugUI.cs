using UnityEngine;
using TMPro;

public class DebugUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI fpsText;

    [Header("Settings")]
    public float updateInterval = 0.5f;

    float timer;
    int frameCount;
    float fps;

    void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            fps = frameCount / timer;
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            frameCount = 0;
            timer = 0f;
        }
    }
}