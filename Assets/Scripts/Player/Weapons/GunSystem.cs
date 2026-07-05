using UnityEngine;

/// <summary>
/// Mechanika dzia³ka GAU-22/A 25mm.
/// Obs³uguje pooling pocisków, dŸwiêk i efekt wylotu.
/// </summary>
public class GunSystem : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    WeaponSystem weaponSystem;
    [SerializeField]
    Transform gunMuzzle;

    [Header("Pocisk")]
    [SerializeField]
    GameObject bulletPrefab;
    [SerializeField]
    float muzzleVelocity = 1000f;
    [SerializeField]
    float bulletLifetime = 3f;
    [SerializeField]
    int poolSize = 60;

    [Header("Efekty")]
    [SerializeField]
    ParticleSystem muzzleFlash;
    [SerializeField]
    AudioSource gunAudioSource;
    [SerializeField]
    AudioClip fireSound;

    // Pooling pocisków
    GameObject[] bulletPool;
    int poolIndex = 0;

    // Input - trzymanie klawisza
    bool fireHeld = false;

    public float MuzzleVelocity => muzzleVelocity;

    private void Awake()
    {
        if (weaponSystem == null)
            weaponSystem = GetComponent<WeaponSystem>();

        InitPool();
    }

    void InitPool()
    {
        bulletPool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            bulletPool[i] = Instantiate(bulletPrefab);
            bulletPool[i].SetActive(false);

            if (bulletPool[i].GetComponent<Bullet>() == null)
                bulletPool[i].AddComponent<Bullet>();
        }
    }

    void Update()
    {
        if (!fireHeld) return;
        if (weaponSystem.ActiveWeapon != WeaponSystem.WeaponType.Gun) return;
        if (!weaponSystem.CanFire) return;

        Fire();
    }

    void Fire()
    {
        if (gunMuzzle == null)
        {
            Debug.LogWarning("GunSystem: brak referencji do gunMuzzle!");
            return;
        }

        // Pobierz pocisk z poola
        GameObject bullet = GetPooledBullet();
        if (bullet == null) return;

        // Ustaw pozycjê i rotacjê na wylot lufy
        bullet.transform.SetPositionAndRotation(gunMuzzle.position, gunMuzzle.rotation);
        bullet.SetActive(true);

        // Nadaj prêdkoœæ
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            Vector3 planeVelocity = GetComponent<Plane>() != null
                ? GetComponent<Plane>().Velocity 
                : Vector3.zero;
            rb.linearVelocity = planeVelocity + gunMuzzle.forward * muzzleVelocity;
        }

        // Zaplanuj zwrot do poola
        bullet.GetComponent<Bullet>().ScheduleReturn(bulletLifetime);

        // Efekty
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }
        if (gunAudioSource != null && fireSound != null)
            gunAudioSource.PlayOneShot(fireSound, 0.7f);

        // Poinformuj WeaponSystem
        weaponSystem.RegisterGunShot();
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

    // API dla PlayerController

    ///<summary>Wywo³aj gdy gracz naciœnie klawisz ognia.</summary>
    public void SetFireInput(bool held)
    {
        fireHeld = held;
    }
}
