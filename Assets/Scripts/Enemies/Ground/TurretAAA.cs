using UnityEngine;

/// <summary>
/// Wie¿yczka przeciwlotnicza ZU-23-2.
/// Obraca siê w kierunku gracza i strzela naprzemiennie z dwóch luf.
/// </summary>
public class TurretAAA : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    Transform turretBase;
    [SerializeField]
    Transform barrel;
    [SerializeField]
    Transform muzzleL;
    [SerializeField]
    Transform muzzleR;

    [Header("Detekcja")]
    [SerializeField]
    float detectionRange = 3000f;
    [SerializeField]
    float detectionFov = 360f;

    [Header("Obrót")]
    [SerializeField]
    float yawSpeed = 60f;
    [SerializeField]
    float pitchSpeed = 45f;
    [SerializeField]
    float minPitch = 0f;
    [SerializeField]
    float maxPitch = 85f;

    [Header("Strzelanie")]
    [SerializeField]
    float fireRate = 0.1f;
    [SerializeField]
    float aimThreshold = 3f;
    [SerializeField]
    float bulletSpeed = 800f;
    [SerializeField]
    float bulletLifetime = 3f;
    [SerializeField]
    int poolSize = 60;
    [SerializeField]
    GameObject bulletPrefab;

    [Header("Efekty")]
    [SerializeField]
    ParticleSystem muzzleFlashL;
    [SerializeField]
    ParticleSystem muzzleFlashR;

    [Header("Audio")]
    [SerializeField] 
    AudioSource audioSource;
    [SerializeField] 
    AudioClip fireSound;

    // Stan wewnêtrzny
    Transform target;
    float fireCooldown;
    bool fireFromLeft = true;

    // Pooling
    GameObject[] bulletPool;
    int poolIndex;

    private void Awake()
    {
        InitPool();
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void Update()
    {
        if (target == null) return;

        if (!IsTargetInRange()) return;

        AimAtTarget();

        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f && IsAimed())
        {
            Fire();
            fireCooldown = fireRate;
        }
    }

    bool IsTargetInRange()
    {
        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        if (dist > detectionRange) return false;

        float angle = Vector3.Angle(turretBase.forward, toTarget);
        if (angle > detectionFov * 0.5f) return false;

        return true;
    }

    void AimAtTarget()
    {
        Vector3 toTarget = target.position - barrel.position;

        // Yaw
        Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(flatDir);
            turretBase.rotation = Quaternion.RotateTowards(
                turretBase.rotation, 
                targetYaw,
                yawSpeed * Time.deltaTime
            );
        }

        // Pitch
        Vector3 localDir = turretBase.InverseTransformDirection(toTarget).normalized;
        float targetPitch = Mathf.Clamp(
            -Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg, 
            -maxPitch,
            -minPitch
        );

        float currentPitch = barrel.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;

        float newPitch = Mathf.MoveTowards(
            currentPitch,
            targetPitch,
            pitchSpeed * Time.deltaTime
        );
        barrel.localEulerAngles = new Vector3(newPitch, 0f, 0f);
    }

    bool IsAimed()
    {
        Vector3 toTarget = (target.position - turretBase.position).normalized;
        float angle = Vector3.Angle(barrel.forward, toTarget);
        return angle < aimThreshold;
    }

    void Fire()
    {
        Transform muzzle = fireFromLeft ? muzzleL : muzzleR;
        ParticleSystem flash = fireFromLeft ? muzzleFlashL : muzzleFlashR;
        fireFromLeft = !fireFromLeft;

        GameObject bullet = GetPooledBullet();
        if (bullet == null) return;

        bullet.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
        bullet.SetActive(true);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = muzzle.forward * bulletSpeed;

        bullet.GetComponent<Bullet>().ScheduleReturn(bulletLifetime);

        if (flash != null)
        {
            flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flash.Play();
        }

        if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound, 0.7f);
    }

    void InitPool()
    {
        if (bulletPrefab == null) return;

        bulletPool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            bulletPool[i] = Instantiate(bulletPrefab);
            bulletPool[i].SetActive(false);
        }
    }

    GameObject GetPooledBullet()
    {
        for (int i = 0; i < poolSize; i++)
        {
            int idx = (poolIndex + i) % poolSize;
            if (!bulletPool[idx].activeSelf)
            {
                poolIndex = (idx + 1) % poolSize;
                return bulletPool[idx];
            }
        }
        return null;
    }
}
