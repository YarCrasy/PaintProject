using UnityEngine;

public class ShapeSelector : MonoBehaviour
{
    public void UpdateShape(int set)
    {
        ShapeManager.instance.currentShape = (ShapeType) set;
    }
}
