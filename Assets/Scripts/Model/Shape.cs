using System.Collections.Generic;
using UnityEngine;

public enum ShapeType { Point, Line, Circle, RegularShape, IrregularShape }

public class Shape
{
    public int id;
    public int drawingId;
    public ShapeType type;
    public Color color;
    public Color fillColor;
    public float thickness;
    public Vector2 position;
    public List<Vector2> points = new();

    public Shape(ShapeType type)
    {
        this.type = type;
    }
}
