using UnityEngine;

public class DoorBuy : Buyable
{
    [Header("Door Settings")]
    public float openYOffset = -20f;
    public float openSpeed = 5f;

    bool isOpen = false;
    Vector3 closedPosition;
    Vector3 openPosition;

    void Start()
    {
        closedPosition = transform.position;
        openPosition = new Vector3(transform.position.x, transform.position.y + openYOffset, transform.position.z);
    }

    void Update()
    {
        if (isOpen)
            transform.position = Vector3.Lerp(transform.position, openPosition, openSpeed * Time.deltaTime);
    }

    new public string interactPrompt =>
        $"Open Door - {cost} Points";

    protected override void OnPurchase(PlayerStats stats)
    {
        isOpen = true;
        Debug.Log("[DoorBuy] Door opened.");
    }
}