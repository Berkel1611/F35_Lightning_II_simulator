using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kontroler AI dla F-35. Wysy³a te same inputy do Plane.cs co PlayerController.
/// Stany: Patrol, Engage, DodgeMissile, RecoverSpeed, AvoidGround.
/// </summary>
public class AIController : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    Plane plane;
    [SerializeField]
    GunSystem gunSystem;
    [SerializeField]
    MissileSystem missileSystem;
    [SerializeField]
    AITarget selfTarget;

    [Header("Prêdkoœæ")]
    [SerializeField]
    float minSpeed = 100f;
    [SerializeField]
    float maxSpeed = 250f;
    [SerializeField]
    float recoverSpeedMin = 80f;
    [SerializeField]
    float recoverSpeedMax = 120f;
    [SerializeField]
    float patrolSpeed = 150f;

    [Header("Steering")]
    [SerializeField]
    float steeringSpeed = 2f;
    [SerializeField]
    float rollFactor = 0.05f;
    [SerializeField]
    float yawfactor = 0.05f;
    [SerializeField]
    float pitchUpThreshold = -20f;
    [SerializeField]
    float fineSteeringAngle = 10f;

    [Header("Unikanie ziemi")]
    [SerializeField]
    float groundAvoidanceDistance = 1500f;
    [SerializeField]
    float groundAvoidanceAngle = -10f;
    [SerializeField]
    float groundAvoidanceMinSpeed = 150f;
    [SerializeField]
    float groundAvoidanceMaxSpeed = 250f;
    [SerializeField]
    LayerMask groundLayer;

    [Header("Patrol")]
    [SerializeField]
    float patrolRadius = 5000f;
    [SerializeField]
    float patrolMinAltitude = 500f;
    [SerializeField]
    float patrolMaxAltitude = 3000f;
    [SerializeField]
    float waypointRadius = 300f;

    [Header("Dzia³ko")]
    [SerializeField]
    float cannonRange = 1500f;
    [SerializeField]
    float cannonMaxAngle = 5f;
    [SerializeField]
    float cannonBurstLength = 1.5f;
    [SerializeField]
    float cannonBurstCooldown = 3f;
    [SerializeField]
    float bulletSpeed = 1000f;

    [Header("Rakiety")]
    [SerializeField]
    float missileMinRange = 500f;
    [SerializeField]
    float missileMaxRange = 8000f;
    [SerializeField]
    float missileMaxAngle = 30f;
    [SerializeField]
    float missileCooldown = 5f;
    [SerializeField]
    float missileLockDelay = 1f;

    [Header("Unikanie rakiet")]
    [SerializeField]
    float minDodgeDistance = 300f;

    // Stan wewnêtrzny
    enum AIState { Patrol, Engage, DodgeMissile, RecoverSpeed }
    AIState state = AIState.Patrol;

    AITarget target;
    Vector3 patrolPoint;
    Vector3 lastInput;
    bool isRecoveringSpeed;

    // Dzia³ko
    bool cannonFiring;
    float cannonBurstTimer;
    float cannonCooldownTimer;

    // Rakiety
    float missileCooldownTimer;
    float missileLockTimer;

    // Unikanie rakiet
    bool dodging;
    Vector3 lastDodgePoint;
    List<Vector3> dodgeOffsets = new List<Vector3>();
    const float dodgeUpdateInterval = 0.25f;
    float dodgeTimer;

    // Reaction delay queue
    struct ControlInput
    {
        public float time;
        public Vector3 targetPosition;
    }
    Queue<ControlInput> inputQueue = new Queue<ControlInput>();
    [SerializeField]
    float reactionDelayMin = 0.2f;
    [SerializeField]
    float reactionDelayMax = 0.8f;
    [SerializeField]
    float reactionDelayDistance = 1000f;

    private void Start()
    {
        // ZnajdŸ gracza jako cel
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            target = player.GetComponent<AITarget>();

        GeneratePatrolPoint();
    }

    private void FixedUpdate()
    {
        if (plane.Dead) return;

        float dt = Time.fixedDeltaTime;

        Vector3 steering = Vector3.zero;
        float throttle = 0f;
        bool emergency = false;

        // Unikanie ziemi (najwy¿szy priorytet)
        if (CheckGroundAvoidance())
        {
            steering = AvoidGround();
            throttle = 1f;
            emergency = true;
        }
        else
        {
            // Unikanie rakiet
            Missile incomingMissile = selfTarget != null ? selfTarget.GetIncomingMissile() : null;
            if (incomingMissile != null)
            {
                state = AIState.DodgeMissile;
                if (!dodging)
                {
                    dodging = true;
                    lastDodgePoint = plane.Rigidbody.position;
                    dodgeTimer = 0f;
                }
                var dodgePos = GetDodgePosition(dt, incomingMissile);
                steering = CalculateSteering(dt, dodgePos);
                throttle = 1f;
                emergency = true;
            }
            else
            {
                dodging = false;
                if (state == AIState.DodgeMissile)
                    state = target != null ? AIState.Engage : AIState.Patrol;
            }

            // Odzyskiwanie prêdkoœci
            if (!emergency && (plane.LocalVelocity.z < recoverSpeedMin || isRecoveringSpeed))
            {
                isRecoveringSpeed = plane.LocalVelocity.z < recoverSpeedMax;
                state = AIState.RecoverSpeed;
                steering = RecoverSpeed();
                throttle = 1f;
                emergency = true;
            }
            else if (!emergency)
            {
                isRecoveringSpeed = false;

                // Wybór stanu
                if (target != null && target.IsAlive)
                {
                    float targetAltitude = target.Position.y;
                    float myAltitude = plane.Rigidbody.position.y;

                    if (targetAltitude < 300f || myAltitude < 500f)
                        state = AIState.Patrol;
                    else
                        state = AIState.Engage;
                }
                else
                    state = AIState.Patrol;

                float altitudeDiff = (target != null ? target.Position.y : 0) - plane.Rigidbody.position.y;
                float minSpd = altitudeDiff > 200f ? maxSpeed : minSpeed;
                throttle = CalculateThrottle(minSpd, maxSpeed);
            }
        }

        // Oblicz target position dla kolejki
        Vector3 targetPosition = state == AIState.Patrol
            ? patrolPoint
            : (target != null ? GetLeadPosition() : patrolPoint);

        inputQueue.Enqueue(new ControlInput { time = Time.time, targetPosition = targetPosition });

        plane.SetThrottleInput(throttle);

        if (emergency)
        {
            if (isRecoveringSpeed)
                steering.x = Mathf.Clamp(steering.x, -0.5f, 0.5f);
            plane.SetControlInput(steering);
        }
        else
        {
            SteerToTarget(dt, targetPosition);
        }

        // Patrol waypoint
        if (state == AIState.Patrol)
        {
            if (Vector3.Distance(plane.Rigidbody.position, patrolPoint) < waypointRadius)
                GeneratePatrolPoint();
        }

        // Uzbrojenie
        if (state == AIState.Engage)
            UpdateWeapons(dt);
        else
        {
            gunSystem.SetFireInput(false);
            cannonFiring = false;
        }
    }

    // Steering

    Vector3 CalculateSteering(float dt, Vector3 targetPosition)
    {
        var error = targetPosition - plane.Rigidbody.position;
        error = Quaternion.Inverse(plane.Rigidbody.rotation) * error;

        var errorDir = error.normalized;
        var pitchError = new Vector3(0, error.y, error.z).normalized;
        var rollError = new Vector3(error.x, error.y, 0).normalized;
        var yawError = new Vector3(error.x, 0, error.z).normalized;

        var targetInput = new Vector3();

        var pitch = Vector3.SignedAngle(Vector3.forward, pitchError, Vector3.right);
        if (-pitch < pitchUpThreshold)
            pitch += 360f;
        targetInput.x = pitch;

        if (Vector3.Angle(Vector3.forward, errorDir) < fineSteeringAngle)
        {
            var yaw = Vector3.SignedAngle(Vector3.forward, yawError, Vector3.up);
            targetInput.y = yaw * yawfactor;
        }
        else
        {
            var roll = Vector3.SignedAngle(Vector3.up, rollError, Vector3.forward);
            targetInput.z = roll * rollFactor;
        }

        targetInput.x = Mathf.Clamp(targetInput.x, -1, 1);
        targetInput.y = Mathf.Clamp(targetInput.y, -1, 1);
        targetInput.z = Mathf.Clamp(targetInput.z, -1, 1);

        var input = Vector3.MoveTowards(lastInput, targetInput, steeringSpeed * dt);
        lastInput = input;

        float minAltitude = 500f;
        float altitudeError = minAltitude - plane.Rigidbody.position.y;
        if (altitudeError > 0)
            targetInput.x -= Mathf.Clamp(altitudeError / 100f, 0f, 1f);

        return input;
    }

    void SteerToTarget(float dt, Vector3 planePosition)
    {
        bool foundTarget = false;
        Vector3 targetPosition = Vector3.zero;
        Vector3 steering = Vector3.zero;

        float delay = reactionDelayMax;
        if (Vector3.Distance(planePosition, plane.Rigidbody.position) < reactionDelayDistance)
            delay = reactionDelayMin;

        while (inputQueue.Count > 0)
        {
            var input = inputQueue.Peek();
            if (input.time + delay <= Time.time)
            {
                targetPosition = input.targetPosition;
                inputQueue.Dequeue();
                foundTarget = true;
            }
            else break;
        }

        if (foundTarget)
            steering = CalculateSteering(dt, targetPosition);

        plane.SetControlInput(steering);
    }

    // Manewry awaryjne

    bool CheckGroundAvoidance()
    {
        var ray = new Ray(
            plane.Rigidbody.position,
            plane.Rigidbody.rotation * Quaternion.Euler(groundAvoidanceAngle, 0, 0) * Vector3.forward
        );

        bool hit = Physics.Raycast(ray, groundAvoidanceDistance + plane.LocalVelocity.z, groundLayer);
        return hit;
    }

    Vector3 AvoidGround()
    {
        var roll = plane.Rigidbody.rotation.eulerAngles.z;
        if (roll > 180f) roll -= 360f;
        var steering = new Vector3(-1f, 0f, Mathf.Clamp(-roll * rollFactor, -1f, 1f));
        return steering;
    }

    Vector3 RecoverSpeed()
    {
        var roll = plane.Rigidbody.rotation.eulerAngles.z;
        var pitch = plane.Rigidbody.rotation.eulerAngles.x;
        if (roll > 180f) roll -= 360f;
        if (pitch > 180f) pitch -= 360f;
        return new Vector3(Mathf.Clamp(-pitch, -1, 1), 0, Mathf.Clamp(-roll * rollFactor, -1, 1));
    }

    Vector3 GetDodgePosition(float dt, Missile missile)
    {
        dodgeTimer = Mathf.Max(0, dodgeTimer - dt);
        var missilePos = missile.transform.position;
        var dist = Mathf.Max(minDodgeDistance, Vector3.Distance(missilePos, plane.Rigidbody.position));

        if (dodgeTimer == 0)
        {
            var missileForward = missile.Rigidbody.rotation * Vector3.forward;
            dodgeOffsets.Clear();
            dodgeOffsets.Add(new Vector3(0, dist, 0));
            dodgeOffsets.Add(new Vector3(0, -dist, 0));
            dodgeOffsets.Add(Vector3.Cross(missileForward, Vector3.up) * dist);
            dodgeOffsets.Add(Vector3.Cross(missileForward, Vector3.up) * -dist);
            dodgeTimer = dodgeUpdateInterval;
        }

        float min = float.PositiveInfinity;
        Vector3 minDodge = missilePos + dodgeOffsets[0];

        foreach (var offset  in dodgeOffsets)
        {
            var dodgePos = missilePos + offset;
            float offsetDist = Vector3.Distance(dodgePos, lastDodgePoint);
            if (offsetDist < min)
            {
                minDodge = dodgePos;
                min = offsetDist;
            }
        }

        lastDodgePoint = minDodge;
        return minDodge;
    }

    // Patrol

    void GeneratePatrolPoint()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-patrolRadius, patrolRadius),
            Random.Range(patrolMinAltitude, patrolMaxAltitude),
            Random.Range(-patrolRadius, patrolRadius)
        );
        patrolPoint = transform.position + randomOffset;
        patrolPoint.y = Mathf.Clamp(patrolPoint.y, patrolMinAltitude, patrolMaxAltitude);
    }

    // Uzbrojenie

    Vector3 GetLeadPosition()
    {
        if (target == null) return plane.Rigidbody.position;

        Vector3 pos = target.Position;
        pos.y = Mathf.Max(pos.y, 500f);

        float dist = Vector3.Distance(plane.Rigidbody.position, pos);
        if (dist < cannonRange)
            pos = Utilities.FirstOrderIntercept(
                plane.Rigidbody.position,
                plane.Rigidbody.linearVelocity,
                bulletSpeed,
                pos,
                target.Velocity
            );

        return pos;
    }

    void UpdateWeapons(float dt)
    {
        if (target == null || !target.IsAlive)
        {
            gunSystem.SetFireInput(false);
            cannonFiring = false;
            return;
        }

        if (cannonFiring)
        {
            cannonBurstTimer = Mathf.Max(0, cannonBurstTimer - dt);
            if (cannonBurstTimer == 0)
            {
                cannonFiring = false;
                cannonCooldownTimer = cannonBurstCooldown;
                gunSystem.SetFireInput(false);
            }
        }
        else
        {
            cannonCooldownTimer = Mathf.Max(0, cannonCooldownTimer - dt);

            var leadPos = Utilities.FirstOrderIntercept(
                plane.Rigidbody.position,
                plane.Rigidbody.linearVelocity,
                bulletSpeed,
                target.Position,
                target.Velocity
            );

            float dist = Vector3.Distance(plane.Rigidbody.position, target.Position);
            var toTarget = (leadPos - plane.Rigidbody.position).normalized;
            float angle = Vector3.Angle(toTarget, plane.Rigidbody.rotation * Vector3.forward);

            if (dist < cannonRange && angle < cannonMaxAngle && cannonCooldownTimer == 0)
            {
                cannonFiring = true;
                cannonBurstTimer = cannonBurstLength;
                gunSystem.SetFireInput(true);
            }
        }
    }

    void UpdateMissiles(float dt)
    {
        missileCooldownTimer = Mathf.Max(0, missileCooldownTimer - dt);

        if (target == null || !target.IsAlive) return;
        if (missileCooldownTimer > 0) return;

        float dist = Vector3.Distance(plane.Rigidbody.position, target.Position);
        if (dist < missileMinRange || dist > missileMaxRange) return;

        var toTarget = (target.Position - plane.Rigidbody.position).normalized;
        float angle = Vector3.Angle(toTarget, plane.Rigidbody.rotation * Vector3.forward);
        if (angle > missileMaxAngle) return;

        if (!missileSystem.IsLocked)
        {
            missileSystem.SetFireInput(false);
            return;
        }

        missileLockTimer += dt;
        if (missileLockTimer >= missileLockDelay)
        {
            missileSystem.SetFireInput(true);
            missileCooldownTimer = missileCooldown;
            missileLockTimer = 0f;
        }
    }

    float CalculateThrottle(float minSpd, float maxSpd)
    {
        if (plane.LocalVelocity.z < minSpd) return 1f;
        if (plane.LocalVelocity.z > maxSpd) return -1f;
        return 0f;
    }
}
