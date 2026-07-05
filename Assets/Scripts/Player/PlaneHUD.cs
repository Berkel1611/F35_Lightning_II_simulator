using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlaneHUD : MonoBehaviour
{
    [SerializeField]
    float updateRate;
    [SerializeField]
    Color normalColor;
    [SerializeField]
    Color lockColor;
    [SerializeField]
    List<GameObject> helpDialogs;

    [Header("HUD Elements")]
    [SerializeField]
    Compass compass;
    [SerializeField]
    PitchLadder pitchLadder;
    [SerializeField]
    Slider throttleArrow;
    [SerializeField]
    Bar throttleBar;
    [SerializeField]
    TextMeshProUGUI throttleText;
    [SerializeField]
    Transform hudCenter;
    [SerializeField]
    Transform velocityCenter;
    [SerializeField]
    TextMeshProUGUI aoaIndicator;
    [SerializeField]
    TextMeshProUGUI gforceIndicator;
    [SerializeField]
    AltitudeTape altitudeTape;
    [SerializeField]
    AirspeedTape airspeedTape;
    [SerializeField]
    Bar healthBar;
    [SerializeField]
    Image healthBarFill;
    [SerializeField]
    TextMeshProUGUI healthText;
    [SerializeField]
    Transform targetBox;
    [SerializeField]
    TextMeshProUGUI targetName;
    [SerializeField]
    TextMeshProUGUI targetRange;
    [SerializeField]
    Transform missileLock;
    [SerializeField]
    Transform reticle;
    [SerializeField]
    RectTransform reticleLine;
    [SerializeField]
    RectTransform targetArrow;
    [SerializeField]
    RectTransform missileArrow;
    [SerializeField]
    TextMeshProUGUI missileDistanceText;
    [SerializeField]
    float targetArrowThreshold;
    [SerializeField]
    float missileArrowThreshold;
    [SerializeField]
    float cannonRange;
    [SerializeField]
    float bulletSpeed;
    [SerializeField]
    GameObject aiMessage;

    [Header("Config Menu")]
    [SerializeField]
    GameObject configMenu;
    [SerializeField]
    GameObject helpDialog;
    [SerializeField]
    Toggle enableEngine;
    [SerializeField]
    Toggle enableFCS;
    [SerializeField]
    Toggle rollControl;
    [SerializeField]
    Toggle pitchControl;
    [SerializeField]
    Toggle yawControl;
    [SerializeField]
    UnityEngine.UI.Slider centerOfGravitySlider;
    [SerializeField]
    TextMeshProUGUI centerOfGravityLabel;

    [Header("Weapon Info ó External Only")]
    [SerializeField] 
    WeaponSystem weaponSystem;
    [SerializeField] 
    PlaneCamera planeCamera;
    [SerializeField] 
    GameObject weaponInfoPanel;
    [SerializeField] 
    TextMeshProUGUI activeWeaponText;
    [SerializeField] 
    TextMeshProUGUI masterArmText;
    [SerializeField] 
    TextMeshProUGUI ammoCountText;

    [Header("Uzbrojenie - target/lock")]
    [SerializeField]
    TargetingSystem targetingSystem;
    [SerializeField]
    MissileSystem missileSystem;
    [SerializeField]
    AITarget selfTarget;

    [Header("Missile Indicators")]
    [SerializeField]
    GameObject missileIndicatorPrefab;
    [SerializeField]
    Transform missileIndicatorParent;

    [Header("Mission Objective Indicator")]
    [SerializeField]
    RectTransform objectiveMarker;
    [SerializeField]
    TextMeshProUGUI objectiveLabel;
    [SerializeField]
    TextMeshProUGUI objectiveDistanceText;

    Dictionary<Missile, RectTransform> missileIndicators = new Dictionary<Missile, RectTransform>();

    Plane plane;
    Transform planeTransform;
    new Camera camera;
    Transform cameraTransform;
    RectTransform canvasTransform;
    Canvas canvas;

    GameObject hudCenterGO;
    GameObject velocityMarkerGO;
    GameObject targetBoxGO;
    Image targetBoxImage;
    GameObject missileLockGO;
    Image missileLockImage;
    GameObject reticleGO;
    GameObject targetArrowGO;
    GameObject missileArrowGO;
    GameObject objectiveMarkerGO;
    Transform objectiveTarget;
    string objectiveText;
    float objectiveMinDistance;

    float lastUpdateTime;

    private void Start()
    {
        canvasTransform = GetComponent<RectTransform>();
        canvas = GetComponent<Canvas>();

        if (hudCenter != null) hudCenterGO = hudCenter.gameObject;
        if (velocityCenter != null) velocityMarkerGO = velocityCenter.gameObject;
        if (targetBox != null) { targetBoxGO = targetBox.gameObject; targetBoxImage = targetBox.GetComponent<Image>(); }
        if (missileLock != null) { missileLockGO = missileLock.gameObject; missileLockImage = missileLock.GetComponent<Image>(); }
        if (reticle != null) reticleGO = reticle.gameObject;
        if (targetArrow != null) targetArrowGO = targetArrow.gameObject;
        if (missileArrow != null) missileArrowGO = missileArrow.gameObject;
        if (objectiveMarker != null) objectiveMarkerGO = objectiveMarker.gameObject;
    }

    public void SetPlane(Plane plane)
    {
        this.plane = plane;

        if (plane == null)
            planeTransform = null;
        else
            planeTransform = plane.GetComponent<Transform>();

        if (compass != null)
            compass.SetPlane(plane);

        if (pitchLadder != null)
            pitchLadder.SetPlane(plane);

        if (altitudeTape != null)
            altitudeTape.SetPlane(plane);

        if (airspeedTape != null)
            airspeedTape.SetPlane(plane);

        ResetToggle(enableEngine);
        ResetToggle(enableFCS);
        ResetToggle(rollControl);
        ResetToggle(pitchControl);
        ResetToggle(yawControl);

        centerOfGravitySlider.value = plane.CenterOfGravityPosition * 100;
    }

    public void SetCamera(Camera camera)
    {
        this.camera = camera;

        if (camera == null)
            cameraTransform = null;
        else
            cameraTransform = camera.GetComponent<Transform>();

        if (compass != null)
            compass.SetCamera(camera);

        if (pitchLadder != null)
            pitchLadder.SetCamera(camera);
    }

    public void ToggleHelpDialogs()
    {
        foreach (var dialog in helpDialogs)
            dialog.SetActive(!dialog.activeSelf);
    }

    void ResetToggle(Toggle toggle)
    {
        if (toggle != null)
            toggle.isOn = true;
    }

    void UpdateVelocityMarker()
    {
        var velocity = planeTransform.forward;

        if (plane.LocalVelocity.sqrMagnitude > 1)
            velocity = plane.Rigidbody.linearVelocity;

        var hudPos = TransformToHUDSpace(cameraTransform.position + velocity);

        if (hudPos.z > 0)
        {
            velocityMarkerGO.SetActive(true);
            velocityCenter.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
        }
        else
        {
            velocityMarkerGO.SetActive(false);
        }
    }

    void UpdateAOA()
    {
        aoaIndicator.text = string.Format("{0:0.0} AOA", plane.AngleOfAttack * Mathf.Rad2Deg);
    }

    void UpdateGForce()
    {
        var gforce = plane.LocalGForce.y / 9.81f;
        gforceIndicator.text = string.Format("{0:0.0} G", gforce);
    }

    Vector3 TransformToHUDSpace(Vector3 worldSpace)
    {
        var screenSpace = camera.WorldToScreenPoint(worldSpace);
        return (screenSpace - new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2)) / canvas.scaleFactor;
    }

    void UpdateHUDCenter()
    {
        var rotation = cameraTransform.localEulerAngles;
        var hudPos = TransformToHUDSpace(cameraTransform.position + planeTransform.forward);

        if (hudPos.z > 0)
        {
            hudCenterGO.SetActive(true);
            hudCenter.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
            hudCenter.localEulerAngles = new Vector3(0, 0, -rotation.z);
        }
        else
        {
            hudCenterGO.SetActive(false);
        }
    }

    void UpdateWeapons()
    {
        if (targetingSystem == null || !targetingSystem.HasTarget)
        {
            if (targetBoxGO != null) targetBoxGO.SetActive(false);
            if (missileLockGO != null) missileLockGO.SetActive(false);
            if (reticleGO != null) reticleGO.SetActive(false);
            if (targetArrowGO != null) targetArrowGO.SetActive(false);
            return;
        }

        var target = targetingSystem.SelectedTarget;
        float targetDistance = Vector3.Distance(planeTransform.position, target.Position);

        // Target box
        var targetPos = TransformToHUDSpace(target.Position);

        if (targetPos.z > 0)
        {
            targetBoxGO.SetActive(true);
            targetBox.localPosition = new Vector3(targetPos.x, targetPos.y, 0);
        }
        else
        {
            targetBoxGO.SetActive(false);
        }

        targetName.text = target.Name;
        targetRange.text = string.Format("{0:0} m", targetDistance);

        // MissileLock
        bool missileLocking = missileSystem != null && missileSystem.IsTracking;
        bool missileLocked = missileSystem != null && missileSystem.IsLocked;

        if (missileLocking)
        {
            float smoothProgress = Mathf.SmoothStep(0f, 1f, missileSystem.LockProgress);
            Vector3 lockWorldPos = Vector3.Lerp(
                plane.Rigidbody.position + transform.forward,
                target.Position,
                smoothProgress);

            var lockPos = TransformToHUDSpace(lockWorldPos);
            if (lockPos.z > 0)
            {
                missileLockGO.SetActive(true);
                missileLock.localPosition = new Vector3(lockPos.x, lockPos.y, 0);
            }
            else
            {
                missileLockGO.SetActive(false);
            }
        }
        else
        {
            missileLockGO.SetActive(false);
        }

        // Kolor lock
        Color boxColor = missileLocked ? lockColor : normalColor;
        foreach (var img in targetBox.GetComponentsInChildren<Image>())
            img.color = boxColor;
        foreach (var img in missileLock.GetComponentsInChildren<Image>())
            img.color = boxColor;
        targetName.color = boxColor;
        targetRange.color = boxColor;

        // Target arrow
        var targetDir = (target.Position - plane.Rigidbody.position).normalized;
        float targetAngle = Vector3.Angle(camera.transform.forward, targetDir);

        if (targetAngle > targetArrowThreshold)
        {
            targetArrowGO.SetActive(true);
            var targetDir3D = (target.Position - plane.Rigidbody.position).normalized;
            var right = cameraTransform.right;
            var up = cameraTransform.up;
            var dir2D = new Vector2(Vector3.Dot(targetDir3D, right), Vector3.Dot(targetDir3D, up)).normalized;
            float radius = 500f;
            targetArrow.localEulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, dir2D));
            targetArrow.localPosition = new Vector3(dir2D.x * radius, dir2D.y * radius, 0);
        }
        else
        {
            targetArrowGO.SetActive(false);
        }

        // Reticle
        if (weaponSystem.ActiveWeapon == WeaponSystem.WeaponType.Gun)
        {
            var leadPos = Utilities.FirstOrderIntercept(
                plane.Rigidbody.position, plane.Rigidbody.linearVelocity,
                bulletSpeed, target.Position, target.Velocity);
            var reticlePos = TransformToHUDSpace(leadPos);

            if (reticlePos.z > 0 && targetDistance <= cannonRange)
            {
                reticleGO.SetActive(true);
                reticle.localPosition = new Vector3(reticlePos.x, reticlePos.y, 0);

                // Linia ≥πczπce reticle z celem
                if (reticleLine != null)
                {
                    var r2 = new Vector2(reticlePos.x, reticlePos.y);
                    var t2 = new Vector2(targetPos.x, targetPos.y);
                    if (Mathf.Sign(targetPos.z) != Mathf.Sign(reticlePos.z)) r2 = -r2;
                    var error = r2 - t2;
                    reticleLine.localEulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, error) + 180f);
                    reticleLine.sizeDelta = new Vector2(reticleLine.sizeDelta.x, error.magnitude);
                }
            }
            else
            {
                reticleGO.SetActive(false);
            }
        }
        else
        {
            reticleGO.SetActive(false);
        }   
    }

    void UpdateWarnings()
    {
        if (selfTarget == null) return;

        var incomingMissile = selfTarget.GetIncomingMissile();

        if (incomingMissile != null)
        {
            if (missileArrowGO != null)
            {
                var missilePos = TransformToHUDSpace(incomingMissile.Rigidbody.position);
                var missileDir = (incomingMissile.Rigidbody.position - plane.Rigidbody.position).normalized;
                float missileAngle = Vector3.Angle(cameraTransform.forward, missileDir);

                if (missileAngle > missileArrowThreshold)
                {
                    missileArrowGO.SetActive(true);
                    var missileDir3D = (incomingMissile.Rigidbody.position - plane.Rigidbody.position).normalized;
                    var right = cameraTransform.right;
                    var up = cameraTransform.up;
                    var dir2D = new Vector2(Vector3.Dot(missileDir3D, right), Vector3.Dot(missileDir3D, up)).normalized;
                    float radius = 500f;
                    missileArrow.localEulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, dir2D));
                    missileArrow.localPosition = new Vector3(dir2D.x * radius, dir2D.y * radius, 0);

                    if (missileDistanceText != null)
                    {
                        missileDistanceText.gameObject.SetActive(true);
                        float dist = Vector3.Distance(incomingMissile.Rigidbody.position, plane.Rigidbody.position);
                        missileDistanceText.text = string.Format("{0:0}m", dist);
                    }
                }
                else
                {
                    missileArrowGO.SetActive(false);
                    if (missileDistanceText != null) 
                        missileDistanceText.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (missileArrowGO != null) missileArrowGO.SetActive(false);
            if (missileDistanceText != null) 
                missileDistanceText.gameObject.SetActive(false);
        }
    }

    void UpdateMissileIndicators()
    {
        var allMissiles = new List<Missile>();
        if (missileSystem != null)
            allMissiles.AddRange(missileSystem.FiredMissiles);
        if (selfTarget != null)
            allMissiles.AddRange(selfTarget.IncomingMissiles);

        // UsuÒ wskaüniki dla zniszczonych rakiet
        var toRemove = new List<Missile>();
        foreach (var kvp in missileIndicators)
        {
            if (!allMissiles.Contains(kvp.Key))
            {
                Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var m in toRemove)
            missileIndicators.Remove(m);

        // Dodaj wskaüniki dla nowych rakiet
        foreach (var missile in allMissiles)
        {
            if (!missileIndicators.ContainsKey(missile))
            {
                var prefab = missileIndicatorPrefab;
                var go = Instantiate(prefab, missileIndicatorParent);
                missileIndicators[missile] = go.GetComponent<RectTransform>();

                bool isEnemy = selfTarget != null && selfTarget.IncomingMissiles.Contains(missile);
                Color indicatorColor = isEnemy ? new Color(1f, 0f, 0f, 1f) : new Color(0f, 1f, 0f, 1f);
                foreach (var img in go.GetComponentsInChildren<Image>())
                    img.color = indicatorColor;
                foreach (var txt in go.GetComponentsInChildren<TextMeshProUGUI>())
                    txt.color = indicatorColor;
            }

            var indicator = missileIndicators[missile];
            var hudPos = TransformToHUDSpace(missile.Rigidbody.position);
            float dist = Vector3.Distance(missile.Rigidbody.position, plane.Rigidbody.position);

            if (hudPos.z > 0)
            {
                indicator.gameObject.SetActive(true);
                indicator.localPosition = new Vector3(hudPos.x, hudPos.y, 0);

                var texts = indicator.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = missile.gameObject.name;
                if (texts.Length > 1) texts[1].text = string.Format("{0:0}m", dist);
            }
            else
            {
                indicator.gameObject.SetActive(false);
            }
        }
    }

    void UpdateMissionObjective()
    {
        if (objectiveTarget == null || objectiveMarkerGO == null)
        {
            if (objectiveMarkerGO != null) objectiveMarkerGO.SetActive(false);
            return;
        }

        float distance = Vector3.Distance(plane.Rigidbody.position, objectiveTarget.position);

        if (distance < objectiveMinDistance)
        {
            objectiveMarkerGO.SetActive(false);
            return;
        }

        var hudPos = TransformToHUDSpace(objectiveTarget.position);

        if (hudPos.z > 0)
        {
            objectiveMarkerGO.SetActive(true);
            objectiveMarker.localPosition = new Vector3(hudPos.x, hudPos.y, 0);

            if (objectiveLabel != null)
                objectiveLabel.text = objectiveText;

            if (objectiveDistanceText != null)
                objectiveDistanceText.text = string.Format("{0:0} m", distance);
        }
        else
        {
            objectiveMarkerGO.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (plane == null || camera == null) return;

        float power = Engine.InvertThrottleGear(plane.EnginePowerOutput);

        throttleArrow.SetValue(plane.Throttle);
        throttleBar.SetValue(power);
        throttleText.text = string.Format("{0:0}%", plane.EnginePowerOutput);

        healthBar.SetValue(plane.Health / plane.MaxHealth);
        healthText.text = string.Format("{0:0}%", plane.Health / plane.MaxHealth * 100f);

        if (healthBarFill != null)
        {
            if (plane.Health / plane.MaxHealth > 0.5f)
                healthBarFill.color = new Color(0, 1, 0, 0.86f);
            else if (plane.Health / plane.MaxHealth > 0.25f)
                healthBarFill.color = new Color(1, 1, 0, 0.86f);
            else
                healthBarFill.color = new Color(1, 0, 0, 0.86f);
        }

        if (!plane.Dead) {
            UpdateVelocityMarker();
            UpdateHUDCenter();
            UpdateWeapons();
            UpdateWarnings();
            UpdateMissionObjective();
            UpdateMissileIndicators();
        }
        else
        {
            hudCenterGO.SetActive(false);
            velocityMarkerGO.SetActive(false);
        }

        // update these elements at reduced rate to make reading them easier
        if (Time.time > lastUpdateTime + (1f / updateRate))
        {
            UpdateAOA();
            UpdateGForce();
            lastUpdateTime = Time.time;
        }

        // Weapon info ó tylko external view
        if (weaponSystem != null && planeCamera != null && weaponInfoPanel != null)
        {
            bool isExternal = planeCamera.CurrentMode == PlaneCamera.CameraMode.External;
            weaponInfoPanel.SetActive(isExternal);

            if (isExternal)
            {
                activeWeaponText.text = weaponSystem.ActiveWeapon.ToString();
                masterArmText.text = weaponSystem.MasterArm ? "ARMED" : "SAFE";
                masterArmText.color = weaponSystem.MasterArm ? Color.green : Color.red;
                switch (weaponSystem.ActiveWeapon)
                {
                    case WeaponSystem.WeaponType.Gun:
                        ammoCountText.text = $"{weaponSystem.GunRounds} RDS";
                        break;
                    case WeaponSystem.WeaponType.AIM120:
                        ammoCountText.text = $"{weaponSystem.AIM120Count} / 4";
                        break;
                    case WeaponSystem.WeaponType.AIM9X:
                        ammoCountText.text = $"{weaponSystem.AIM9XCount} / 2";
                        break;
                    case WeaponSystem.WeaponType.GBU31:
                        ammoCountText.text = $"{weaponSystem.GBU31Count} / 2";
                        break;
                }
                ammoCountText.color = weaponSystem.HasAmmo ? Color.green : Color.red;
            }
        }
    }

    public void SetMissionObjective(Transform target, string label, float minDistance)
    {
        objectiveTarget = target;
        objectiveText = label;
        objectiveMinDistance = minDistance;
    }

    public void ClearMissionObjective()
    {
        objectiveTarget = null;
    }

    public void SetEnableEngine(bool value)
    {
        if (plane != null)
            plane.EngineEnabled = value;
    }

    public void SetEnableFCS(bool value)
    {
        if (plane != null)
            plane.EnableFCS = value;
    }

    public void SetEnableRollControl(bool value)
    {
        if (plane != null)
            plane.EnableRollControl = value;
    }

    public void SetEnablePitchControl(bool value)
    {
        if (plane != null)
            plane.EnablePitchControl = value;
    }

    public void SetEnableYawControl(bool value)
    {
        if (plane != null)
            plane.EnableYawControl = value;
    }

    public void SetCenterOfGravity(float value)
    {
        if (plane != null)
        {
            float effectiveValue = value / 100f;
            plane.CenterOfGravityPosition = effectiveValue;
            centerOfGravityLabel.text = string.Format("{0:.00}", effectiveValue);
        }
    }

    public void ToggleConfigMenu()
    {
        if (configMenu != null)
            configMenu.SetActive(!configMenu.activeSelf);
    }

    public void ToggleHelpMenu()
    {
        if (helpDialog != null)
            helpDialog.SetActive(!helpDialog.activeSelf);
    }
}
