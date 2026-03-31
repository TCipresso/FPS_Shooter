using UnityEngine;
using System.Collections.Generic;

public class Sword : WeaponBase
{
    [Header("Sword Settings")]
    public int attackDamage = 150;
    public float attackRadius = 2f;
    [Range(0f, 1f)]
    public float hitAngle = 0.3f;

    [Header("Debug")]
    public bool showDebugSphere = true;

    float nextSwingTime = 0f;
    HashSet<ZombieBase> hitThisSwing = new HashSet<ZombieBase>();

    protected override void Awake()
    {
        base.Awake();
        isAutomatic = true;

        // Force infinite ammo regardless of inspector values
        currentMag = int.MaxValue;
        maxMag = int.MaxValue;
        reserveAmmo = int.MaxValue;
        maxReserve = int.MaxValue;
    }

    void OnValidate()
    {
        maxMag = int.MaxValue;
        maxReserve = int.MaxValue;
        currentMag = int.MaxValue;
        reserveAmmo = int.MaxValue;
    }

    public override void Shoot()
    {
        if (!CanShoot()) return;
        if (Time.time < nextSwingTime) return;

        nextSwingTime = Time.time + FireInterval;
        hitThisSwing.Clear();
        TriggerCockAnimation();
        ApplyRecoil();

        Debug.Log("[Sword] Swing!");
    }

    public void OnHitFrame()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (Collider hit in hits)
        {
            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(mainCamera.transform.forward, dirToTarget);

            if (dot < hitAngle) continue;

            ZombieBase zombie = hit.GetComponent<ZombieBase>();
            if (zombie != null && !hitThisSwing.Contains(zombie))
            {
                hitThisSwing.Add(zombie);
                zombie.TakeDamage(attackDamage, playerStats, goldMultiplier);
                Debug.Log("[Sword] Hit: " + hit.gameObject.name);
            }
        }
    }

    public override void Reload() { }

    public new bool CanShoot()
    {
        if (isCocking) return false;
        if (fpsController != null && fpsController.IsSprinting) return false;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugSphere) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, attackRadius);
    }
}