using System.Collections;
using UnityEngine;

/// <summary>
/// System zrzutu bomb GBU-31 JDAM.
/// Obs³uguje animacjê klap komór wewnêtrznych i integracjê z WeaponSystem.
/// </summary>
public class BombSystem : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    WeaponSystem weaponSystem;
    [SerializeField]
    Plane plane;

    [Header("Prefab bomby")]
    [SerializeField]
    GameObject bombPrefab;

    [Header("Lewa komora - klapy")]
    [SerializeField]
    Transform leftBay_LeftDoor;
    [SerializeField]
    Transform leftBay_RightDoor;

    [Header("Prawa komora - klapy")]
    [SerializeField]
    Transform rightBay_LeftDoor;
    [SerializeField]
    Transform rightBay_RightDoor;

    [Header("Parametry klapy")]
    [SerializeField]
    float doorOpenAngle = 90f;
    [SerializeField]
    float doorAnimTime = 0.8f;
    [SerializeField]
    float dropDelay = 0.3f;

    [Header("Audio")]
    [SerializeField] 
    AudioSource audioSource;
    [SerializeField] 
    AudioClip doorOpeningSound;
    [SerializeField] 
    AudioClip bombReleaseSound;

    // Rotacje pocz¹tkowe
    Quaternion leftBay_LeftDoor_InitRot;
    Quaternion leftBay_RightDoor_InitRot;
    Quaternion rightBay_LeftDoor_InitRot;
    Quaternion rightBay_RightDoor_InitRot;

    bool dropFromLeft = true;
    bool isDropping = false;
    bool fireHeld = false;

    private void Awake()
    {
        if (weaponSystem == null) weaponSystem = GetComponent<WeaponSystem>();
        if (plane == null) plane = GetComponentInParent<Plane>();

        // Zapamiêtaj rotacje pocz¹tkowe klap
        leftBay_LeftDoor_InitRot = leftBay_LeftDoor.localRotation;
        leftBay_RightDoor_InitRot = leftBay_RightDoor.localRotation;
        rightBay_LeftDoor_InitRot = rightBay_LeftDoor.localRotation;
        rightBay_RightDoor_InitRot = rightBay_RightDoor.localRotation;
    }

    private void Update()
    {
        if (!fireHeld) return;
        if (weaponSystem.ActiveWeapon != WeaponSystem.WeaponType.GBU31) return;
        if (!weaponSystem.CanFire) return;
        if (isDropping) return;

        StartCoroutine(DropSequence());
    }

    IEnumerator DropSequence()
    {
        isDropping = true;

        // Wybierz aktywn¹ komorê
        Transform doorL = dropFromLeft ? leftBay_LeftDoor : rightBay_LeftDoor;
        Transform doorR = dropFromLeft ? leftBay_RightDoor : rightBay_RightDoor;
        Quaternion initL = dropFromLeft ? leftBay_LeftDoor_InitRot : rightBay_LeftDoor_InitRot;
        Quaternion initR = dropFromLeft ? leftBay_RightDoor_InitRot : rightBay_RightDoor_InitRot;

        // Docelowe rotacje klap
        Quaternion openL = initL * Quaternion.Euler(0f, doorOpenAngle, 0f);
        Quaternion openR = initR * Quaternion.Euler(0f, -doorOpenAngle, 0f);

        // 1. Otwieranie klap
        if (audioSource != null && doorOpeningSound != null)
        {
            audioSource.clip = doorOpeningSound;
            audioSource.Play();
        }
        yield return StartCoroutine(AnimateDoors(doorL, doorR, initL, initR, openL, openR, doorAnimTime));
        audioSource.Stop();

        // 2. Odczekaj chwilê przed zrzutem
        yield return new WaitForSeconds(dropDelay);

        // 3. Zrzut bomby
        if (audioSource != null && bombReleaseSound != null)
            audioSource.PlayOneShot(bombReleaseSound);

        DropBomb();

        // 4. Zamykanie klap
        if (audioSource != null && doorOpeningSound != null)
        {
            audioSource.clip = doorOpeningSound;
            audioSource.Play();
        }
        yield return StartCoroutine(AnimateDoors(doorL, doorR, openL, openR, initL, initR, doorAnimTime));
        audioSource.Stop();

        // Zmieñ komorê na nastêpn¹
        dropFromLeft = !dropFromLeft;

        isDropping = false;
    }

    IEnumerator AnimateDoors(
        Transform doorL, Transform doorR,
        Quaternion fromL, Quaternion fromR,
        Quaternion toL, Quaternion toR,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (doorL != null)
                doorL.localRotation = Quaternion.Slerp(fromL, toL, smooth);
            if (doorR != null)
                doorR.localRotation = Quaternion.Slerp(fromR, toR, smooth);

            yield return null;
        }

        // Ustaw dok³adn¹ koñcow¹ rotacjê
        if (doorL != null)
            doorL.localRotation = toL;
        if (doorR != null)
            doorR.localRotation = toR;
    }

    void DropBomb()
    {
        Transform modelTransform = weaponSystem.RegisterBombDrop();
        if (modelTransform == null) return;

        if (bombPrefab == null)
        {
            Debug.LogWarning("BombSystem: brak przypisanego bombPrefab!");
            return;
        }

        // Spawn bomby
        GameObject bombGO = Instantiate(bombPrefab, modelTransform.position, modelTransform.rotation);
        
        Bomb bomb = bombGO.GetComponent<Bomb>();
        if (bomb != null)
            bomb.Launch(plane.Velocity);
    }

    // API dla PlayerController

    /// <summary>Wywo³aj gdy grasz naciœnie klawisz zrzutu.</summary>
    public void SetFireInput(bool held)
    {
        fireHeld = held;
    }
}
