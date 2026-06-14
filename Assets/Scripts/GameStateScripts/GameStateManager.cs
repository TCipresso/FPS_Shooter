using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public bool IsPaused { get; private set; }

    [Header("References")]
    public FPSLook fpsLook;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (fpsLook != null) fpsLook.enabled = false;
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (fpsLook != null) fpsLook.enabled = true;
    }
}