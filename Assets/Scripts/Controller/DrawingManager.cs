using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    public static DrawingManager instance;

    [SerializeField] Material lineMaterial;
    public int drawingIdToLoad = 1;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        int drawingId = ShapeManager.instance != null ? ShapeManager.instance.currentDrawingId : 1;
        LoadDrawing(drawingId);
    }

    public void LoadDrawing(int drawingId)
    {
        List<Shape> shapes = LoadShapesFromDB(drawingId);
        foreach (var shape in shapes)
        {
            DrawShapeFromData(shape);
        }
    }

    private List<Shape> LoadShapesFromDB(int drawingId)
    {
        List<Shape> shapes = new();

        string dbPath = DBController.instance.GetDBPath();
        using var connection = new SqliteConnection(dbPath);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        // 1. Obtener todas las figuras del dibujo
        using (var shapeCmd = connection.CreateCommand())
        {
            shapeCmd.CommandText = @"
                SELECT id, type, color_r, color_g, color_b, thickness, drawing_id, fill_r, fill_g, fill_b, fill_a
                FROM Shapes
                WHERE drawing_id = @drawingId;
            ";
            shapeCmd.Parameters.AddWithValue("@drawingId", drawingId);

            using var reader = shapeCmd.ExecuteReader();
            while (reader.Read())
            {
                var shape = new Shape(Enum.Parse<ShapeType>(reader.GetString(1)))
                {
                    id = reader.GetInt32(0),
                    color = new Color(reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4)),
                    thickness = reader.GetFloat(5),
                    drawingId = reader.GetInt32(6),
                    points = new List<Vector2>(),
                    fillColor = new Color(
                        reader.GetFloat(7), // fill_r
                        reader.GetFloat(8), // fill_g
                        reader.GetFloat(9), // fill_b
                        reader.GetFloat(10) // fill_a
                    )
                };

                shapes.Add(shape);
            }
        }

        // 2. Obtener los puntos de cada figura
        foreach (var shape in shapes)
        {
            using var pointCmd = connection.CreateCommand();
            pointCmd.CommandText = @"
                SELECT x, y FROM Points
                WHERE shape_id = @shapeId
                ORDER BY point_index ASC;
            ";
            pointCmd.Parameters.AddWithValue("@shapeId", shape.id);

            using var pointReader = pointCmd.ExecuteReader();
            while (pointReader.Read())
            {
                float x = pointReader.GetFloat(0);
                float y = pointReader.GetFloat(1);
                shape.points.Add(new Vector2(x, y));
            }

            if (shape.points.Count > 0)
                shape.position = shape.points[0]; // usar el primer punto como posición central
        }

        return shapes;
    }

    private void DrawShapeFromData(Shape shape)
    {
        GameObject shapeObj = new("Shape")
        {
            tag = "Shape"
        };

        LineRenderer lr = shapeObj.AddComponent<LineRenderer>();
        lr.material = new Material(lineMaterial)
        {
            color = shape.color
        };
        lr.widthMultiplier = shape.thickness;

        switch (shape.type)
        {
            case ShapeType.Point:
                lr.positionCount = 2;
                Vector2 p = shape.points[0];
                lr.SetPosition(0, p + Vector2.left * 0.05f);
                lr.SetPosition(1, p - Vector2.left * 0.05f);
                break;

            case ShapeType.Line:
            case ShapeType.IrregularShape:
            case ShapeType.RegularShape:
                lr.positionCount = shape.points.Count;
                for (int i = 0; i < shape.points.Count; i++)
                    lr.SetPosition(i, shape.points[i]);
                if (shape.type != ShapeType.Line)
                    lr.loop = true;
                break;

            case ShapeType.Circle:
                // tratar igual que RegularShape con 60 lados
                lr.positionCount = 60 + 1;
                float r = Vector2.Distance(shape.points[0], shape.points[^1]);
                for (int i = 0; i <= 60; i++)
                {
                    float angle = i * 2 * Mathf.PI / 60;
                    Vector3 point = shape.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                    lr.SetPosition(i, point);
                }
                lr.loop = true;
                break;
        }

        // Relleno para figuras cerradas
        if ((shape.type == ShapeType.IrregularShape || shape.type == ShapeType.RegularShape || shape.type == ShapeType.Circle)
            && shape.points.Count >= 3)
        {
            List<Vector3> points3D = new();
            foreach (var pt in shape.points)
                points3D.Add(new(pt.x, pt.y, 0));
            CreatePolygonFill(points3D, shape.fillColor);
        }
    }

    private void CreatePolygonFill(List<Vector3> points, Color fillColor)
    {
        // No crear mesh si el color es completamente transparente
        if (points.Count < 3 || fillColor.a == 0f) return;

        GameObject fillObj = new("ShapeFill") { tag = "Shape" };
        MeshFilter mf = fillObj.AddComponent<MeshFilter>();
        MeshRenderer mr = fillObj.AddComponent<MeshRenderer>();
        // Usa el mismo material de línea para el relleno, o crea uno específico si lo prefieres
        mr.material = new Material(lineMaterial) { color = fillColor };

        Mesh mesh = new();

        // Proyección a 2D para triangulación
        Vector2[] verts2D = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++)
            verts2D[i] = new Vector2(points[i].x, points[i].y);

        // Triangulación simple (ear clipping)
        Triangulator triangulator = new Triangulator(verts2D);
        int[] indices = triangulator.Triangulate();

        Vector3[] verts3D = points.ToArray();

        mesh.vertices = verts3D;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }
}
