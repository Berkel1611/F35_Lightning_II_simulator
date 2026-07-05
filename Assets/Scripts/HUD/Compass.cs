using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    [SerializeField]
    GameObject tickLargePrefab;
    [SerializeField]
    GameObject tickSmallPrefab;
    [SerializeField]
    GameObject tickCardinalPrefab;
    [SerializeField]
    int largeTickInterval;
    [SerializeField]
    int smallTickInterval;

    struct Tick
    {
        public RectTransform transform;
        public GameObject gameObject;
        public Image image;
        public int angle;

        public Tick(RectTransform transform, GameObject gameObject, Image image, int angle)
        {
            this.transform = transform;
            this.gameObject = gameObject;
            this.image = image;
            this.angle = angle;
        }
    }

    // cardinal directions
    static readonly string[] directions = {
        "N",
        "NE",
        "E",
        "SE",
        "S",
        "SW",
        "W",
        "NW"
    };

    new RectTransform transform;
    List<Tick> ticks;
    List<TextMeshProUGUI> tickText;
    new Camera camera;
    Transform planeTransform;
    List<Graphic> graphics;

    private void Start()
    {
        transform = GetComponent<RectTransform>();
        ticks = new List<Tick>();
        tickText = new List<TextMeshProUGUI>();

        for (int i = 0; i < 360; i++)
        {
            if (i % largeTickInterval == 0)
                MakeLargeTick(i);
            else if (i % smallTickInterval == 0)
                MakeSmallTick(i);
        }
    }

    public void SetCamera(Camera camera)
    {
        this.camera = camera;
    }

    public void SetPlane(Plane plane)
    {
        planeTransform = plane.GetComponent<Transform>();
    }

    public void UpdateColor(Color color)
    {
        foreach (var tick in ticks)
            tick.image.color = color;
        foreach (var text in tickText)
            text.color = color;
    }

    void MakeLargeTick(int angle)
    {
        bool isCardinal = angle % 45 == 0;

        var tickGO = Instantiate(isCardinal ? tickCardinalPrefab : tickLargePrefab, transform);
        var tickTransform = tickGO.GetComponent<RectTransform>();
        var tickImage = tickGO.GetComponent<Image>();
        var text = tickGO.GetComponentInChildren<TextMeshProUGUI>();

        text.text = isCardinal ? directions[angle / 45] : string.Format("{0}°", angle);

        tickText.Add(text);
        ticks.Add(new Tick(tickTransform, tickGO, tickImage, angle));
    }

    void MakeSmallTick(int angle)
    {
        var tickGO = Instantiate(tickSmallPrefab, transform);
        var tickTransform = tickGO.GetComponent<RectTransform>();
        var tickImage = tickGO.GetComponent<Image>();

        ticks.Add(new Tick(tickTransform, tickGO, tickImage, angle));
    }

    float ConvertAngle(float angle)
    {
        if (angle > 180)
        {
            angle -= 360f;
        }

        return angle;
    }

    float GetPosition(float angle)
    {
        float fov = camera.fieldOfView;

        return Utilities.TransformAngle(angle, fov, camera.pixelHeight);
    }

    private void LateUpdate()
    {
        if (camera == null) return;

        float yaw = planeTransform.eulerAngles.y;
        Rect rect = transform.rect;

        foreach (var tick in ticks)
        {
            float angle = Mathf.DeltaAngle(yaw, tick.angle);
            float position = GetPosition(ConvertAngle(angle));

            if (Mathf.Abs(angle) < 90f && position >= rect.xMin && position <= rect.xMax)
            {
                // if tick position is within bounds
                var pos = tick.transform.localPosition;
                tick.transform.localPosition = new Vector3(position, pos.y, pos.z);
                tick.gameObject.SetActive(true);
            }
            else
            {
                tick.transform.gameObject.SetActive(false);
            }
        }
    }
}
