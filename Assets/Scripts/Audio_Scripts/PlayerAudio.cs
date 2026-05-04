using UnityEngine;
using System.Collections.Generic;

public class PlayerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip[] walkClips;
    public AudioClip[] sprintClips;

    [Header("Volume")]
    public float walkVolume = 0.6f;
    public float sprintVolume = 0.85f;

    Queue<AudioClip> _walkBag = new Queue<AudioClip>();
    Queue<AudioClip> _sprintBag = new Queue<AudioClip>();

    PlayerFpsController fpsController;

    void Awake()
    {
        fpsController = FindFirstObjectByType<PlayerFpsController>();
    }

    public void PlayWalkFootstep()
    {
        if (!IsGrounded()) return;
        if (footstepSource == null || walkClips == null || walkClips.Length == 0) return;
        if (_walkBag.Count == 0) Refill(walkClips, _walkBag);
        footstepSource.PlayOneShot(_walkBag.Dequeue(), walkVolume);
    }

    public void PlaySprintFootstep()
    {
        if (!IsGrounded()) return;
        if (footstepSource == null || sprintClips == null || sprintClips.Length == 0) return;
        if (_sprintBag.Count == 0) Refill(sprintClips, _sprintBag);
        footstepSource.PlayOneShot(_sprintBag.Dequeue(), sprintVolume);
    }

    bool IsGrounded()
    {
        if (fpsController == null) return true;
        return fpsController.IsGrounded;
    }

    void Refill(AudioClip[] clips, Queue<AudioClip> bag)
    {
        List<AudioClip> pool = new List<AudioClip>(clips);
        while (pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            bag.Enqueue(pool[i]);
            pool.RemoveAt(i);
        }
    }
}