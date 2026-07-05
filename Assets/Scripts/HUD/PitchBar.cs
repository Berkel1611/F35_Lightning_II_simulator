using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PitchBar : MonoBehaviour
{
    [SerializeField]
    List<TextMeshProUGUI> texts;

    Image image;
    List<Transform> transforms;

    private void Start()
    {
        image = GetComponent<Image>();
        transforms = new List<Transform>();

        foreach (var text in texts)
        {
            transforms.Add(text.GetComponent<Transform>());
        }
    }

    public void SetNumber(int number)
    {
        foreach (var text in texts)
        {
            text.text = string.Format("{0}", number);
        }
    }

    public void UpdateRoll(float angle)
    {
        foreach (var transform in transforms)
        {
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }
    }

    public void UpdateColor(Color color)
    {
        image.color = color;

        foreach (var text in texts)
        {
            text.color = color;
        }
    }
}
