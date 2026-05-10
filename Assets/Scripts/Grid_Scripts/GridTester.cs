using UnityEngine;
using UnityEngine.InputSystem;

public class GridTester : MonoBehaviour
{
    public GridPrefabSpawner spawner;
    public GridPattern patternA;
    public GridPattern patternB;

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            StartCoroutine(spawner.TransitionToPattern(patternA));

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            StartCoroutine(spawner.TransitionToPattern(patternB));
    }
}