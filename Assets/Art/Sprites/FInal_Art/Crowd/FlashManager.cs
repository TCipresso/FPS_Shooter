using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashManager : MonoBehaviour
{
    public List<GameObject> objectsToFlash;
    public float interval = 1.0f;

    private Coroutine flashRoutine; 

    void Start()
    {
        if (objectsToFlash.Count > 0)
        {
            flashRoutine = StartCoroutine(FlashObjects());
        }
    }

    private IEnumerator FlashObjects()
    {
        while (true)
        {
            foreach (GameObject obj in objectsToFlash)
            {
                if (obj != null)
                {
                    obj.SetActive(Random.value > 0.5f); 
                }
            }
            yield return new WaitForSeconds(interval);
        }
    }

    void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
    }
}
