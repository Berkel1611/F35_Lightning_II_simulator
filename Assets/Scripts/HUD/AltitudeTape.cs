using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AltitudeTape : MonoBehaviour
{
    [SerializeField]
    GameObject tickPrefab;
    [SerializeField]
    GameObject tickLargePrefab;
    [SerializeField]
    int tickInterval = 100;
    [SerializeField]
    int largeTickInterval = 500;
    [SerializeField]
    int range = 35000;
    [SerializeField]
    float pixelsPerUnit = 1f;
    [SerializeField]
    RectTransform numberContainer;
    [SerializeField]
    TextMeshProUGUI currentValue;

    struct Tick
    {
        public RectTransform transform;
        public GameObject gameObject;
        public int value;

        public Tick(RectTransform transform, GameObject gameObject, int value)
        {
            this.transform = transform;
            this.gameObject = gameObject;
            this.value = value;
        }
    }

    new RectTransform transform;
    List<Tick> ticks;
    Plane plane;

    private void Start()
    {
        transform = GetComponent<RectTransform>();
        ticks = new List<Tick>();

        for (int i = 0; i <= range; i += tickInterval)
        {
            bool isLarge = i % largeTickInterval == 0;
            MakeTick(i, isLarge ? tickLargePrefab : tickPrefab);
        }
    }

    public void SetPlane(Plane plane)
    {
        this.plane = plane;
    }

    void MakeTick(int value, GameObject prefab)
    {
        var tickGO = Instantiate(prefab, numberContainer);
        var tickTransform = tickGO.GetComponent<RectTransform>();
        var text = tickGO.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
            text.text = string.Format("{0:N0}", value);

        ticks.Add(new Tick(tickTransform, tickGO, value));
    }

    float GetPosition(float altitude, int tickValue)
    {
        return (tickValue - altitude) * pixelsPerUnit;
    }

    private void LateUpdate()
    {
        if (plane == null) return;
        float altitude = plane.AltitudeFeet;

        if (currentValue != null)
        {
            float rounded = Mathf.Round(altitude / 10f) * 10f;
            currentValue.text = string.Format("{0:N0}", rounded);
        }

        Rect rect = transform.rect;

        foreach (var tick in ticks)
        {
            float position = GetPosition(altitude, tick.value);

            if (position >= rect.yMin && position <= rect.yMax)
            {
                var pos = tick.transform.localPosition;
                tick.transform.localPosition = new Vector3(pos.x, position, pos.z);
                tick.gameObject.SetActive(true);
            }
            else
            {
                tick.gameObject.SetActive(false);
            }
        }
    }
}
