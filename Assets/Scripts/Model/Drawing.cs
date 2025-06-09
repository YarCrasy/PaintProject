using System;
using System.Collections.Generic;

[Serializable]
public class Drawing
{
    public int id;
    public string name;
    public string createdAt;
    public string updatedAt;

    public List<Shape> shapes = new();
}
