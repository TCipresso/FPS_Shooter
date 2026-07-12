using UnityEngine;

public class SteppedAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] int targetFrameRate = 12;

    float accumulated;
    float stepInterval;

    void Awake()
    {
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        stepInterval = 1f / targetFrameRate;
    }

    void Update()
    {
        accumulated += Time.deltaTime;

        if (accumulated >= stepInterval)
        {
            animator.Update(accumulated);
            accumulated = 0f;
        }
        else
        {
            animator.Update(0f);
        }
    }
}