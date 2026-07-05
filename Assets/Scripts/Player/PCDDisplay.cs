using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PCDDisplay : MonoBehaviour
{
    [SerializeField] 
    Plane plane;
    [SerializeField] 
    PlaneAnimation planeAnimation;
    [SerializeField]
    WeaponSystem weaponSystem;

    [Header("Systems")]
    [SerializeField] 
    TextMeshProUGUI gearText;
    [SerializeField]
    TextMeshProUGUI airbrakeText;
    [SerializeField]
    TextMeshProUGUI engineText;
    [SerializeField] 
    Bar throttleBar;
    [SerializeField]
    TextMeshProUGUI gforceText;
    [SerializeField]
    TextMeshProUGUI aoaText;

    [Header("Weapons")]
    [SerializeField] 
    TextMeshProUGUI aim120Text;
    [SerializeField] 
    TextMeshProUGUI aim9xText;
    [SerializeField] 
    TextMeshProUGUI gbu31Text;
    [SerializeField] 
    TextMeshProUGUI gauText;

    [Header("Weapon Model Indicators")]
    [SerializeField] 
    Image indicator_AIM120_OL1;
    [SerializeField] 
    Image indicator_AIM120_OL2;
    [SerializeField] 
    Image indicator_AIM120_OR1;
    [SerializeField] 
    Image indicator_AIM120_OR2;
    [SerializeField] 
    Image indicator_AIM9X_L;
    [SerializeField] 
    Image indicator_AIM9X_R;
    [SerializeField] 
    Image indicator_GBU31_L;
    [SerializeField] 
    Image indicator_GBU31_R;

    [Header("Active Weapon Indicator")]
    [SerializeField]
    TextMeshProUGUI activeWeaponText;
    [SerializeField] 
    TextMeshProUGUI masterArmText;

    Color weaponPresent = new Color(0f, 1f, 0f);
    Color weaponGone = new Color(0f, 0.25f, 0f);

    void Update()
    {
        // Gear
        gearText.text = planeAnimation.GearDown ? "DOWN" : "UP";
        gearText.color = planeAnimation.GearDown ? Color.green : Color.red;

        // Airbrake
        airbrakeText.text = plane.AirbrakeDeployed ? "DEPLOYED" : "OFF";
        airbrakeText.color = plane.AirbrakeDeployed ? Color.yellow : Color.green;

        // Engine
        float throttle = plane.Throttle;
        if (throttle <= 0.05f)
        {
            engineText.text = "IDLE";
            engineText.color = Color.gray;
        }
        else if (plane.EnginePowerOutput >= 50f)
        {
            engineText.text = "AB";
            engineText.color = Color.red;
        }
        else
        {
            engineText.text = "MIL";
            engineText.color = Color.green;
        }

        // Throttle bar
        if (throttleBar)
            throttleBar.SetValue(Engine.InvertThrottleGear(plane.EnginePowerOutput));
        
        // G-force
        float g = plane.LocalGForce.y / 9.81f;
        gforceText.text = (plane.LocalGForce.y / 9.81f).ToString("F1") + " G";
        // AOA
        aoaText.text = (plane.AngleOfAttack * Mathf.Rad2Deg).ToString("F1") + "°";

        // Master Arm
        masterArmText.text = weaponSystem.MasterArm ? "ARMED" : "SAFE";
        masterArmText.color = weaponSystem.MasterArm ? Color.green : Color.red;

        // Weapons
        aim120Text.text = $"{weaponSystem.AIM120Count} / 4";
        aim120Text.color = weaponSystem.AIM120Count > 0 ? Color.green : Color.red;

        aim9xText.text = $"{weaponSystem.AIM9XCount} / 2";
        aim9xText.color = weaponSystem.AIM9XCount > 0 ? Color.green : Color.red;

        gbu31Text.text = $"{weaponSystem.GBU31Count} / 2";
        gbu31Text.color = weaponSystem.GBU31Count > 0 ? Color.green : Color.red;

        gauText.text = $"{weaponSystem.GunRounds} RDS";
        gauText.color = weaponSystem.GunRounds > 0 ? Color.white : Color.red;

        activeWeaponText.text = weaponSystem.ActiveWeapon.ToString();

        indicator_AIM120_OL1.color = weaponSystem.AIM120ModelActive(0) ? weaponPresent : weaponGone;
        indicator_AIM120_OL2.color = weaponSystem.AIM120ModelActive(2) ? weaponPresent : weaponGone;
        indicator_AIM120_OR1.color = weaponSystem.AIM120ModelActive(1) ? weaponPresent : weaponGone;
        indicator_AIM120_OR2.color = weaponSystem.AIM120ModelActive(3) ? weaponPresent : weaponGone;
        indicator_AIM9X_L.color = weaponSystem.AIM9XModelActive(0) ? weaponPresent : weaponGone;
        indicator_AIM9X_R.color = weaponSystem.AIM9XModelActive(1) ? weaponPresent : weaponGone;
        indicator_GBU31_L.color = weaponSystem.GBU31ModelActive(0) ? weaponPresent : weaponGone;
        indicator_GBU31_R.color = weaponSystem.GBU31ModelActive(1) ? weaponPresent : weaponGone;
    }
}