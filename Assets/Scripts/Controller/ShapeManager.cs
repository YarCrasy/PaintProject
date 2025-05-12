using UnityEngine;
using UnityEngine.EventSystems;

public enum ShapeType { Point, Line, Circle, RegularShape, }

public class ShapeManager : MonoBehaviour
{
    public static ShapeManager instance;

    public ShapeType currentShape = ShapeType.Line;
    private Vector3 startPoint;
    private bool isDrawing = false;

    public Material lineMaterial;

    private GameObject previewObject;
    private LineRenderer previewRenderer;

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            startPoint = GetWorldPoint();
            isDrawing = true;

            previewObject = new("PreviewShape");
            previewRenderer = previewObject.AddComponent<LineRenderer>();
            previewRenderer.material = lineMaterial;
            previewRenderer.startWidth = previewRenderer.endWidth = 0.05f;
            previewRenderer.loop = currentShape == ShapeType.Circle;
        }

        if (isDrawing)
        {
            if (Input.GetMouseButton(0))
            {
                Vector3 currentPoint = GetWorldPoint();
                UpdatePreviewShape(startPoint, currentPoint);
            }

            if (Input.GetMouseButtonUp(0))
            {
                Vector3 endPoint = GetWorldPoint();
                DrawShape(startPoint, endPoint);
                Destroy(previewObject);
                isDrawing = false;

                // Guardar los datos aqui
            }
        }
    }

    Vector3 GetWorldPoint()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = 10;
        return Camera.main.ScreenToWorldPoint(mouse);
    }

    void UpdatePreviewShape(Vector3 p1, Vector3 p2)
    {
        switch (currentShape)
        {
            case ShapeType.Line:
                previewRenderer.positionCount = 2;
                previewRenderer.SetPosition(0, p1);
                previewRenderer.SetPosition(1, p2);
                break;

            case ShapeType.Circle:
                DrawCircle(previewRenderer, p1, Vector3.Distance(p1, p2));
                break;
        }
    }

    void DrawShape(Vector3 p1, Vector3 p2)
    {
        GameObject shapeObj = new("Shape");
        LineRenderer lr = shapeObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = lr.endWidth = 0.05f;
        lr.loop = currentShape == ShapeType.Circle;

        switch (currentShape)
        {
            case ShapeType.Line:
                lr.positionCount = 2;
                lr.SetPosition(0, p1);
                lr.SetPosition(1, p2);
                break;

            case ShapeType.Circle:
                DrawCircle(lr, p1, Vector3.Distance(p1, p2));
                break;
        }
    }

    void DrawCircle(LineRenderer lr, Vector3 center, float radius)
    {
        int segments = 60;
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            lr.SetPosition(i, point);
        }
    }
}
