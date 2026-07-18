using UnityEngine;

public class SwarmZombieVisual : MonoBehaviour
{
    public MeshFilter meshFilter;
    public Mesh[] walkFrames;
    public Mesh[] attackFrames;

    public void SetWalkFrame(int frame)
    {
        if (walkFrames == null || walkFrames.Length == 0 || meshFilter == null) return;
        meshFilter.sharedMesh = walkFrames[frame % walkFrames.Length];
    }

    public void SetAttackFrame(int frame)
    {
        if (attackFrames == null || attackFrames.Length == 0 || meshFilter == null) return;
        meshFilter.sharedMesh = attackFrames[frame % attackFrames.Length];
    }
}