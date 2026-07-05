using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneAnimation : MonoBehaviour
{
    [Header("Control Surfaces")]
    [SerializeField] 
    Transform aileronLeft;
    [SerializeField] 
    Transform aileronRight;
    [SerializeField] 
    Transform elevatorLeft;
    [SerializeField] 
    Transform elevatorRight;
    [SerializeField] 
    Transform rudderLeft;
    [SerializeField] 
    Transform rudderRight;

    [SerializeField] 
    float aileronMaxAngle = 20f;
    [SerializeField] 
    float elevatorMaxAngle = 20f;
    [SerializeField] 
    float rudderMaxAngle = 20f;
    [SerializeField] 
    float controlSurfaceSpeed = 5f;

    [Header("Afterburner")]
    [SerializeField] 
    List<GameObject> afterburnerGraphics;
    [SerializeField] 
    float afterburnerThreshold = 50f;
    [SerializeField] 
    float afterburnerMinSize = 0.5f;
    [SerializeField] 
    float afterburnerMaxSize = 1.5f;

    [Header("Landing Gear")]
    [SerializeField] 
    string retractTrigger = "Retract";
    [SerializeField] 
    string extendTrigger = "Extend";

    [Header("Cockpit Animations")]
    [SerializeField]
    Transform sidestick;
    [SerializeField]
    float sidestickPitchMaxAngle = 15f;
    [SerializeField]
    float sidestickRollMaxAngle = 10f;
    [SerializeField]
    Transform throttleStick;
    [SerializeField]
    float throttleRange = 0.1f;
    [SerializeField]
    Transform pedalLeft;
    [SerializeField]
    Transform pedalRight;
    [SerializeField]
    float pedalMaxAngle = 15f;
    [SerializeField]
    float hotasSpeed = 8f;


    [SerializeField] 
    Plane plane;
    [SerializeField] 
    Animator animator;
    [SerializeField] 
    PlaneWheels planeWheels;

    // Pocz¹tkowe rotacje dla powierzchni sterowych
    Quaternion initAileronLeft, initAileronRight;
    Quaternion initElevatorLeft, initElevatorRight;
    Quaternion initRudderLeft, initRudderRight;
    // Wizualne k¹ty powierzchni sterowych (wyg³adzone)
    float visAileronLeft, visAileronRight;
    float visElevator;
    float visRudder;

    // Pocz¹tkowe pozycje dla elementów HOTAS
    Quaternion initSidestick;
    Vector3 initThrottle;
    Quaternion initPedalLeft, initPedalRight;
    // Wewnêtrzne wartoœci hotas wyg³adzone
    float visSidestickPitch, visSidestickRoll;
    float visThrottle;
    float visPedal;

    List<Transform> afterburnerTransforms = new List<Transform>();

    bool gearDown = true;

    private void Start()
    {
        if (aileronLeft) initAileronLeft = aileronLeft.localRotation;
        if (aileronRight) initAileronRight = aileronRight.localRotation;
        if (elevatorLeft) initElevatorLeft = elevatorLeft.localRotation;
        if (elevatorRight) initElevatorRight = elevatorRight.localRotation;
        if (rudderLeft) initRudderLeft = rudderLeft.localRotation;
        if (rudderRight) initRudderRight = rudderRight.localRotation;
        if (sidestick) initSidestick = sidestick.localRotation;
        if (throttleStick) initThrottle = throttleStick.localPosition;
        if (pedalLeft) initPedalLeft = pedalLeft.localRotation;
        if (pedalRight) initPedalRight = pedalRight.localRotation;

        foreach (var go in afterburnerGraphics)
            afterburnerTransforms.Add(go.transform);
    }

    public void ToggleLandingGear()
    {
        if (planeWheels.IsGrounded()) return;

        gearDown = !gearDown;
        animator.SetTrigger(gearDown ? extendTrigger : retractTrigger);
    }

    public bool GearDown => gearDown;

    void UpdateControlSurfaces()
    {
        var surfaces = plane.ControlSurfacesNormalized;
        float t = Time.deltaTime * controlSurfaceSpeed;

        visElevator = Mathf.Lerp(visElevator, surfaces.elevator * elevatorMaxAngle, t);
        visAileronLeft = Mathf.Lerp(visAileronLeft, surfaces.aileron * aileronMaxAngle, t);
        visAileronRight = Mathf.Lerp(visAileronRight, -surfaces.aileron * aileronMaxAngle, t);
        visRudder = Mathf.Lerp(visRudder, surfaces.rudder * rudderMaxAngle, t);

        if (aileronLeft)
            aileronLeft.localRotation = initAileronLeft * Quaternion.Euler(visAileronLeft, 0, 0);
        if (aileronRight)
            aileronRight.localRotation = initAileronRight * Quaternion.Euler(visAileronRight, 0, 0);
        if (elevatorLeft)
            elevatorLeft.localRotation = initElevatorLeft * Quaternion.Euler(-visElevator, 0, 0);
        if (elevatorRight)
            elevatorRight.localRotation = initElevatorRight * Quaternion.Euler(-visElevator, 0, 0);
        if (rudderLeft)
            rudderLeft.localRotation = initRudderLeft * Quaternion.Euler(0, 0, visRudder);
        if (rudderRight)
            rudderRight.localRotation = initRudderRight * Quaternion.Euler(0, 0, visRudder);
    }

    void UpdateHOTAS()
    {
        var ctrl = plane.ControlSurfacesNormalized;
        float t = Time.deltaTime * hotasSpeed;

        // Sidestick
        visSidestickPitch = Mathf.Lerp(visSidestickPitch,
            ctrl.elevator * sidestickPitchMaxAngle, t);
        visSidestickRoll = Mathf.Lerp(visSidestickRoll,
            ctrl.aileron * sidestickRollMaxAngle, t);

        if (sidestick)
            sidestick.localRotation = initSidestick
                * Quaternion.Euler(visSidestickPitch, 0, -visSidestickRoll);

        // Throttle
        visThrottle = Mathf.Lerp(visThrottle, plane.Throttle, t);
        if (throttleStick)
            throttleStick.localPosition = initThrottle
                + new Vector3(0, 0, visThrottle * throttleRange);

        // Peda³y
        visPedal = Mathf.Lerp(visPedal, ctrl.rudder, t);
        if (pedalLeft)
            pedalLeft.localRotation = initPedalLeft * 
                Quaternion.Euler(0, 0, -visPedal * pedalMaxAngle);
        if (pedalRight)
            pedalRight.localRotation = initPedalRight * 
                Quaternion.Euler(0, 0, visPedal * pedalMaxAngle);
    }

    void UpdateAfterburners()
    {
        float power = plane.EnginePowerOutput;
        bool active = power >= afterburnerThreshold;
        float t = Mathf.Clamp01(Mathf.InverseLerp(afterburnerThreshold, 100f, power));
        float size = Mathf.Lerp(afterburnerMinSize, afterburnerMaxSize, t);

        for (int i = 0; i < afterburnerGraphics.Count; i++)
        {
            afterburnerGraphics[i].SetActive(active);
            if (active)
            {
                var s = afterburnerTransforms[i].localScale;
                afterburnerTransforms[i].localScale = new Vector3(size, s.y, size);
            }
        }
    }

    void Update()
    {
        if (plane.Dead) return;
        UpdateControlSurfaces();
        UpdateHOTAS();
        UpdateAfterburners();
    }
}
