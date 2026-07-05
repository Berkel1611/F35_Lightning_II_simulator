using UnityEngine;

/// <summary>
/// Komponent rakiety AIM-120 / AIM-9X.
/// Naprowadzanie proporcjonalne (Proportional Navigation).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Missile : MonoBehaviour
{
    [Header("Napêd")]
    [SerializeField]
    float thrustForce = 15000f;
    [SerializeField]
    float burnTime = 4f;
    [SerializeField]
    ParticleSystem rocketTrail;
    [HideInInspector]
    public Vector3 initialVelocity;

    [Header("Naprowadzanie PN")]
    [SerializeField]
    float navigationConstant = 4f;
    [SerializeField]
    float maxG = 30f;
    [SerializeField]
    float seekerRange = 8000f;
    [SerializeField]
    float seekerFOV = 40f;

    [Header("Detonacja")]
    [SerializeField]
    float blastRadius = 15f;
    [SerializeField]
    float directDamage = 300f;
    [SerializeField]
    float splashDamage = 100f;
    [SerializeField]
    float selfDestructTime = 30f;
    [SerializeField]
    GameObject explosionEffectPrefab;

    // Stan wewnêtrzny
    Rigidbody rb;
    Transform target;
    AITarget targetAITarget;
    Vector3 previousTargetDirection;
    float burnTimer;
    float lifeTimer;
    bool armed = false;
    bool detonated = false;

    // OpóŸnienie uzbrojenia
    [SerializeField]
    float armDelay = 0.3f;
    float armTimer = 0f;

    public event System.Action OnDetonated;

    public Rigidbody Rigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        detonated = false;
        armed = false;
        armTimer = 0f;
        burnTimer = burnTime;
        lifeTimer = selfDestructTime;
        previousTargetDirection = Vector3.zero;
        if (rocketTrail != null)
            rocketTrail.Stop();
    }

    public void Launch(Vector3 launchVelocity, Transform lockedTarget)
    {
        rb.linearVelocity = launchVelocity;
        target = lockedTarget;
        previousTargetDirection = (target != null)
            ? (target.position - transform.position).normalized
            : transform.forward;

        if (target != null)
        {
            targetAITarget = target.GetComponent<AITarget>();
            if (targetAITarget != null)
                targetAITarget.NotifyMissileLaunched(this, true);
        }
    }

    private void FixedUpdate()
    {
        if (detonated) return;

        float dt = Time.fixedDeltaTime;
        lifeTimer -= dt;
        armTimer += dt;

        if (!armed && armTimer >= armDelay)
            armed = true;

        // Napêd
        if (burnTimer > 0f)
        {
            rb.AddForce(transform.forward * thrustForce);
            burnTimer -= dt;
            if (rocketTrail != null && !rocketTrail.isPlaying)
                rocketTrail.Play();
        }
        else if (rocketTrail != null && rocketTrail.isPlaying)
            rocketTrail.Stop();

        // Naprowadzanie
        if (target != null && armed)
            ApplyProportionalNavigation(dt);
        else rb.MoveRotation(Quaternion.LookRotation(rb.linearVelocity.normalized));

        // Samodestrukcja
        if (lifeTimer <= 0f)
            Detonate(transform.position);
    }

    void ApplyProportionalNavigation(float dt)
    {
        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        // SprawdŸ czy cel w zasiêgu i FOV
        if (distance > seekerRange)
        {
            target = null;
            return;
        }

        Vector3 targetDir = toTarget.normalized;
        float angle = Vector3.Angle(transform.forward, targetDir);
        if (angle > seekerFOV)
        {
            target = null;
            return;
        }

        // Szybkoœæ zamkniêcia
        Vector3 closingVelocity = rb.linearVelocity;
        float Vc = closingVelocity.magnitude;

        // LOS rate
        if (previousTargetDirection == Vector3.zero)
        {
            previousTargetDirection = targetDir;
            return;
        }

        Vector3 losRate = (targetDir - previousTargetDirection) / dt;
        previousTargetDirection = targetDir;

        // Polecenie przyspieszenia
        Vector3 accelerationCommand = navigationConstant * Vc * losRate;

        // Ogranicz do maxG
        float maxAccel = maxG * 9.81f;
        if (accelerationCommand.magnitude > maxAccel)
            accelerationCommand = accelerationCommand.normalized * maxAccel;

        rb.AddForce(accelerationCommand, ForceMode.Acceleration);

        // Obróæ rakietê w kierunku lotu
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(rb.linearVelocity.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, dt * 10f));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!armed) return;
        Detonate(collision.contacts[0].point);
    }

    void Detonate(Vector3 point)
    {
        if (detonated) return;
        detonated = true;

        // Powiadom cel o detonacji
        if (targetAITarget != null)
            targetAITarget.NotifyMissileLaunched(this, false);

        OnDetonated?.Invoke();

        // Efekt eksplozji
        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab, point, Quaternion.identity);
            fx.transform.localScale = Vector3.one * 3f;
        }

        // Splash damage
        Collider[] hits = Physics.OverlapSphere(point, blastRadius);
        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(point, hit.transform.position);
            float dmg = Mathf.Lerp(directDamage, splashDamage, dist / blastRadius);

            Plane plane = hit.GetComponent<Plane>();
            if (plane != null)
            {
                plane.ApplyDamage(dmg);
                continue;
            }

            GroundTarget gt = hit.GetComponent<GroundTarget>();
            if (gt != null)
                gt.ApplyDamage(dmg);
        }

        Destroy(gameObject);
    }
}
