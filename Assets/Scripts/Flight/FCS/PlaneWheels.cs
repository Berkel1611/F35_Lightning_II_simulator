using UnityEngine;

public class PlaneWheels : MonoBehaviour
{
    [SerializeField]
    WheelCollider[] wheelColliders;
    [SerializeField] 
    Transform[] wheelMeshes;
    [SerializeField] 
    Plane plane;

    [SerializeField] 
    float brakeTorque = 5000f;
    [SerializeField] 
    float maxSteerAngle = 30f;
    [SerializeField] 
    float noseWheelLockSpeed = 36f;

    [SerializeField]
    float unstickTorque = 500f;
    [SerializeField]
    float unstickSpeedThreshold = 1f;

    void FixedUpdate()
    {
        // hamowanie
        float brake = plane.AirbrakeDeployed ? brakeTorque : 0f;
        foreach (WheelCollider wheel in wheelColliders)
            wheel.brakeTorque = brake;

        // skrêt kó³kiem nosowym
        float speed = plane.Rigidbody.linearVelocity.magnitude;
        float motor = (plane.Throttle > 0.05f && speed < unstickSpeedThreshold) ? unstickTorque : 0f;
        foreach (WheelCollider wheel in wheelColliders)
            wheel.motorTorque = motor;
        float speedFactor = 1f - Mathf.Clamp01(speed / noseWheelLockSpeed);
        wheelColliders[0].steerAngle = -plane.ControlSurfacesNormalized.rudder * maxSteerAngle * speedFactor;
    }

    void Update()
    {
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (!wheelColliders[i].enabled) continue;

            wheelColliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelMeshes[i].position = pos;
            wheelMeshes[i].rotation = rot;
        }
    }

    public bool IsGrounded()
    {
        foreach (var wheel in wheelColliders)
            if (wheel.isGrounded) return true;
        return false;
    }
}
