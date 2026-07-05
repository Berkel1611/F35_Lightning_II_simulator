using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    new Camera camera;
    [SerializeField]
    Plane plane;
    [SerializeField]
    PlaneAnimation planeAnimation;
    [SerializeField]
    PlaneHUD planeHUD;
    [SerializeField]
    WeaponSystem weaponSystem;
    [SerializeField]
    GunSystem gunSystem;
    [SerializeField]
    MissileSystem missileSystem;
    [SerializeField]
    BombSystem bombSystem;
    [SerializeField]
    float engineStartDuration = 6f;

    Vector3 controlInput;
    PlaneCamera planeCamera;

    public enum EngineStartState { Off, Starting, Running }

    EngineStartState engineState = EngineStartState.Off;
    float engineStartTimer;

    public EngineStartState EngineState => engineState;


    private void Awake()
    {
        planeCamera = GetComponent<PlaneCamera>();
    }

    private void Start()
    {
        if (plane != null)
        {
            plane.EngineEnabled = false;
            SetPlane(plane);
        }
    }

    public void SetPlane(Plane plane)
    {
        this.plane = plane;

        if (plane != null && planeHUD != null)
        {
            planeHUD.SetPlane(plane);
            planeHUD.SetCamera(camera);
        }

        planeCamera.SetPlane(plane);
    }

    public void SetThrottleInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;
        if (engineState != EngineStartState.Running) return;

        plane.SetThrottleInput(context.ReadValue<float>());
    }

    public void OnRollPitchInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;

        var input = context.ReadValue<Vector2>();
        controlInput = new Vector3(input.y, controlInput.y, -input.x);
    }

    public void OnYawInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;
        
        var input = context.ReadValue<float>();
        controlInput = new Vector3(controlInput.x, input, controlInput.z);
    }
    
    public void OnCameraInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;

        var input = context.ReadValue<Vector2>();
        planeCamera.SetInput(input);
    }

    public void OnToggleLandingGear(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        planeAnimation.ToggleLandingGear();
    }

    public void OnExternalView(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        planeCamera.SetExternalView();
    }

    public void OnCockpitView(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        planeCamera.SetCockpitView();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        bool held = context.phase == InputActionPhase.Performed
            || context.phase == InputActionPhase.Started;
        gunSystem?.SetFireInput(held);
        missileSystem?.SetFireInput(held);
        bombSystem?.SetFireInput(held);
    }

    public void OnCycleWeapon(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        if (weaponSystem == null) return;
        weaponSystem.CycleWeapon();
    }

    public void OnMasterArm(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        if (weaponSystem == null) return;
        weaponSystem.ToggleMasterArm();
    }

    public void OnEngineToggle(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        if (plane == null) return;

        if (engineState == EngineStartState.Off)
        {
            engineState = EngineStartState.Starting;
            engineStartTimer = 0f;
        }
        else if (engineState == EngineStartState.Running)
        {
            engineState = EngineStartState.Off;
            plane.EngineEnabled = false;
            plane.SetThrottleInput(0f);
        }
    }

    private void Update()
    {
        if (plane == null) return;

        if (engineState == EngineStartState.Starting)
        {
            engineStartTimer += Time.deltaTime;
            if (engineStartTimer >= engineStartDuration)
            {
                engineState = EngineStartState.Running;
                plane.EngineEnabled = true;
            }
        }

        plane.SetControlInput(controlInput);
    }
}
