using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SVGExporter
{
    public static void ExportDrawingToSVG(List<Shape> shapes, int drawingId)
    {
        // Dimensiones del SVG
        float svgWidth = 800f;
        float svgHeight = 600f;

        // 1. Encuentra el bounding box de todos los puntos
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var shape in shapes)
        {
            foreach (var p in shape.points)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }
        }
        // Evita división por cero
        float rangeX = Mathf.Max(maxX - minX, 0.01f);
        float rangeY = Mathf.Max(maxY - minY, 0.01f);

        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string path = Path.Combine(desktopPath, $"drawing_{drawingId}.svg");
        using (StreamWriter sw = new(path))
        {
            sw.WriteLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{svgWidth}' height='{svgHeight}'>");

            foreach (var shape in shapes)
            {
                // Función para transformar un punto de Unity a SVG
                Vector2 ToSVG(Vector2 p)
                {
                    float x = ((p.x - minX) / rangeX) * svgWidth;
                    float y = svgHeight - ((p.y - minY) / rangeY) * svgHeight; // Invertir Y
                    return new Vector2(x, y);
                }

                switch (shape.type)
                {
                    case ShapeType.Line:
                        if (shape.points.Count >= 2)
                        {
                            Vector2 p1 = ToSVG(shape.points[0]);
                            Vector2 p2 = ToSVG(shape.points[1]);
                            sw.WriteLine($"<line x1='{p1.x}' y1='{p1.y}' x2='{p2.x}' y2='{p2.y}' stroke='rgb({shape.color.r * 255},{shape.color.g * 255},{shape.color.b * 255})' stroke-width='{shape.thickness * 10}' />");
                        }
                        break;
                    case ShapeType.Point:
                        {
                            Vector2 p = ToSVG(shape.points[0]);
                            sw.WriteLine($"<circle cx='{p.x}' cy='{p.y}' r='2' fill='rgb({shape.color.r * 255},{shape.color.g * 255},{shape.color.b * 255})' />");
                        }
                        break;
                    case ShapeType.IrregularShape:
                    case ShapeType.RegularShape:
                    case ShapeType.Circle:
                        {
                            // Relleno
                            string fill = shape.fillColor.a > 0
                                ? $"fill='rgba({shape.fillColor.r * 255},{shape.fillColor.g * 255},{shape.fillColor.b * 255},{shape.fillColor.a})'"
                                : "fill='none'";
                            // Borde
                            string stroke = $"stroke='rgb({shape.color.r * 255},{shape.color.g * 255},{shape.color.b * 255})'";
                            string strokeWidth = $"stroke-width='{shape.thickness * 10}'";

                            sw.Write("<polygon points='");
                            foreach (var p in shape.points)
                            {
                                Vector2 svgP = ToSVG(p);
                                sw.Write($"{svgP.x},{svgP.y} ");
                            }
                            sw.WriteLine($"' {fill} {stroke} {strokeWidth} />");
                        }
                        break;
                    default:
                        // Otros casos como polilínea sin relleno
                        sw.Write("<polyline points='");
                        foreach (var p in shape.points)
                        {
                            Vector2 svgP = ToSVG(p);
                            sw.Write($"{svgP.x},{svgP.y} ");
                        }
                        sw.Write($"' fill='none' stroke='rgb({shape.color.r * 255},{shape.color.g * 255},{shape.color.b * 255})' stroke-width='{shape.thickness * 10}' />\n");
                        break;
                }
            }

            sw.WriteLine("</svg>");
        }
        Debug.Log($"SVG exportado: {path}");
    }
}