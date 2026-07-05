using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Komponent celu - powietrznego lub naziemnego.
/// Udostêpnia dane pozycji, prêdkoœci i przychodz¹cych rakiet dla AIController i HUD.
/// </summary>
public class AITarget : MonoBehaviour
{
    [SerializeField]
    new string name;

    public string Name => name;
    public Vector3 Position => rb.position;
    public Vector3 Velocity => rb.linearVelocity;
    public bool IsAlive => damageable != null && damageable.IsAlive;
    public List<Missile> IncomingMissiles => incomingMissiles;

    Rigidbody rb;
    IDamageable damageable;
    List<Missile> incomingMissiles = new List<Missile>();

    const float sortInterval = 0.5f;
    float sortTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        damageable = GetComponent<IDamageable>();
    }

    private void FixedUpdate()
    {
        sortTimer = Mathf.Max(0f, sortTimer - Time.fixedDeltaTime);
        if (sortTimer == 0)
        {
            SortIncomingMissiles();
            sortTimer = sortInterval;
        }
    }

    void SortIncomingMissiles()
    {
        if (incomingMissiles.Count == 0) return;

        var pos = Position;
        incomingMissiles.Sort((a, b) =>
        {
            float dA = Vector3.Distance(a.transform.position, pos);
            float dB = Vector3.Distance(b.transform.position, pos);
            return dA.CompareTo(dB);
        });
    }

    ///<summary>Zwraca najbli¿sz¹ nadlatuj¹c¹ rakietê lub null.</summary>
    public Missile GetIncomingMissile()
    {
        incomingMissiles.RemoveAll(m => m == null);
        return incomingMissiles.Count > 0 ? incomingMissiles[0] : null;
    }

    ///<summary>Wywo³ywane przez Missile.cs gdy rakieta jest odpalona lub eksploduje.</summary>
    public void NotifyMissileLaunched(Missile missile, bool active)
    {
        if (active)
        {
            if (!incomingMissiles.Contains(missile))
            {
                incomingMissiles.Add(missile);
                SortIncomingMissiles();
            }
        }
        else
        {
            incomingMissiles.Remove(missile);
        }
    }
}
