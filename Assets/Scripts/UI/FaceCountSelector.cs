using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FaceCountSelector : MonoBehaviour
{
    [SerializeField] Slider faceCountSlider;
    [SerializeField] TextMeshProUGUI faceCountDisplay;

    private void Start()
    {
        SetFace();
    }

    public void SetFace()
    {
        float count = faceCountSlider.value;
        faceCountDisplay.text = count.ToString("0");
        ShapeManager.instance.SetFaceCount((int)count);
    }

}
