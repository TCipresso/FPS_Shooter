using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class MenuUIHelper : MonoBehaviour
{
    public static MenuUIHelper Instance { get; private set; }

    [Header("References")]
    public GameObject pauseMenuUI;
    public InputActionReference pauseAction;
    public FPSLook fpsLook;

    [Header("Loot Chest")]
    [Tooltip("Pre-placed loot chest popup panel (disabled by default).")]
    public GameObject lootChestUI;

    bool isPaused = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
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

    System.Action onLootChestClosed;

    // Called by LootChestBuy on purchase with the rolled attachment. onClosed runs when the
    // player closes the panel (LootChestBuy passes a callback that destroys the chest).
    public void OpenLootChest(WeaponAttachmentSO reward, System.Action onClosed = null)
    {
        onLootChestClosed = onClosed;
        Time.timeScale = 0f;
        EnterDraftState();

        if (lootChestUI == null) return;

        ChestUIDraft draft = lootChestUI.GetComponent<ChestUIDraft>();
        if (draft != null)
            draft.Present(reward);
        else
            lootChestUI.SetActive(true);
    }

    // Hook this to the loot chest panel's close / continue button.
    public void CloseLootChest()
    {
        if (lootChestUI != null) lootChestUI.SetActive(false);
        Time.timeScale = 1f;
        ExitDraftState();

        System.Action cb = onLootChestClosed;
        onLootChestClosed = null;
        cb?.Invoke();
    }
    public void ExitDraftState()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (fpsLook != null) fpsLook.enabled = true;
    }
    public void RestartGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[MenuUIHelper] Restarting game.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[MenuUIHelper] Quitting game.");
        Application.Quit();
    }
}