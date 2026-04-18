using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuUIHelper : MonoBehaviour
{
    [Header("References")]
    public GameObject pauseMenuUI;
    public InputActionReference pauseAction;
    public FPSLook fpsLook;
    bool isPaused = false;

    void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.action.Enable();
    }

    void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.action.Disable();
    }

    void Update()
    {
        if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
            TogglePause();
    }

    void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (fpsLook != null) fpsLook.enabled = true;
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (fpsLook != null) fpsLook.enabled = false;
        isPaused = true;
    }

    public void EnterDraftState()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (fpsLook != null) fpsLook.enabled = false;
    }

    public void ExitDraftState()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (fpsLook != null) fpsLook.enabled = true;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[MenuUIHelper] Quitting game.");
        Application.Quit();
    }
}