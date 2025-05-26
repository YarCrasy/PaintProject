using UnityEngine;

public class ShapeSelector : MonoBehaviour
{
    [SerializeField] GameObject faceCount;

    public void ChangeShape(int set)
    {
        ShapeManager.instance.SetShape(set);
        faceCount.SetActive(set == (int)ShapeType.RegularShape);
    }
}
