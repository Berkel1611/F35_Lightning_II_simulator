using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// System odpalania rakiet AIM-120 / AIM-9X.
/// Obs³uguje lock-in celu i integracjê z WeaponSystem.
/// </summary>
public class MissileSystem : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    WeaponSystem weaponSystem;
    [SerializeField]
    Plane plane;
    [SerializeField]
    TargetingSystem targetingSystem;

    [Header("Prefaby rakiet")]
    [SerializeField]
    GameObject aim120Prefab;
    [SerializeField]
    GameObject aim9xPrefab;

    [Header("Audio")]
    [SerializeField] 
    AudioSource audioSource;
    [SerializeField] 
    AudioClip launchSound;

    [Header("Lock-on")]
    [SerializeField]
    float lockOnFov = 30f;
    [SerializeField]
    float lockOnRange = 10000f;
    [SerializeField]
    float lockOnTime = 2f;

    // Lock-on
    Transform lockedTarget;
    Transform trackingTarget;
    float lockTimer = 0f;
    bool isLocked = false;

    // Wystrzelone rakiety
    List<Missile> firedMissiles = new List<Missile>();

    // W³aœciwoœci publiczne dla HUD/PCD
    public bool IsLocked => isLocked;
    public Transform LockedTarget => lockedTarget;
    public float LockProgress => Mathf.Clamp01(lockTimer / lockOnTime);
    public Transform TrackingTarget => trackingTarget;
    public bool IsTracking => trackingTarget != null;
    public Vector3 TrackingDirection => trackingTarget != null 
        ? (trackingTarget.position - transform.position).normalized 
        : transform.forward;
    public IReadOnlyList<Missile> FiredMissiles => firedMissiles;

    // Input
    bool fireHeld = false;

    private void Awake()
    {
        if (weaponSystem == null) weaponSystem = GetComponent<WeaponSystem>();
        if (plane == null) plane = GetComponent<Plane>();
    }

    private void Update()
    {
        UpdateLockOn();
        if (!fireHeld) return;

        var active = weaponSystem.ActiveWeapon;
        if (active != WeaponSystem.WeaponType.AIM120 &&
            active != WeaponSystem.WeaponType.AIM9X)
            return;

        if (!weaponSystem.CanFire) return;
        if (!isLocked) return;

        LaunchMissile();
    }

    void UpdateLockOn()
    {
        var active = weaponSystem.ActiveWeapon;
        if (active != WeaponSystem.WeaponType.AIM120 &&
            active != WeaponSystem.WeaponType.AIM9X)
        {
            ResetLock();
            return;
        }

        Transform candidate = CanLockOn(targetingSystem?.SelectedTarget);

        if (candidate == null)
        {
            ResetLock();
            return;
        }

        if (candidate != trackingTarget)
        {
            trackingTarget = candidate;
            lockTimer = 0f;
            isLocked = false;
        }

        lockTimer += Time.deltaTime;

        if (lockTimer >= lockOnTime)
        {
            lockedTarget = trackingTarget;
            isLocked = true;
        }
    }

    Transform CanLockOn(AITarget target)
    {
        if (target == null || !target.IsAlive) return null;

        Vector3 toTarget = target.Position - transform.position;
        float dist = toTarget.magnitude;
        if (dist > lockOnRange) return null;

        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > lockOnFov * 0.5f) return null;

        return target.transform;
    }

    void ResetLock()
    {
        isLocked = false;
        lockedTarget = null;
        trackingTarget = null;
        lockTimer = 0f;
    }

    void LaunchMissile()
    {
        Transform pylonTransform = weaponSystem.RegisterMissileLaunch();
        if (pylonTransform == null) return;

        bool isAIM120 = weaponSystem.ActiveWeapon == WeaponSystem.WeaponType.AIM120;
        GameObject prefab = isAIM120 ? aim120Prefab : aim9xPrefab;
        if (prefab == null) return;

        GameObject missileGO = Instantiate(prefab, pylonTransform.position, transform.rotation);
        var missile = missileGO.GetComponent<Missile>();
        missile.OnDetonated += () => firedMissiles.Remove(missile);
        firedMissiles.Add(missile);
        missile.Launch(plane.Velocity, lockedTarget);

        if (audioSource != null && launchSound != null)
            audioSource.PlayOneShot(launchSound);

        ResetLock();
    }

    // API dla PlayerController

    /// <summary>Wywo³aj gdy gracz naciœnie klawisz odpalenia rakiety.</summary>
    public void SetFireInput(bool held)
    {
        fireHeld = held;
    }
}
