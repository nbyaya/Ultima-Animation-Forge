using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace UltimaAnimationForge.Models;

public enum DragonBrushShape
{
    Circle,
    Square,
    Diamond,
    HorizontalLine,
    VerticalLine,
    Slash,
    Backslash,
    Cross,
    X
}

public enum DragonMapTool
{
    Paint,
    Eyedropper,
    Fill,
    Erase,
    Rectangle,
    Line,
    Ellipse,
    Stamp
}

// DragonBrushShape 包装类
public sealed class UoMapMakerBrushShapeItem : IEquatable<UoMapMakerBrushShapeItem>
{
    public DragonBrushShape Shape { get; }

    public UoMapMakerBrushShapeItem(DragonBrushShape shape)
    {
        Shape = shape;
    }

    public override string ToString() => Shape switch
    {
        DragonBrushShape.Circle => "笔圆",
        DragonBrushShape.Square => "笔方",
        DragonBrushShape.Diamond => "笔菱",
        DragonBrushShape.HorizontalLine => "笔横线",
        DragonBrushShape.VerticalLine => "笔纵线",
        DragonBrushShape.Slash => "笔斜杠",
        DragonBrushShape.Backslash => "笔反斜杠",
        DragonBrushShape.Cross => "笔十字",
        DragonBrushShape.X => "笔X",
        _ => "未知"
    };

    public bool Equals(UoMapMakerBrushShapeItem? other) => other != null && Shape == other.Shape;
    public override bool Equals(object? obj) => Equals(obj as UoMapMakerBrushShapeItem);
    public override int GetHashCode() => Shape.GetHashCode();
}

// DragonMapTool 包装类
public sealed class UoMapMakerToolItem : IEquatable<UoMapMakerToolItem>
{
    public DragonMapTool Tool { get; }

    public UoMapMakerToolItem(DragonMapTool tool)
    {
        Tool = tool;
    }

    public override string ToString() => Tool switch
    {
        DragonMapTool.Paint => "画笔",
        DragonMapTool.Eyedropper => "取色器",
        DragonMapTool.Fill => "填充",
        DragonMapTool.Erase => "橡皮",
        DragonMapTool.Rectangle => "矩形",
        DragonMapTool.Line => "直线",
        DragonMapTool.Ellipse => "椭圆",
        DragonMapTool.Stamp => "图章",
        _ => "未知"
    };

    public bool Equals(UoMapMakerToolItem? other) => other != null && Tool == other.Tool;
    public override bool Equals(object? obj) => Equals(obj as UoMapMakerToolItem);
    public override int GetHashCode() => Tool.GetHashCode();
}

public sealed class DragonMapStamp
{
    public string Name { get; set; } = string.Empty;
    public int[,] Pattern { get; set; } = new int[0, 0];

    public override string ToString() => Name;
}

public sealed class DragonTerrainColor
{
    public int PaletteIndex { get; set; }
    public string PaletteHex => PaletteIndex.ToString("X2");

    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;

    public int Z { get; set; }
    public List<int> TileIds { get; set; } = new();

    public Color Color { get; set; }
    public IBrush Brush => new SolidColorBrush(Color);

    public string Name => $"{GroupName} {Z}";
    public string TerrainType => GroupName;

    public string DisplayText =>
        $"{PaletteHex} | {GroupName} | Z {Z} | 图块 {TileIds.Count}";
}