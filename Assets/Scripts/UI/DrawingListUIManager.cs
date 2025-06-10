using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DrawingListUIManager : MonoBehaviour
{
    public GameObject drawingItemPrefab;
    public GameObject listViewport;
    public Transform contentParent;

    private List<Drawing> drawingsList = new();

    public void ShowDrawingsList()
    {
        // Limpia la lista anterior
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        drawingsList = DBController.instance.GetAllDrawings();

        foreach (var drawing in drawingsList)
        {
            GameObject item = Instantiate(drawingItemPrefab, contentParent);
            item.transform.Find("DrawingName").GetComponent<TextMeshProUGUI>().text = drawing.name;

            // Botón Cargar
            Button loadBtn = item.transform.Find("LoadButton").GetComponent<Button>();
            loadBtn.onClick.AddListener(() => OnLoadDrawing(drawing.id));

            // Botón Exportar SVG
            Button svgBtn = item.transform.Find("ExportSVGButton").GetComponent<Button>();
            svgBtn.onClick.AddListener(() => OnExportSVG(drawing.id));
        }
    }

    private void OnLoadDrawing(int drawingId)
    {
        ShapeManager.instance.currentDrawingId = drawingId;
        DrawingManager.instance.LoadDrawing(drawingId);
        listViewport.SetActive(false);
    }

    private void OnExportSVG(int drawingId)
    {
        List<Shape> shapes = DBController.instance.LoadShapes(drawingId);
        SVGExporter.ExportDrawingToSVG(shapes, drawingId);
        // Puedes mostrar un mensaje de éxito aquí
    }
}