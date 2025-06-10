using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mono.Data.Sqlite;

public class DBController : MonoBehaviour
{
    public static DBController instance;
    private string dbPath;
    private string filePath;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        filePath = Application.persistentDataPath + "/paint.db";
        dbPath = "URI=file:" + filePath;

        if (!File.Exists(filePath)) CreateDB();
    }

    public string GetDBPath() => dbPath;

    private void CreateDB()
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Drawings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT,
                created_at TEXT DEFAULT (datetime('now')),
                updated_at TEXT DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS Shapes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                drawing_id INTEGER NOT NULL,
                type TEXT NOT NULL,
                color_r REAL NOT NULL,
                color_g REAL NOT NULL,
                color_b REAL NOT NULL,
                thickness REAL NOT NULL,
                fill_r REAL,
                fill_g REAL,
                fill_b REAL,
                fill_a REAL,
                FOREIGN KEY (drawing_id) REFERENCES Drawings(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Points (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                shape_id INTEGER NOT NULL,
                point_index INTEGER NOT NULL,
                x REAL NOT NULL,
                y REAL NOT NULL,
                FOREIGN KEY (shape_id) REFERENCES Shapes(id) ON DELETE CASCADE
            );
        ";
        command.ExecuteNonQuery();
    }

    public int CreateNewDrawing(string name = "Nuevo dibujo")
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Drawings (name) VALUES (@name);
            SELECT last_insert_rowid();
        ";
        cmd.Parameters.AddWithValue("@name", name);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void InsertShape(int drawingId, string type, Color color, float thickness, List<Vector2> points, Color fillColor)
    {
        using var connection = new SqliteConnection(GetDBPath());
        connection.Open();

        if (fillColor == null) {
            fillColor = new Color(0, 0, 0, 0); // Default transparent fill color
        }

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        using var transaction = connection.BeginTransaction();
        try
        {
            int shapeId;

            using (var shapeCmd = connection.CreateCommand())
            {
                shapeCmd.CommandText = @"
                    INSERT INTO Shapes (drawing_id, type, color_r, color_g, color_b, thickness, fill_r, fill_g, fill_b, fill_a)
                    VALUES (@drawingId, @type, @r, @g, @b, @thickness, @fill_r, @fill_g, @fill_b, @fill_a);
                    SELECT last_insert_rowid();
                ";
                shapeCmd.Parameters.AddWithValue("@drawingId", drawingId);
                shapeCmd.Parameters.AddWithValue("@type", type);
                shapeCmd.Parameters.AddWithValue("@r", color.r);
                shapeCmd.Parameters.AddWithValue("@g", color.g);
                shapeCmd.Parameters.AddWithValue("@b", color.b);
                shapeCmd.Parameters.AddWithValue("@thickness", thickness);
                shapeCmd.Parameters.AddWithValue("@fill_r", fillColor.r);
                shapeCmd.Parameters.AddWithValue("@fill_g", fillColor.g);
                shapeCmd.Parameters.AddWithValue("@fill_b", fillColor.b);
                shapeCmd.Parameters.AddWithValue("@fill_a", fillColor.a);

                shapeId = int.Parse(shapeCmd.ExecuteScalar().ToString());
            }

            using (var pointCmd = connection.CreateCommand())
            {
                pointCmd.CommandText = @"
                    INSERT INTO Points (shape_id, point_index, x, y)
                    VALUES (@shapeId, @pointIndex, @x, @y);
                ";

                var shapeIdParam = pointCmd.CreateParameter(); shapeIdParam.ParameterName = "@shapeId";
                var indexParam = pointCmd.CreateParameter(); indexParam.ParameterName = "@pointIndex";
                var xParam = pointCmd.CreateParameter(); xParam.ParameterName = "@x";
                var yParam = pointCmd.CreateParameter(); yParam.ParameterName = "@y";

                pointCmd.Parameters.Add(shapeIdParam);
                pointCmd.Parameters.Add(indexParam);
                pointCmd.Parameters.Add(xParam);
                pointCmd.Parameters.Add(yParam);

                for (int i = 0; i < points.Count; i++)
                {
                    shapeIdParam.Value = shapeId;
                    indexParam.Value = i;
                    xParam.Value = points[i].x;
                    yParam.Value = points[i].y;
                    pointCmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
        catch (Exception e)
        {
            transaction.Rollback();
            Debug.LogError("DB InsertShape error: " + e.Message);
        }
    }

    public List<Shape> LoadShapes(int drawingId)
    {
        List<Shape> shapes = new();

        using var connection = new SqliteConnection(dbPath);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        using (var shapeCmd = connection.CreateCommand())
        {
            shapeCmd.CommandText = @"
                SELECT id, type, color_r, color_g, color_b, thickness, fill_r, fill_g, fill_b, fill_a
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
                    fillColor = new Color(reader.GetFloat(6), reader.GetFloat(7), reader.GetFloat(8), reader.GetFloat(9)),
                    drawingId = drawingId
                };
                shapes.Add(shape);
            }
        }

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
                shape.points.Add(new Vector2(pointReader.GetFloat(0), pointReader.GetFloat(1)));
            }

            if (shape.points.Count > 0)
                shape.position = shape.points[0];
        }

        return shapes;
    }

    public void UpdateDrawingName(int drawingId, string newName)
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Drawings
            SET name = @name, updated_at = datetime('now')
            WHERE id = @id;
        ";
        cmd.Parameters.AddWithValue("@name", newName);
        cmd.Parameters.AddWithValue("@id", drawingId);
        cmd.ExecuteNonQuery();
    }

    public List<Drawing> GetAllDrawings()
    {
        List<Drawing> drawings = new();
        using var connection = new SqliteConnection(dbPath);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, created_at, updated_at FROM Drawings ORDER BY created_at DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            drawings.Add(new Drawing
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                createdAt = reader.GetString(2),
                updatedAt = reader.GetString(3)
            });
        }
        return drawings;
    }

    public void DeleteShapesOfDrawing(int drawingId)
    {
        using var connection = new SqliteConnection(GetDBPath());
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM Shapes WHERE drawing_id = @drawingId;
        ";
        cmd.Parameters.AddWithValue("@drawingId", drawingId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteDrawing(int drawingId)
    {
        using var connection = new SqliteConnection(GetDBPath());
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM Drawings WHERE id = @drawingId;
        ";
        cmd.Parameters.AddWithValue("@drawingId", drawingId);
        cmd.ExecuteNonQuery();
    }
}
