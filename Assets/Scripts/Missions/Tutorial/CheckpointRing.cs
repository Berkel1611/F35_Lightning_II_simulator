using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reprezentuje pierœcieñ kontrolny w misji, który gracz musi przelecieæ, aby zaliczyæ kolejny etap. Odpowiada za wykrywanie kolizji z graczem, zmianê swojego stanu (aktywny/pokonany) oraz wywo³ywanie zdarzenia po przejœciu przez pierœcieñ. Mo¿e byæ u¿ywany do tworzenia sekwencji pierœcieni, które gracz musi pokonaæ w okreœlonej kolejnoœci.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class CheckpointRing : MonoBehaviour
{
    [Header("Wygl¹d")]
    [SerializeField]
    Renderer ringRenderer;
    [SerializeField]
    Color activeColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField]
    Color previewColor = new Color(0.2f, 0.6f, 1f, 0.5f);

    [Header("Zdarzenie")]
    public UnityEvent OnRingPassed;

    public bool IsPassed { get; private set; } = false;
    public bool IsActive { get; private set; } = false;

    SphereCollider trigger;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        SetActive(false);
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        trigger.enabled = active;

        if (ringRenderer != null)
        {
            ringRenderer.enabled = active;
            if (active) SetColor(activeColor);
        }
    }

    public void SetPreview(bool preview)
    {
        if (IsPassed) return;
        trigger.enabled = IsActive;

        if (ringRenderer != null)
        {
            ringRenderer.enabled = preview;
            if (preview && !IsActive) SetColor(previewColor);
        }
    }

    public void Hide()
    {
        IsActive = false;
        trigger.enabled = false;
        if (ringRenderer != null) ringRenderer.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive || IsPassed) return;
        if (!other.CompareTag("Player")) return;

        Pass();
    }
    void Pass()
    {
        IsPassed = true;
        Hide();
        OnRingPassed?.Invoke();
    }

    void SetColor(Color color)
    {
        if (ringRenderer == null) return;
        ringRenderer.material.SetColor("_UnlitColor", color);
    }

    // Gizmo w edytorze
    private void OnDrawGizmos()
    {
        Gizmos.color = IsPassed ? Color.green : (IsActive ? Color.cyan : Color.gray);
        Gizmos.DrawWireSphere(transform.position, GetComponent<SphereCollider>()?.radius ?? 50f);
    }
}
