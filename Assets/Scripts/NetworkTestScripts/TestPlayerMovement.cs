using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public Camera playerCamera;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (!isLocalPlayer)
            playerCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        Vector2 input = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
        }

        Vector3 move = new Vector3(input.x, 0, input.y) * moveSpeed * Time.deltaTime;
        transform.Translate(move);
    }

    public override void OnStartLocalPlayer()
    {
        GetComponent<Renderer>().material.color = Color.green;
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(true);
    }
}