using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShapeManager : MonoBehaviour
{
    public static ShapeManager instance;

    [HideInInspector] public int currentDrawingId;

    ShapeType currentShape = ShapeType.Point;
    private Vector3 startPoint;
    private bool isDrawing = false;

    public Material lineMaterial;
    public Material fillMaterial;

    [SerializeField] ColorPicker borderPicker, fillPicker;

    private GameObject previewObject;
    private LineRenderer previewRenderer;

    int faceCount = 3;

    List<GameObject> shapes = new();
    List<Vector3> irregularPoints = new();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        currentDrawingId = DBController.instance.CreateNewDrawing();
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // --- Polígono Irregular ---
        if (currentShape == ShapeType.IrregularShape)
        {
            // Preview dinámico: actualiza la línea hasta el cursor
            if (irregularPoints.Count > 0)
            {
                Vector3 mousePos = GetWorldPoint();
                UpdateIrregularPreview(mousePos);
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 point = GetWorldPoint();

                // Si hay al menos 3 puntos y el clic es cerca del primero, intenta cerrar el polígono
                if (irregularPoints.Count >= 3 && Vector2.Distance(point, irregularPoints[0]) < 0.2f)
                {
                    // Comprobar si el cierre es válido
                    if (IsNewSegmentValid(irregularPoints, irregularPoints[0], true))
                    {
                        DrawIrregularShape(irregularPoints);
                        Destroy(previewObject);
                        previewObject = null;
                        previewRenderer = null;

                        // Guardar en base de datos
                        List<Vector2> shapePoints = new();
                        foreach (Vector3 pos in irregularPoints)
                            shapePoints.Add(new(pos.x, pos.y));

                        Shape shapeData = new(currentShape)
                        {
                            drawingId = currentDrawingId,
                            color = borderPicker.currentColor,
                            thickness = 0.075f,
                            position = shapePoints[0],
                            points = shapePoints
                        };

                        DBController.instance.InsertShape(
                            drawingId: shapeData.drawingId,
                            type: shapeData.type.ToString(),
                            color: shapeData.color,
                            thickness: shapeData.thickness,
                            points: shapeData.points,
                            fillColor: fillPicker.currentColor
                        );

                        irregularPoints.Clear();
                    }
                    else
                    {
                        Debug.LogWarning("No se puede cerrar el polígono: los lados se cruzan.");
                    }
                }
                // Si no, añadir un nuevo punto normalmente
                else
                {
                    if (IsNewSegmentValid(irregularPoints, point, false))
                    {
                        irregularPoints.Add(point);

                        if (previewObject == null)
                        {
                            previewObject = new("PreviewIrregular");
                            previewRenderer = previewObject.AddComponent<LineRenderer>();
                            previewRenderer.material = new Material(lineMaterial)
                            {
                                color = borderPicker.currentColor
                            };
                            previewRenderer.widthMultiplier = 0.075f;
                            previewRenderer.loop = false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No se puede añadir el punto: el segmento cruza otro lado.");
                    }
                }
            }

            if (Input.GetMouseButtonDown(1) && irregularPoints.Count > 2)
            {
                if (IsNewSegmentValid(irregularPoints, irregularPoints[0], true))
                {
                    DrawIrregularShape(irregularPoints);
                    Destroy(previewObject);
                    previewObject = null;
                    previewRenderer = null;

                    // Guardar en base de datos
                    List<Vector2> shapePoints = new();
                    foreach (Vector3 pos in irregularPoints)
                        shapePoints.Add(new(pos.x, pos.y));

                    Shape shapeData = new(currentShape)
                    {
                        drawingId = currentDrawingId,
                        color = borderPicker.currentColor,
                        thickness = 0.075f,
                        position = shapePoints[0],
                        points = shapePoints
                    };

                    DBController.instance.InsertShape(
                        drawingId: shapeData.drawingId,
                        type: shapeData.type.ToString(),
                        color: shapeData.color,
                        thickness: shapeData.thickness,
                        points: shapeData.points,
                        fillColor: fillPicker.currentColor
                    );

                    irregularPoints.Clear();
                }
                else
                {
                    Debug.LogWarning("No se puede cerrar el polígono: los lados se cruzan.");
                }
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            startPoint = GetWorldPoint();
            isDrawing = true;

            previewObject = new("PreviewShape");
            previewRenderer = previewObject.AddComponent<LineRenderer>();
            previewRenderer.material = new Material(lineMaterial)
            {
                color = borderPicker.currentColor
            };

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
                Vector3[] positions = new Vector3[previewRenderer.positionCount];
                previewRenderer.GetPositions(positions);

                List<Vector2> shapePoints = new();
                foreach (Vector3 pos in positions)
                    shapePoints.Add(new(pos.x, pos.y));

                Shape shapeData = new(currentShape)
                {
                    drawingId = currentDrawingId,
                    color = borderPicker.currentColor,
                    thickness = 0.075f,
                    position = new(startPoint.x, startPoint.y),
                    points = shapePoints
                };

                DBController.instance.InsertShape(
                    drawingId: shapeData.drawingId,
                    type: shapeData.type.ToString(),
                    color: shapeData.color,
                    thickness: shapeData.thickness,
                    points: shapeData.points,
                    fillColor: fillPicker.currentColor
                );
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
        ShapeCase(previewRenderer, p1, p2);
    }

    void UpdateIrregularPreview(Vector3? dynamicPoint = null)
    {
        if (previewRenderer == null) return;
        previewRenderer.positionCount = irregularPoints.Count + (dynamicPoint.HasValue ? 1 : 0);
        for (int i = 0; i < irregularPoints.Count; i++)
            previewRenderer.SetPosition(i, irregularPoints[i]);
        if (dynamicPoint.HasValue)
            previewRenderer.SetPosition(irregularPoints.Count, dynamicPoint.Value);
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
        GameObject shapeObj = new("Shape")
        {
            tag = "Shape",
        };
        LineRenderer lr = shapeObj.AddComponent<LineRenderer>();
        lr.material = new(lineMaterial)
        {
            color = borderPicker.currentColor
        };
        lr.widthMultiplier = 0.075f;
        lr.loop = currentShape == ShapeType.Circle;

        shapes.Add(shapeObj);

        ShapeCase(lr, p1, p2);
    }

    void DrawIrregularShape(List<Vector3> points)
    {
        // Relleno
        CreatePolygonFill(points, fillPicker.currentColor);

        // Borde
        GameObject shapeObj = new("Shape") { tag = "Shape" };
        LineRenderer lr = shapeObj.AddComponent<LineRenderer>();
        lr.material = new(lineMaterial) { color = borderPicker.currentColor };
        lr.widthMultiplier = 0.075f;
        lr.loop = true;
        lr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            lr.SetPosition(i, points[i]);
        shapes.Add(shapeObj);
    }

    void ShapeCase(LineRenderer lr, Vector3 p1, Vector3 p2)
    {
        switch (currentShape)
        {
            case ShapeType.Point:
                DrawPoint(lr, p1);
                break;
            case ShapeType.Line:
                DrawLine(lr, p1, p2);
                break;
            case ShapeType.Circle:
                if (fillPicker.currentColor.a == 0f)
                    DrawCircle(lr, p1, Vector3.Distance(p1, p2));
                else
                    DrawCircleFilled(p1, Vector3.Distance(p1, p2), 60, fillPicker.currentColor, borderPicker.currentColor, 0.075f);
                break;
            case ShapeType.RegularShape:
                if (fillPicker.currentColor.a == 0f)
                    DrawRegularShape(lr, p1, Vector3.Distance(p1, p2));
                else
                    DrawRegularShapeFilled(p1, Vector3.Distance(p1, p2), faceCount, fillPicker.currentColor, borderPicker.currentColor, 0.075f);
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
        if (p1 == p2)
        {
            DrawPoint(lr, p1);
            return;
        }
        lr.positionCount = 2;
        lr.SetPosition(0, p1);
        lr.SetPosition(1, p2);
    }

    void DrawRegularShape(LineRenderer lr, Vector3 center, float radius)
    {
        if (radius == 0)
        {
            DrawPoint(lr, center);
            return;
        }

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
        if (radius == 0)
        {
            DrawPoint(lr, center);
            return;
        }
        faceCount = 60; 
        DrawRegularShape(lr, center, radius);
    }

    void DrawCircleFilled(Vector3 center, float radius, int segments, Color fillColor, Color borderColor, float thickness)
    {
        if (radius <= 0) return;

        // --- Relleno (Mesh) ---
        List<Vector3> points = new();
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            points.Add(center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius);
        }
        CreatePolygonFill(points, fillColor);

        // --- Borde (LineRenderer) ---
        GameObject shapeObj = new("Shape") { tag = "Shape" };
        LineRenderer lr = shapeObj.AddComponent<LineRenderer>();
        lr.material = new(lineMaterial) { color = borderColor };
        lr.widthMultiplier = thickness;
        lr.loop = true;
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
            lr.SetPosition(i, points[i]);
        shapes.Add(shapeObj);
    }

    void DrawRegularShapeFilled(Vector3 center, float radius, int faces, Color fillColor, Color borderColor, float thickness)
    {
        if (radius <= 0 || faces < 3) return;

        // --- Relleno (Mesh) ---
        List<Vector3> points = new();
        for (int i = 0; i < faces; i++)
        {
            float angle = i * 2 * Mathf.PI / faces;
            points.Add(center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius);
        }
        CreatePolygonFill(points, fillColor);

        // --- Borde (LineRenderer) ---
        GameObject shapeObj = new("Shape") { tag = "Shape" };
        LineRenderer lr = shapeObj.AddComponent<LineRenderer>();
        lr.material = new(lineMaterial) { color = borderColor };
        lr.widthMultiplier = thickness;
        lr.loop = true;
        lr.positionCount = faces;
        for (int i = 0; i < faces; i++)
            lr.SetPosition(i, points[i]);
        shapes.Add(shapeObj);
    }

    void CreatePolygonFill(List<Vector3> points, Color fillColor)
    {
        if (points.Count < 3 || fillColor.a == 0f) return;

        GameObject fillObj = new("ShapeFill") { tag = "Shape" };
        MeshFilter mf = fillObj.AddComponent<MeshFilter>();
        MeshRenderer mr = fillObj.AddComponent<MeshRenderer>();

        // Crear material con transparencia
        Material mat = new Material(fillMaterial);
        mat.color = fillColor;
        if (mat.HasProperty("_Mode"))
            mat.SetFloat("_Mode", 3); // 3 = Transparent en Standard Shader
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        mr.material = mat;

        Mesh mesh = new Mesh();
        Vector2[] verts2D = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++)
            verts2D[i] = new Vector2(points[i].x, points[i].y);

        Triangulator triangulator = new Triangulator(verts2D);
        int[] indices = triangulator.Triangulate();

        Vector3[] verts3D = points.ToArray();

        mesh.vertices = verts3D;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }

    void CreatePolygonFillFromShape(Shape shape)
    {
        if ((shape.type == ShapeType.IrregularShape || shape.type == ShapeType.RegularShape || shape.type == ShapeType.Circle)
            && shape.points.Count >= 3)
        {
            List<Vector3> points3D = new();
            foreach (var pt in shape.points)
                points3D.Add(new(pt.x, pt.y, 0));
            CreatePolygonFill(points3D, shape.fillColor);
        }
    }

    // Devuelve true si los segmentos (p1,p2) y (q1,q2) se cruzan
    bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        float o1 = Orientation(p1, p2, q1);
        float o2 = Orientation(p1, p2, q2);
        float o3 = Orientation(q1, q2, p1);
        float o4 = Orientation(q1, q2, p2);

        if (o1 != o2 && o3 != o4)
            return true;

        return false;
    }

    // Ayuda: orientación de tres puntos
    float Orientation(Vector2 a, Vector2 b, Vector2 c)
    {
        float val = (b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y);
        if (Mathf.Approximately(val, 0)) return 0; // colineales
        return (val > 0) ? 1 : 2; // horario o antihorario
    }

    // Verifica si el nuevo segmento es válido (no cruza lados existentes)
    bool IsNewSegmentValid(List<Vector3> points, Vector3 newPoint, bool closing)
    {
        int n = points.Count;
        if (n < 2) return true;

        Vector2 newStart, newEnd;
        if (closing)
        {
            newStart = points[n - 1];
            newEnd = points[0];
        }
        else
        {
            newStart = points[n - 1];
            newEnd = newPoint;
        }

        for (int i = 0; i < n - 1; i++)
        {
            // Ignora el segmento adyacente al nuevo
            if (!closing && i == n - 2) continue;
            // Al cerrar, ignora el primer y último segmento
            if (closing && (i == 0 || i == n - 2)) continue;

            Vector2 segStart = points[i];
            Vector2 segEnd = points[i + 1];

            if (SegmentsIntersect(newStart, newEnd, segStart, segEnd))
                return false;
        }
        return true;
    }

    public void ClearScreen()
    {
        // Borra el dibujo actual de la base de datos (incluye Shapes y Points por ON DELETE CASCADE)
        DBController.instance.DeleteDrawing(currentDrawingId);

        // Limpia la escena
        foreach (var obj in shapes)
            if (obj != null) Destroy(obj);
        shapes.Clear();

        var fills = GameObject.FindGameObjectsWithTag("ShapeFill");
        foreach (var fill in fills)
            Destroy(fill);

        irregularPoints.Clear();
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            previewRenderer = null;
        }

        currentDrawingId = DBController.instance.CreateNewDrawing();
    }

    public void ClearAndSave()
    {
        // Ya se guarda automáticamente al dibujar
        // Limpia la escena
        foreach (var obj in shapes)
            if (obj != null) Destroy(obj);
        shapes.Clear();

        var fills = GameObject.FindGameObjectsWithTag("ShapeFill");
        foreach (var fill in fills)
            Destroy(fill);

        irregularPoints.Clear();
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            previewRenderer = null;
        }

        currentDrawingId = DBController.instance.CreateNewDrawing();
    }

}
