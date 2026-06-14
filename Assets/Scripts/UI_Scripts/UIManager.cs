using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject itemPickupUI;

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

    public void OpenItemPickupUI()
    {
        if (itemPickupUI != null)
            itemPickupUI.SetActive(true);

        GameStateManager.Instance?.PauseGame();
    }

    public void CloseItemPickupUI()
    {
        if (itemPickupUI != null)
            itemPickupUI.SetActive(false);

        GameStateManager.Instance?.ResumeGame();
    }
}