using UnityEngine;
using UnityEngine.InputSystem;

public class GridTester : MonoBehaviour
{
    public GridManager gridManager;
    public GridPattern patternA;
    public GridPattern patternB;

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            gridManager.ApplyPattern(patternA);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            gridManager.ApplyPattern(patternB);
    }
}