using UnityEngine;

/// <summary>
/// Komponent zdrowia dla celów naziemnych (czo³gi, ciê¿arówki, wie¿yczki).
/// </summary>
public class GroundTarget : MonoBehaviour, IDamageable
{
    [Header("Zdrowie")]
    [SerializeField]
    float maxHealth = 500f;
    [SerializeField]
    float damagedThreshold = 0.5f;

    [Header("Efekty")]
    [SerializeField]
    GameObject destructionEffect;
    [SerializeField]
    GameObject damagedEffect;
    [SerializeField]
    GameObject burningEffect;

    float currentHealth;
    bool destroyed = false;
    bool damaged = false;

    public bool IsAlive => !destroyed;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ApplyDamage(float damage)
    {
        if (destroyed) return;

        currentHealth -= damage;

        if (!damaged && currentHealth / maxHealth < damagedThreshold)
        {
            damaged = true;
            if (damagedEffect != null)
            {
                damagedEffect.SetActive(true);
                ParticleSystem ps = damagedEffect.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
            }
        }

        if (currentHealth <= 0f)
            DestroyTarget();
    }

    void DestroyTarget()
    {
        destroyed = true;

        if (destructionEffect != null)
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        if (burningEffect != null)
            Instantiate(burningEffect, transform.position, Quaternion.identity);

        gameObject.tag = "Untagged";
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Destroy(gameObject);
    }
}
