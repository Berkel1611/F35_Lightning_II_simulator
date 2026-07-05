using UnityEngine;

/// <summary>
/// Komponent pocisku GAU-22/A.
/// Obs³uguje zwrot do poola po czasie i detekcjê trafienia.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [SerializeField] 
    float damage = 25f;
    [SerializeField] 
    GameObject impactEffectPrefab;
    [SerializeField] 
    LayerMask collisionMask;
    [SerializeField] 
    float sphereCastRadius = 0.05f;

    [Header("Tracer")]
    [SerializeField] 
    LineRenderer tracerLine;
    [SerializeField] 
    Material tracerMaterial;
    [SerializeField] 
    float tracerWidth = 0.02f;

    [Header("Audio")]
    [SerializeField] 
    AudioClip impactSound;
    [SerializeField] 
    float impactVolume = 1f;

    float returnTimer = 0f;
    bool scheduled = false;
    Vector3 startPosition;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (tracerLine == null)
        {
            tracerLine = gameObject.AddComponent<LineRenderer>();
            tracerLine.positionCount = 2;
            tracerLine.startWidth = tracerWidth;
            tracerLine.endWidth = tracerWidth/2f;
            tracerLine.useWorldSpace = true;
            tracerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (tracerMaterial != null)
                tracerLine.material = tracerMaterial;
        }
    }

    private void OnEnable()
    {
        scheduled = false;
        returnTimer = 0f;
        startPosition = transform.position;

        if (tracerLine != null)
        {
            tracerLine.SetPosition(0, startPosition);
            tracerLine.SetPosition(1, startPosition);
            tracerLine.enabled = true;
        }
    }

    private void Update()
    {
        if (tracerLine != null && tracerLine.enabled)
        {
            Vector3 direction = rb.linearVelocity.normalized;
            tracerLine.SetPosition(0, transform.position - direction * 25f);
            tracerLine.SetPosition(1, transform.position);
        }

        if (!scheduled) return;
        returnTimer -= Time.deltaTime;
        if (returnTimer <= 0f)
            ReturnToPool();
    }

    private void FixedUpdate()
    {
        // SphereCast miêdzy poprzedni¹ a aktualn¹ pozycj¹
        Vector3 velocity = rb.linearVelocity;
        float distance = velocity.magnitude * Time.fixedDeltaTime;

        if (distance > 0.001f)
        {
            Ray ray = new Ray(transform.position, velocity.normalized);
            if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, distance, collisionMask))
            {
                // Efekt trafienia
                if (impactEffectPrefab != null)
                    Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));

                if (impactSound != null)
                    AudioSource.PlayClipAtPoint(impactSound, hit.point, impactVolume);

                // Damage
                Plane target = hit.collider.GetComponent<Plane>();
                if (target != null)
                    target.ApplyDamage(damage);

                GroundTarget gt = hit.collider.GetComponent<GroundTarget>();
                if (gt != null)
                    gt.ApplyDamage(damage);

                ReturnToPool();
            }
        }
    }

    /// <summary>
    /// Zaplanuj automatyczny zwrot poola po [lifetime] sekundach.
    /// </summary>
    public void ScheduleReturn(float lifetime)
    {
        returnTimer = lifetime;
        scheduled = true;
    }

    void ReturnToPool()
    {
        scheduled = false;
        if (tracerLine != null) 
            tracerLine.enabled = false;
        gameObject.SetActive(false);
    }
}
