using UnityEngine;

/// <summary>
/// Komponent bomby GBU-31 JDAM.
/// Lot balistyczny z grawitacj¹ + lekkie naprowadzanie GPS.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Bomb : MonoBehaviour
{
    [Header("Detonacja")]
    [SerializeField]
    float blastRadius = 25f;
    [SerializeField]
    float directDamage = 1000f;
    [SerializeField]
    float splashDamage = 200f;
    [SerializeField]
    GameObject explosionEffectPrefab;

    [Header("Naprowadzanie JDAM")]
    [SerializeField]
    bool guidanceEnabled = true;
    [SerializeField]
    float guidanceForce = 500f;

    Rigidbody rb;
    Vector3 targetPoint;
    bool hasTarget = false;
    bool detonated = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>Inicjalizacja przez BombSystem po zrzucie.</summary>
    public void Launch(Vector3 launchVelocity)
    {
        rb.linearVelocity = launchVelocity;
        rb.useGravity = true;

        rb.AddForce(Vector3.down * 1000f, ForceMode.Impulse);

        // Wyznacz punkt docelowy
        if (guidanceEnabled)
            targetPoint = PredictImpactPoint(launchVelocity);

        hasTarget = guidanceEnabled && targetPoint != Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (detonated) return;
        if (!hasTarget) return;

        // Lekka korekta w kierunku celu
        Vector3 toTarget = (targetPoint - transform.position).normalized;
        rb.AddForce(toTarget * guidanceForce);
    }

    /// <summary>Przybli¿one obliczenie punktu uderzenia na podstawie trajektorii balistycznej.</summary>
    Vector3 PredictImpactPoint(Vector3 velocity)
    {
        float h = transform.position.y;
        float vy = velocity.y;
        float g = Mathf.Abs(Physics.gravity.y);

        float discriminant = vy * vy + 2f * g * h;
        if (discriminant < 0) return Vector3.zero;

        float t = (-vy + Mathf.Sqrt(discriminant)) / g;

        return new Vector3(
            transform.position.x + velocity.x * t,
            0f,
            transform.position.z + velocity.z * t
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        Detonate(collision.contacts[0].point);
    }

    void Detonate(Vector3 point)
    {
        if (detonated) return;
        detonated = true;

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
