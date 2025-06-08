using UnityEngine;

public enum ShapeType { Point, Line, Circle, RegularShape, IrregularShape, }

public abstract class Shape
{
    public ShapeType type;
    public Vector3 position; // Position in world space and center of the shape


}
