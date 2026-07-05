using UnityEngine;

/// <summary>
/// Centralny system wyboru celu dla gracza.
/// Jedno Ÿród³o prawdy dla innych systemów (HUD, PCD, MissileSystem) o aktualnym celu i statusie lock-on.
/// Selekcja automatyczna - wybiera obiekt z tagiem Enemy najbli¿szy osi dzioba samolotu, w zasiêgi detectionRange.
/// </summary>
public class TargetingSystem : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    Transform planeTransform;

    [Header("Detekcja")]
    [SerializeField]
    string targetTag = "Enemy";
    [SerializeField]
    float detectionRange = 10000f;
    [SerializeField]
    float detectionFov = 120f;
    [SerializeField]
    float updateInterval = 0.5f;

    // API publiczne

    public AITarget SelectedTarget { get; private set; }
    public bool HasTarget => SelectedTarget != null;
    public Vector3 TargetPosition => HasTarget ? SelectedTarget.Position : Vector3.zero;
    public Vector3 TargetVelocity => HasTarget ? SelectedTarget.Velocity : Vector3.zero;

    // Stan wewnêtrzny

    float updateTimer = 0f;

    // Unity lifecycle

    private void Awake()
    {
        if (planeTransform == null)
            planeTransform = transform;
    }

    private void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer > 0f) return;

        updateTimer = updateInterval;
        UpdateTarget();
    }

    // Logika selekcji

    void UpdateTarget()
    {
        if (SelectedTarget != null && !SelectedTarget.IsAlive)
            SelectedTarget = null;
        
        AITarget best = FindBestTarget();
        if (best != null)
            SelectedTarget = best;

    }

    AITarget FindBestTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);

        AITarget best = null;
        float bestAngle = detectionFov * 0.5f;

        foreach (var enemy in enemies)
        {
            AITarget candidate = enemy.GetComponent<AITarget>();
            if (candidate == null) continue;
            if (!candidate.IsAlive) continue;

            Vector3 toEnemy = candidate.Position - planeTransform.position;
            float dist = toEnemy.magnitude;
            if (dist > detectionRange) continue;

            float angle = Vector3.Angle(planeTransform.forward, toEnemy);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = candidate;
            }
        }

        return best;
    }
}
