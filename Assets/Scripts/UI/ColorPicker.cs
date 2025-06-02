using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    [SerializeField] Image colorDisplayer;
    [SerializeField] Slider redSlider, greenSlider, blueSlider;

    [HideInInspector] public Color currentColor;

    private void Awake()
    {
        UpdateColorDisplay();
    }

    public void UpdateColorDisplay()
    {
        currentColor = new Color(redSlider.value / 255, greenSlider.value / 255, blueSlider.value / 255);
        colorDisplayer.color = currentColor;
        Debug.Log("actual color: " + currentColor);
    }

}
