using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AirplaneController : MonoBehaviour
{
    [SerializeField]
    List<AeroSurface> controlSurfaces = null;
    //[SerializeField]
    //List<WheelCollider> wheels = null;
    [SerializeField]
    float rollControlSensitivity = 0.2f;
    [SerializeField]
    float pitchControlSensitivity = 0.2f;
    [SerializeField]
    float yawControlSensitivity = 0.2f;

    [Range(-1, 1)]
    public float Pitch;
    [Range(-1, 1)]
    public float Yaw;
    [Range(-1, 1)]
    public float Roll;
    [Range(0, 1)]
    public float Flap;
    [SerializeField]
    Text displayText = null;

    float thrustPercent;
    float brakesTorque;

    AircraftPhysics aircraftPhysics;
    Rigidbody rb;

    [Header("Control Surfaces (Visual Transforms)")]
    public Transform aileronLeft;
    public Transform aileronRight;
    public Transform elevatorLeft;
    public Transform elevatorRight;
    public Transform rudderLeft;
    public Transform rudderRight;

    [Header("Visual Animation Settings")]
    public float aileronMaxAngle = 20f;
    public float elevatorMaxAngle = 20f;
    public float rudderMaxAngle = 20f;
    public float controlSurfaceSpeed = 5f;

    private Quaternion initAileronLeft;
    private Quaternion initAileronRight;
    private Quaternion initElevatorLeft;
    private Quaternion initElevatorRight;
    private Quaternion initRudderLeft;
    private Quaternion initRudderRight;

    private float visAileronLeft = 0f;
    private float visAileronRight = 0f;
    private float visElevator = 0f;
    private float visRudder = 0f;

    [Header("Landing Gear")]
    public Transform wheelLeft;
    public Transform wheelRight;
    public Transform wheelNose;
    public KeyCode gearToggleKey = KeyCode.G;
    private Animator animator;
    private bool gearDown = true;

    private void Start()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (aileronLeft != null) initAileronLeft = aileronLeft.localRotation;
        if (aileronRight != null) initAileronRight = aileronRight.localRotation;
        if (elevatorLeft != null) initElevatorLeft = elevatorLeft.localRotation;
        if (elevatorRight != null) initElevatorRight = elevatorRight.localRotation;
        if (rudderLeft != null) initRudderLeft = rudderLeft.localRotation;
        if (rudderRight != null) initRudderRight = rudderRight.localRotation;
    }

    private void Update()
    {
        Pitch = 0f;
        Roll = 0f;
        Yaw = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) Pitch = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) Pitch = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) Roll = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) Roll = 1f;
        if (Input.GetKey(KeyCode.Q)) Yaw = -1f;
        if (Input.GetKey(KeyCode.E)) Yaw = 1f;

        if (Input.GetKeyDown(KeyCode.Space))
            thrustPercent = thrustPercent > 0 ? 0 : 1f;

        if (Input.GetKeyDown(KeyCode.F))
            Flap = Flap > 0 ? 0 : 0.3f;

        //if (Input.GetKeyDown(KeyCode.B))
        //    brakesTorque = brakesTorque > 0 ? 0 : 100f;

        // HUD
        if (displayText != null)
        {
            displayText.text = "V: " + ((int)rb.linearVelocity.magnitude).ToString("D3") + " m/s\n";
            displayText.text += "A: " + ((int)transform.position.y).ToString("D4") + " m\n";
            displayText.text += "T: " + (int)(thrustPercent * 100) + "%\n";
            displayText.text += brakesTorque > 0 ? "B: ON" : "B: OFF";
        }

        // Podwozie
        HandleLandingGear();

        if (Time.frameCount % 30 == 0)
        {
            Vector3 v = rb.linearVelocity;
            Debug.Log($"Speed: {v.magnitude:F1} m/s | " +
                      $"Thrust: {aircraftPhysics.thrust:F0} N | " +
                      $"Alt: {transform.position.y:F1} m | " +
                      $"Pitch: {Pitch:F2} Roll: {Roll:F2}");
        }
    }

    private void FixedUpdate()
    {
        SetControlSurfecesAngles(Pitch, Roll, Yaw, Flap);
        aircraftPhysics.SetThrustPercent(thrustPercent);
        //foreach (var wheel in wheels)
        //{
        //    wheel.brakeTorque = brakesTorque;
        //    // small torque to wake up wheel collider
        //    wheel.motorTorque = 0.01f;
        //}

        // Animacje wizualne
        AnimateControlSurfaces(Pitch, Roll, Yaw);
    }

    public void SetControlSurfecesAngles(float pitch, float roll, float yaw, float flap)
    {
        foreach (var surface in controlSurfaces)
        {
            if (surface == null || !surface.IsControlSurface) continue;
            switch (surface.InputType)
            {
                case ControlInputType.Pitch:
                    surface.SetFlapAngle(pitch * pitchControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Roll:
                    surface.SetFlapAngle(roll * rollControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Yaw:
                    surface.SetFlapAngle(yaw * yawControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Flap:
                    surface.SetFlapAngle(Flap * surface.InputMultiplyer);
                    break;
            }
        }
    }

    private void AnimateControlSurfaces(float pitch, float roll, float yaw)
    {
        float t = Time.fixedDeltaTime * controlSurfaceSpeed;

        visElevator = Mathf.Lerp(visElevator, pitch * elevatorMaxAngle, t);
        visAileronLeft = Mathf.Lerp(visAileronLeft, roll * aileronMaxAngle, t);
        visAileronRight = Mathf.Lerp(visAileronRight, -roll * aileronMaxAngle, t);
        visRudder = Mathf.Lerp(visRudder, yaw * rudderMaxAngle, t);

        if (aileronLeft != null)
            aileronLeft.localRotation = initAileronLeft * Quaternion.Euler(visAileronLeft, 0f, 0f);
        if (aileronRight != null)
            aileronRight.localRotation = initAileronRight * Quaternion.Euler(visAileronRight, 0f, 0f);
        if (elevatorLeft != null)
            elevatorLeft.localRotation = initElevatorLeft * Quaternion.Euler(-visElevator, 0f, 0f);
        if (elevatorRight != null)
            elevatorRight.localRotation = initElevatorRight * Quaternion.Euler(-visElevator, 0f, 0f);
        if (rudderLeft != null)
            rudderLeft.localRotation = initRudderLeft * Quaternion.Euler(0f, 0f, -visRudder);
        if (rudderRight != null)
            rudderRight.localRotation = initRudderRight * Quaternion.Euler(0f, 0f, -visRudder);
    }

    private void HandleLandingGear()
    {
        if (!Input.GetKeyDown(gearToggleKey)) return;

        if (gearDown)
        {
            animator.SetTrigger("Retract");
            gearDown = false;
            StartCoroutine(DisableWheelCollidersAfterDelay(7.5f));
        }
        else
        {
            animator.SetTrigger("Extend");
            gearDown = true;
            SetWheelColliders(true);
        }
    }

    private IEnumerator DisableWheelCollidersAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetWheelColliders(false);
    }

    private void SetWheelColliders(bool enabled)
    {
        foreach (var wheel in new[] { wheelLeft, wheelRight, wheelNose })
        {
            if (wheel == null) continue;
            var col = wheel.GetComponent<Collider>();
            if (col != null) col.enabled = enabled;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            SetControlSurfecesAngles(Pitch, Roll, Yaw, Flap);
    }
}
