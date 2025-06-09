using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    public static DrawingManager instance;

    [SerializeField] private Material lineMaterial;
    public int drawingIdToLoad = 1;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadDrawing(drawingIdToLoad);
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
                SELECT id, type, color_r, color_g, color_b, thickness, drawing_id
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
                    points = new List<Vector2>()
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
        GameObject shapeObj = new("Shape");
        shapeObj.tag = "Shape";

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
    }
}
