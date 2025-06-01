using UnityEngine;

public enum ShapeType { Point, Line, Circle, RegularShape, IrregularShape, }

public abstract class Shape
{
    public ShapeType type;

}
