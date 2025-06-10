using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    [SerializeField] Image colorDisplayer;
    [SerializeField] Slider redSlider, greenSlider, blueSlider;
    [SerializeField] Toggle transparentToggle; // Asigna este Toggle en el inspector

    [HideInInspector] public Color currentColor;

    private void Awake()
    {
        UpdateColorDisplay();
        if (transparentToggle != null)
            transparentToggle.onValueChanged.AddListener(SetTransparency);
    }

    public void UpdateColorDisplay()
    {
        float alpha = (transparentToggle != null && transparentToggle.isOn) ? 0f : 1f;
        currentColor = new Color(redSlider.value / 255f, greenSlider.value / 255f, blueSlider.value / 255f, alpha);
        colorDisplayer.color = currentColor;
    }

    // Este método es compatible con UnityEvent<bool>
    public void SetTransparency(bool set)
    {
        UpdateColorDisplay();
    }
}
