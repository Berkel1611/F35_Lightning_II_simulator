using System;
using UnityEngine;

/// <summary>
/// Odpowiada za strefê l¹dowania w samouczku. Gracz musi wejœæ do tej strefy i utrzymaæ prêdkoœæ poni¿ej okreœlonego progu przez okreœlony czas, aby zaliczyæ l¹dowanie.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class LandingZone : MonoBehaviour
{
    [Header("Parametry l¹dowania")]
    [SerializeField]
    float landingSpeedThreshold = 5f;
    [SerializeField]
    float landingHoldTime = 2f;

    [Header("Wygl¹d")]
    [SerializeField]
    Renderer zoneRenderer;
    [SerializeField]
    Color activeColor = new Color(0f, 1f, 0.4f, 0.25f);

    public event Action OnLanded;

    bool isActive = false;
    bool playerInside = false;
    float holdTimer = 0f;
    bool landed = false;

    Plane playerPlane;

    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;

        SetRendererVisible(false);
    }

    public void Activate()
    {
        isActive = true;
        SetRendererVisible(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || landed) return;
        if (!other.CompareTag("Player")) return;

        playerPlane = other.GetComponentInParent<Plane>();
        playerInside = true;
        holdTimer = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        holdTimer = 0f;
    }

    private void Update()
    {
        if (!isActive || landed || !playerInside || playerPlane == null) return;

        float speed = playerPlane.LocalVelocity.magnitude;

        if (speed < landingSpeedThreshold)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= landingHoldTime)
            {
                landed = true;
                OnLanded?.Invoke();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void SetRendererVisible(bool visible)
    {
        if (zoneRenderer == null) return;
        zoneRenderer.enabled = visible;

        if (visible)
            zoneRenderer.material.SetColor("_UnlitColor", activeColor);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;
        Gizmos.color = isActive ? new Color(0f, 1f, 0.4f, 0.5f) : new Color(1f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
    }
}
