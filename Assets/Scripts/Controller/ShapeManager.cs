using UnityEngine;
using UnityEngine.EventSystems;

public enum ShapeType { Point, Line, Circle, RegularShape, IrregularShape, }

public class ShapeManager : MonoBehaviour
{
    public static ShapeManager instance;

    ShapeType currentShape = ShapeType.Point;
    private Vector3 startPoint;
    private bool isDrawing = false;

    public Material lineMaterial;

    private GameObject previewObject;
    private LineRenderer previewRenderer;

    int faceCount = 3;

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
            previewRenderer.widthMultiplier = 0.075f;
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
            case ShapeType.Point:
                DrawPoint(previewRenderer, p1);
                break;
            case ShapeType.Line:
                DrawLine(previewRenderer, p1, p2);
                break;

            case ShapeType.Circle:
                DrawCircle(previewRenderer, p1, Vector3.Distance(p1, p2));
                break;

            case ShapeType.RegularShape:
                DrawRegularShape(previewRenderer, p1, Vector3.Distance(p1, p2));
                break;
        }
    }

    public void SetShape(int set)
    {
        currentShape = (ShapeType) set;
        if (previewObject != null)
        {
            Destroy(previewObject);
            isDrawing = false;
        }
    }

    public void SetFaceCount(int set)
    {
        faceCount = set;
        if (faceCount < 3) faceCount = 3;
    }

    void DrawShape(Vector3 p1, Vector3 p2)
    {
        GameObject shapeObj = new("Shape");
        LineRenderer lr = shapeObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.widthMultiplier = 0.075f;
        lr.loop = currentShape == ShapeType.Circle;

        switch (currentShape)
        {
            case ShapeType.Point:
                DrawPoint(lr, p1);
                break;
            case ShapeType.Line:
                DrawLine(lr, p1, p2);
                break;

            case ShapeType.Circle:
                DrawCircle(lr, p1, Vector3.Distance(p1, p2));
                break;

            case ShapeType.RegularShape:
                DrawRegularShape(lr, p1, Vector3.Distance(p1, p2));
                break;
        }
    }

    void DrawPoint(LineRenderer lr, Vector3 pos)
    {
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = 0.10f;
        DrawLine(lr, pos + Vector3.left*0.05f, pos - Vector3.left*0.05f);
    }

    void DrawLine(LineRenderer lr, Vector3 p1, Vector3 p2)
    {
        lr.positionCount = 2;
        lr.SetPosition(0, p1);
        lr.SetPosition(1, p2);
    }

    void DrawRegularShape(LineRenderer lr, Vector3 center, float radius)
    {
        lr.positionCount = faceCount + 1;
        for (int i = 0; i <= faceCount; i++)
        {
            float angle = i * 2 * Mathf.PI / faceCount;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            lr.SetPosition(i, point);
        }
    }

    void DrawCircle(LineRenderer lr, Vector3 center, float radius)
    {
        faceCount = 60; 
        DrawRegularShape(lr, center, radius);
    }
}
