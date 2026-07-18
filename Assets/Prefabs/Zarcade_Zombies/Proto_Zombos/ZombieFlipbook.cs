using UnityEngine;

public class ZombieFlipbook : MonoBehaviour
{
    public enum Anim { Idle, Walk, Attack }

    [Header("References")]
    public MeshFilter meshFilter;
    public ZombieBase zombie;
    public Transform headCollider;

    [Header("Idle")]
    public Mesh[] idleFrames;
    public float idleFps = 6f;
    public Vector3[] idleHeadOffsets;

    [Header("Walk")]
    public Mesh[] walkFrames;
    public float walkFps = 10f;
    public Vector3[] walkHeadOffsets;

    [Header("Attack")]
    public Mesh[] attackFrames;
    public float attackFps = 12f;
    public Vector3[] attackHeadOffsets;
    public int attackHitFrame = 3;

    Anim currentAnim = Anim.Idle;
    Mesh[] currentFrames;
    Vector3[] currentHeadOffsets;
    float currentFps;
    int frameIndex;
    float frameTimer;
    bool attackHitFired;

    void Awake()
    {
        if (meshFilter == null)
            meshFilter = GetComponentInChildren<MeshFilter>();
        if (zombie == null)
            zombie = GetComponentInParent<ZombieBase>();

        frameTimer = Random.value;
        Play(Anim.Idle);
    }

    void Update()
    {
        if (currentFrames == null || currentFrames.Length == 0) return;

        frameTimer += Time.deltaTime * currentFps;
        if (frameTimer < 1f) return;

        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            AdvanceFrame();
        }
    }

    void AdvanceFrame()
    {
        frameIndex++;

        if (currentAnim == Anim.Attack)
        {
            if (frameIndex >= attackHitFrame && !attackHitFired)
            {
                attackHitFired = true;
                zombie?.OnHitFrame();
            }

            if (frameIndex >= currentFrames.Length)
            {
                zombie?.OnAttackComplete();
                return;
            }
        }
        else
        {
            if (frameIndex >= currentFrames.Length)
                frameIndex = 0;
        }

        ApplyFrame();
    }

    void ApplyFrame()
    {
        meshFilter.sharedMesh = currentFrames[frameIndex];

        if (headCollider != null && currentHeadOffsets != null && currentHeadOffsets.Length == currentFrames.Length)
            headCollider.localPosition = currentHeadOffsets[frameIndex];
    }

    public void Play(Anim anim)
    {
        Mesh[] frames;
        Vector3[] offsets;
        float fps;

        switch (anim)
        {
            case Anim.Walk:
                frames = walkFrames;
                offsets = walkHeadOffsets;
                fps = walkFps;
                break;
            case Anim.Attack:
                frames = attackFrames;
                offsets = attackHeadOffsets;
                fps = attackFps;
                break;
            default:
                frames = idleFrames;
                offsets = idleHeadOffsets;
                fps = idleFps;
                break;
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Flipbook has no frames assigned for {anim}.");
            return;
        }

        currentAnim = anim;
        currentFrames = frames;
        currentHeadOffsets = offsets;
        currentFps = fps;
        frameIndex = 0;
        attackHitFired = false;

        ApplyFrame();
    }

    public void SetWalking(bool walking)
    {
        if (currentAnim == Anim.Attack) return;

        Anim target = walking ? Anim.Walk : Anim.Idle;
        if (currentAnim != target)
            Play(target);
    }

    public void TriggerAttack()
    {
        Play(Anim.Attack);
    }

    public void ForceIdle()
    {
        Play(Anim.Idle);
    }
}