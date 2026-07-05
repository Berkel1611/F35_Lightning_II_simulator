using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralny system uzbrojenia F-35A.
/// Przechowuje stan ca³ego loadoutu, obs³uguje prze³¹czanie broni i flagê Master Arm.
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    // Typy uzbrojenia
    public enum WeaponType
    {
        Gun,
        AIM120,
        AIM9X,
        GBU31
    }

    // Dane loadoutu
    [Header("Ammo")]
    [SerializeField]
    int gunRounds = 182;
    [SerializeField]
    int aim120Count = 2;
    [SerializeField]
    int aim9xCount = 2;
    [SerializeField]
    int gbu31Count = 2;

    [Header("Modele broni na samolocie")]
    [SerializeField] 
    List<GameObject> aim120Models = new List<GameObject>();
    [SerializeField] 
    List<GameObject> aim9xModels = new List<GameObject>();
    [SerializeField]
    List<GameObject> gbu31Models = new List<GameObject>();

    [Header("Stan systemu")]
    [SerializeField]
    bool masterArm = false;
    [SerializeField]
    WeaponType activeWeapon = WeaponType.Gun;

    // Cooldowny
    [Header("Cooldowns")]
    [SerializeField]
    float gunFireRate = 0.018f;
    [SerializeField]
    float missileLaunchCooldown = 1.5f;
    [SerializeField]
    float bombDropCooldown = 0.5f;

    // Stany wewnêtrzne
    float gunCooldownTimer = 0f;
    float missileCooldownTimer = 0f;
    float bombCooldownTimer = 0f;

    // Indeksy do prze³¹czania broni
    int nextAim120Index = 0;
    int nextAim9xIndex = 0;
    int nextGbu31Index = 0;

    // W³aœciwoœci publiczne do odczytu stanu
    public bool MasterArm => masterArm;
    public WeaponType ActiveWeapon => activeWeapon;

    public int GunRounds => gunRounds;
    public int AIM120Count => aim120Count;
    public int AIM9XCount => aim9xCount;
    public int GBU31Count => gbu31Count;

    /// <summary>True gdy aktywna broñ ma amunicjê.</summary>
    public bool HasAmmo
    {
        get
        {
            switch (activeWeapon)
            {
                case WeaponType.Gun:
                    return gunRounds > 0;
                case WeaponType.AIM120:
                    return aim120Count > 0;
                case WeaponType.AIM9X:
                    return aim9xCount > 0;
                case WeaponType.GBU31:
                    return gbu31Count > 0;
                default:
                    return false;
            }
        }
    }

    /// <summary>True gdy broñ mo¿e teraz strzelaæ.</summary>
    public bool CanFire
    {
        get
        {
            if (!masterArm) return false;
            if (!HasAmmo) return false;

            switch (activeWeapon)
            {
                case WeaponType.Gun:
                    return gunCooldownTimer <= 0f;
                case WeaponType.AIM120:
                case WeaponType.AIM9X:
                    return missileCooldownTimer <= 0f;
                case WeaponType.GBU31:
                    return bombCooldownTimer <= 0f;
                default:
                    return false;
            }
        }
    }

    public bool AIM120ModelActive(int index) => GetModelActive(aim120Models, index);
    public bool AIM9XModelActive(int index) => GetModelActive(aim9xModels, index);
    public bool GBU31ModelActive(int index) => GetModelActive(gbu31Models, index);

    // Unity lifecycle
    private void Update()
    {
        TickCooldowns(Time.deltaTime);
    }

    void TickCooldowns(float dt)
    {
        if (gunCooldownTimer > 0f)
            gunCooldownTimer = Mathf.Max(0f, gunCooldownTimer - dt);
        if (missileCooldownTimer > 0f)
            missileCooldownTimer = Mathf.Max(0f, missileCooldownTimer - dt);
        if (bombCooldownTimer > 0f)
            bombCooldownTimer = Mathf.Max(0f, bombCooldownTimer - dt);
    }

    // API dla systemów konkretnych broni

    /// <summary> Rejestruje strza³ z dzia³ka. Odejmuje nabój i resetuje cooldown. </summary>
    public void RegisterGunShot()
    {
        if (gunRounds <= 0) return;
        gunRounds--;
        gunCooldownTimer = gunFireRate;
    }

    /// <summary> Rejestruje odpalenie rakiety aktywnego typu. Zwraca Transform pylonu, z którego startuje rakieta. </summary>
    public Transform RegisterMissileLaunch()
    {
        Transform pylon = null;

        if (activeWeapon == WeaponType.AIM120 && aim120Count > 0)
        {
            pylon = ConsumeWeaponModel(aim120Models, ref nextAim120Index);
            aim120Count--;
            missileCooldownTimer = missileLaunchCooldown;
        }
        else if (activeWeapon == WeaponType.AIM9X && aim9xCount > 0)
        {
            pylon = ConsumeWeaponModel(aim9xModels, ref nextAim9xIndex);
            aim9xCount--;
            missileCooldownTimer = missileLaunchCooldown;
        }

        return pylon;
    }

    /// <summary> Rejestruje zrzut bomby. Zwraca Transform pylonu. </summary>
    public Transform RegisterBombDrop()
    {
        if (gbu31Count <= 0) return null;

        Transform pylon = ConsumeWeaponModel(gbu31Models, ref nextGbu31Index);
        gbu31Count--;
        bombCooldownTimer = bombDropCooldown;
        return pylon;
    }

    // Prze³¹czanie broni

    /// <summary> Cykliczne prze³¹czanie na nastêpn¹ broñ. </summary>
    public void CycleWeapon()
    {
        int count = System.Enum.GetValues(typeof(WeaponType)).Length;
        activeWeapon = (WeaponType)(((int)activeWeapon + 1) % count);
    }

    /// <summary> Bezpoœredni wybór broni. </summary>
    public void SelectWeapon(WeaponType type)
    {
        activeWeapon = type;
    }

    // Master Arm

    public void ToggleMasterArm()
    {
        masterArm = !masterArm;
    }

    public void SetMasterArm(bool value)
    {
        masterArm = value;
    }

    // Funkcje pomocnicze

    /// <summary> Deaktywuje model broni i zwraca jego Transform jako punkt startowy pocisku. </summary>
    Transform ConsumeWeaponModel(List<GameObject> models, ref int nextIndex)
    {
        if (models == null || models.Count == 0) return null;

        for (int i = 0; i < models.Count; i++)
        {
            int idx = (nextIndex + i) % models.Count;
            if (models[idx] != null && models[idx].activeSelf)
            {
                Transform t = models[idx].transform;
                models[idx].SetActive(false);
                nextIndex = (idx + 1) % models.Count;
                return t;
            }
        }
        return null;
    }

    public bool GetModelActive(List<GameObject> models, int index)
    {
        if (models == null || index >= models.Count) return false;
        return models[index] != null && models[index].activeSelf;
    }
}
