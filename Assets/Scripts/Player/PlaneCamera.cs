using UnityEngine;
using UnityEngine.InputSystem;

public class PlaneCamera : MonoBehaviour
{
    public enum CameraMode { External, Cockpit }

    [Header("Camera Reference")]
    [SerializeField]
    new Camera camera;
    [SerializeField]
    float maxLookDeltaPerFrame = 50f;

    [Header("External View")]
    [SerializeField]
    Vector3 cameraOffset = new Vector3(0f, 2f, -12f);
    [SerializeField]
    Vector3 pivotOffset = new Vector3(0f, 0f, 7.5f);
    [SerializeField]
    Vector2 lookAngle = new Vector2(120f, 60f);
    [SerializeField]
    float movementScale = 1f;
    [SerializeField]
    float movementAlpha = 0.05f;
    [SerializeField]
    float externalFOV = 70f;
    [SerializeField]
    float mouseSensitivity = 1f;

    [Header("Cockpit View")]
    [SerializeField]
    Vector3 cockpitOffset = new Vector3(0f, 0.8f, -3.7f);
    [SerializeField]
    Vector2 cockpitLookAngle = new Vector2(120f, 80f);
    [SerializeField]
    float cockpitFOV = 70f;
    [SerializeField]
    LayerMask cockpitLayer;
    [SerializeField]
    LayerMask airframeLayer;
    [SerializeField]
    float cockpitZoomMin = -0.5f;
    [SerializeField]
    float cockpitZoomMax = 0.8f;
    [SerializeField]
    float cockpitZoomSpeed = 0.5f;

    float cockpitZoom = 0f;

    [Header("G-Force Effect")]
    [SerializeField]
    float gForceOffsetScale = 0.3f;
    [SerializeField]
    float gForceFOVScale = 2f;
    [SerializeField]
    float gForceAlpha = 0.05f;
    [SerializeField]
    float maxGForceOffset = 4f;
    [SerializeField]
    float maxGForceFOV = 15f;

    [Header("Death View")]
    [SerializeField]
    Vector3 deathOffset = new Vector3(0f, 3f, -20f);
    [SerializeField]
    float deathSensivity = 60f;

    Transform cameraTransform;
    Plane plane;
    Transform planeTransform;

    Vector2 lookInput;
    CameraMode currentMode = CameraMode.External;

    Vector2 look;
    Vector2 lookAverage;
    Vector3 avAverage;
    float gForceZAverage;
    float currentFOV;

    Vector2 deadLook;

    void Awake()
    {
        cameraTransform = camera.transform;
        currentFOV = externalFOV;
        camera.fieldOfView = currentFOV;

        look = Vector2.zero;
        lookAverage = Vector2.zero;
        cameraTransform.localPosition = cameraOffset;
    }

    public void SetPlane(Plane plane)
    {
        this.plane = plane;
        planeTransform = plane != null ? plane.transform : null;
        cameraTransform.SetParent(planeTransform);
        ApplyCullingMask();
    }

    public void SetInput(Vector2 input)
    {
        if (input.magnitude > maxLookDeltaPerFrame)
            return;
        lookInput = input;
    }

    public CameraMode CurrentMode => currentMode;

    public void SetExternalView()
    {
        if (currentMode == CameraMode.External)
        {
            ResetView();
            return;
        }

        currentMode = CameraMode.External;
        look = Vector2.zero;
        lookAverage = Vector2.zero;
        ApplyCullingMask();
    }

    public void SetCockpitView()
    {
        if (currentMode == CameraMode.Cockpit)
        {
            ResetView();
            return;
        }

        currentMode = CameraMode.Cockpit;
        look = Vector2.zero;
        lookAverage = Vector2.zero;
        avAverage = Vector3.zero;
        ApplyCullingMask();
    }

    public void ResetView()
    {
        look = Vector2.zero;
        lookAverage = Vector2.zero;
    }

    void ApplyCullingMask()
    {
        if (currentMode == CameraMode.Cockpit)
        {
            camera.cullingMask |= cockpitLayer;
            camera.cullingMask &= ~airframeLayer;
        }
        else
        {
            camera.cullingMask &= ~cockpitLayer;
            camera.cullingMask |= airframeLayer;
        }
    }

    void LateUpdate()
    {
        if (plane == null) return;

        if (plane.Dead)
        {
            UpdateDeadView();
            return;
        }

        UpdateGForceEffect();

        if (currentMode == CameraMode.Cockpit)
            UpdateCockpitView();
        else
            UpdateExternalView();
    }

    void UpdateGForceEffect()
    {
        float gZ = plane.LocalGForce.z / 9.81f;
        gForceZAverage = Mathf.Lerp(gForceZAverage, gZ, gForceAlpha);
    }

    void UpdateExternalView()
    {
        look += lookInput * mouseSensitivity;
        look.x = (look.x + 360f) % 360f;
        look.y = Mathf.Clamp(look.y, -lookAngle.y, lookAngle.y);
        lookAverage = look;

        var angularVelocity = plane.LocalAngularVelocity;
        angularVelocity.z = -angularVelocity.z;
        avAverage = Vector3.Lerp(avAverage, angularVelocity, movementAlpha);

        var rotation = Quaternion.Euler(-lookAverage.y, lookAverage.x, 0);
        var turningRotation = Quaternion.Euler(
            new Vector3(-avAverage.x, -avAverage.y, avAverage.z) * movementScale);

        float gOffset = Mathf.Clamp(gForceZAverage * gForceOffsetScale, -maxGForceOffset, maxGForceOffset);
        var dynamicOffset = cameraOffset + new Vector3(0f, 0f, -gOffset);

        cameraTransform.localPosition = pivotOffset + rotation * turningRotation * dynamicOffset;
        cameraTransform.localRotation = rotation * turningRotation;

        float targetFOV = externalFOV + Mathf.Clamp(gForceZAverage * gForceFOVScale, -maxGForceFOV, maxGForceFOV);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, gForceAlpha * 2f);
        camera.fieldOfView = currentFOV;
    }

    void UpdateCockpitView()
    {
        look += lookInput * mouseSensitivity;
        look.x = (look.x + 360f) % 360f;
        look.y = Mathf.Clamp(look.y, -cockpitLookAngle.y, cockpitLookAngle.y);
        lookAverage = look;

        float scroll = Mouse.current.scroll.ReadValue().y * 0.5f;
        cockpitZoom = Mathf.Clamp(cockpitZoom + scroll * cockpitZoomSpeed, cockpitZoomMin, cockpitZoomMax);

        var rotation = Quaternion.Euler(-lookAverage.y, lookAverage.x, 0);

        cameraTransform.localPosition = cockpitOffset;
        cameraTransform.localRotation = rotation;

        float targetFOV = cockpitFOV - cockpitZoom * 30f;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, 0.1f);
        camera.fieldOfView = currentFOV;
    }

    void UpdateDeadView()
    {
        deadLook += lookInput * deathSensivity * Time.deltaTime;
        deadLook.x = (deadLook.x + 360f) % 360f;
        deadLook.y = Mathf.Clamp(deadLook.y, -60f, 60f);

        lookAverage = deadLook;
        avAverage = Vector3.zero;

        var rotation = Quaternion.Euler(-deadLook.y, deadLook.x, 0);
        cameraTransform.localPosition = rotation * deathOffset;
        cameraTransform.localRotation = rotation;

        currentFOV = Mathf.Lerp(currentFOV, externalFOV, 0.05f);
        camera.fieldOfView = currentFOV;
    }
}