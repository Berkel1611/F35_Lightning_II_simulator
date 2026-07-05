using UnityEngine;
using System.Collections;

/// <summary>
/// Wyrzutnia rakiet SAM.
/// Wykrywa gracza, lock-on, odpala rakiety naprzemiennie z dwóch punktów.
/// Modele rakiet jako dzieci LaunchPoint - dezaktywuj¹ siê po odpaleniu.
/// Kolejna rakieta odpala siê dopiero, gdy poprzednia eksploduje.
/// </summary>
public class SAMLauncher : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    Transform launcherBase;
    [SerializeField]
    Transform launchPointL;
    [SerializeField]
    Transform launchPointR;

    [Header("Modele rakiet")]
    [SerializeField]
    GameObject missileModelL;
    [SerializeField]
    GameObject missileModelR;

    [Header("Detekcja")]
    [SerializeField]
    float detectionRange = 1000f;

    [Header("Obrót")]
    [SerializeField]
    float yawSpeed = 10f;

    [Header("Lock-on")]
    [SerializeField]
    float lockOnTime = 3f;

    [Header("Strzelanie")]
    [SerializeField]
    GameObject missilePrefab;
    [SerializeField]
    float reloadTime = 15f;

    [Header("Audio")]
    [SerializeField]
    AudioSource audioSource;
    [SerializeField]
    AudioClip launchSound;

    // Stan wewnêtrzny
    Transform target;
    float lockTimer = 0f;
    bool isLocked = false;
    bool fireFromLeft = true;
    bool waitingForDetonation = false;
    bool missilesLeftL = true;
    bool missilesLeftR = true;

    public bool IsLocked => isLocked;
    public float LockProgress => Mathf.Clamp01(lockTimer / lockOnTime);

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void Update()
    {
        if (target == null) return;
        if (waitingForDetonation) return;
        if (!HasMissiles()) return;

        if (!IsTargetInRange())
        {
            ResetLock();
            return;
        }

        RotateToTarget();
        UpdateLockOn();

        if (isLocked)
            LaunchMissile();
    }

    bool IsTargetInRange()
    {
        return Vector3.Distance(transform.position, target.position) <= detectionRange;
    }

    void RotateToTarget()
    {
        Vector3 toTarget = target.position - launcherBase.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;

        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(flatDir);
            launcherBase.rotation = Quaternion.RotateTowards(
                launcherBase.rotation,
                targetYaw,
                yawSpeed * Time.deltaTime
            );
        }
    }

    void UpdateLockOn()
    {
        lockTimer += Time.deltaTime;
        if (lockTimer >= lockOnTime)
            isLocked = true;
    }

    void ResetLock()
    {
        lockTimer = 0f;
        isLocked = false;
    }

    bool HasMissiles()
    {
        return missilesLeftL || missilesLeftR;
    }

    void LaunchMissile()
    {
        // Prze³¹cz na dostêpn¹ stronê
        if (fireFromLeft && !missilesLeftL)
            fireFromLeft = false;
        if (!fireFromLeft && !missilesLeftR)
            fireFromLeft = true;

        Transform launchPoint = fireFromLeft ? launchPointL : launchPointR;
        GameObject missileModel = fireFromLeft ? missileModelL : missileModelR;

        bool launchedFromLeft = fireFromLeft;

        if (launchedFromLeft)
        {
            missilesLeftL = false;
            StartCoroutine(ReloadMissile(true));
        }
        else
        {
            missilesLeftR = false;
            StartCoroutine(ReloadMissile(false));
        }

        fireFromLeft = !fireFromLeft;

        // Ukryj model rakiety na wyrzutni
        if (missileModel != null)
            missileModel.SetActive(false);

        ResetLock();
        waitingForDetonation = true;

        if (missilePrefab == null) return;

        GameObject missileGO = Instantiate(missilePrefab, launchPoint.position, launchPoint.rotation);
        Missile missile = missileGO.GetComponent<Missile>();
        if (missile != null)
        {
            missile.Launch(Vector3.zero, target);
            missile.OnDetonated += OnMissileDetonated;
        }

        if (audioSource != null && launchSound != null)
            audioSource.PlayOneShot(launchSound);
    }

    void OnMissileDetonated()
    {
        waitingForDetonation = false;
        ResetLock();
    }

    IEnumerator ReloadMissile(bool left)
    {
        yield return new WaitForSeconds(reloadTime);

        if (left)
        {
            missilesLeftL = true;

            if (missileModelL != null)
                missileModelL.SetActive(true);
        }
        else
        {
            missilesLeftR = true;

            if (missileModelR != null)
                missileModelR.SetActive(true);
        }
    }
}
