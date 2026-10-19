
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UltimaAnimationForge.Models;
using UltimaAnimationForge.Services;

namespace UltimaAnimationForge.ViewModels;

public partial class MainWindowViewModel
{
    private readonly DragonMapUopExportService dragonMapUopExportService = new();

    [ObservableProperty]
    private bool showDragonBrushPreview;

    [ObservableProperty]
    private double dragonBrushPreviewLeft;

    [ObservableProperty]
    private double dragonBrushPreviewTop;

    [ObservableProperty]
    private double dragonBrushPreviewSize;

    [ObservableProperty]
    private string dragonBrushPreviewRadius = "50%";

    [ObservableProperty]
    private string dragonMapHoverStatusText = "X: - Y: - | 地形: -";

    private readonly Stack<WriteableBitmap> dragonMapUndoStack = new();

    public ObservableCollection<UoMapMakerBrushShapeItem> DragonBrushShapes { get; } = new()
{
    new UoMapMakerBrushShapeItem(DragonBrushShape.Circle),
    new UoMapMakerBrushShapeItem(DragonBrushShape.Square),
    new UoMapMakerBrushShapeItem(DragonBrushShape.Diamond),
    new UoMapMakerBrushShapeItem(DragonBrushShape.HorizontalLine),
    new UoMapMakerBrushShapeItem(DragonBrushShape.VerticalLine),
    new UoMapMakerBrushShapeItem(DragonBrushShape.Slash),
    new UoMapMakerBrushShapeItem(DragonBrushShape.Backslash),
    new UoMapMakerBrushShapeItem(DragonBrushShape.Cross),
    new UoMapMakerBrushShapeItem(DragonBrushShape.X)
};

    public ObservableCollection<UoMapMakerToolItem> DragonMapTools { get; } = new()
{
    new UoMapMakerToolItem(DragonMapTool.Paint),
    new UoMapMakerToolItem(DragonMapTool.Eyedropper),
    new UoMapMakerToolItem(DragonMapTool.Fill),
    new UoMapMakerToolItem(DragonMapTool.Erase),
    new UoMapMakerToolItem(DragonMapTool.Rectangle),
    new UoMapMakerToolItem(DragonMapTool.Line),
    new UoMapMakerToolItem(DragonMapTool.Ellipse),
    new UoMapMakerToolItem(DragonMapTool.Stamp)
};
    public ObservableCollection<DragonMapStamp> DragonMapStamps { get; } = new();

    [ObservableProperty]
    private DragonMapStamp? selectedDragonMapStamp;

    private bool dragonRectangleHasStart;
    private int dragonRectangleStartX;
    private int dragonRectangleStartY;
    private bool dragonLineHasStart;
    private int dragonLineStartX;
    private int dragonLineStartY;
    private bool dragonEllipseHasStart;
    private int dragonEllipseStartX;
    private int dragonEllipseStartY;

    [ObservableProperty]
    private UoMapMakerToolItem? selectedDragonMapTool = new(DragonMapTool.Paint);

    // 获取实际的 DragonMapTool 枚举值
    private DragonMapTool SelectedDragonMapToolValue => SelectedDragonMapTool?.Tool ?? DragonMapTool.Paint;

    private readonly DragonScriptService dragonScriptService = new();

    [ObservableProperty]
    private UoMapMakerBrushShapeItem? selectedDragonBrushShape = new(DragonBrushShape.Circle);

    // 获取实际的 DragonBrushShape 枚举值
    private DragonBrushShape SelectedDragonBrushShapeValue => SelectedDragonBrushShape?.Shape ?? DragonBrushShape.Circle;

    [ObservableProperty]
    private int dragonMapRefreshKey;

    public double DragonMapImageWidth =>
        DragonMapBitmap == null ? 0 : DragonMapBitmap.PixelSize.Width * DragonMapZoom;

    public double DragonMapImageHeight =>
        DragonMapBitmap == null ? 0 : DragonMapBitmap.PixelSize.Height * DragonMapZoom;

    private readonly DragonMapService dragonMapService = new();

    public ObservableCollection<DragonTerrainColor> DragonTerrainColors { get; } = new();

    [ObservableProperty]
    private WriteableBitmap? dragonMapBitmap;

    [ObservableProperty]
    private DragonTerrainColor? selectedDragonTerrainColor;

    [ObservableProperty]
    private int dragonBrushSize = 8;

    [ObservableProperty]
    private string dragonMapStatusText = "未加载龙地图";

    [ObservableProperty]
    private double dragonMapZoom = 1.0;

    public string DragonMapZoomText => $"{DragonMapZoom * 100:0}%";

    public void HandleDragonMapClick(int x, int y)
    {
        if (SelectedDragonMapToolValue == DragonMapTool.Stamp)
        {
            PlaceDragonStampAt(x, y);
            return;
        }

        if (SelectedDragonMapToolValue == DragonMapTool.Eyedropper)
        {
            PickDragonTerrainAt(x, y);
            return;
        }

        if (SelectedDragonMapToolValue == DragonMapTool.Fill)
        {
            FillDragonMapAreaAt(x, y);
            return;
        }

        if (SelectedDragonMapToolValue == DragonMapTool.Erase)
        {
            EraseDragonMapAt(x, y);
            return;
        }

        if (SelectedDragonMapToolValue == DragonMapTool.Rectangle)
        {
            DrawDragonRectangleAt(x, y);
            return;
        }

        if (SelectedDragonMapToolValue == DragonMapTool.Line)
        {
            DrawDragonLineAt(x, y);
            return;
        }

        if (SelectedDragonMapToolValue == DragonMapTool.Ellipse)
        {
            DrawDragonEllipseAt(x, y);
            return;
        }

        PaintDragonMapAt(x, y);
    }

    public void EraseDragonMapAt(int x, int y)
    {
        if (DragonMapBitmap == null)
        {
            return;
        }

        DragonTerrainColor? eraseTerrain = GetEraseTerrain();

        if (eraseTerrain == null)
        {
            return;
        }

        WriteableBitmap bitmap = DragonMapBitmap;

        dragonMapService.PaintBrush(
            bitmap,
            x,
            y,
            DragonBrushSize,
            SelectedDragonBrushShapeValue,
            eraseTerrain.Color);
    }

    private void PickDragonTerrainAt(int x, int y)
    {
        if (DragonMapBitmap == null)
        {
            return;
        }

        Avalonia.Media.Color? pickedColor = dragonMapService.GetPixelColor(DragonMapBitmap, x, y);

        if (pickedColor == null)
        {
            return;
        }

        DragonTerrainColor? match = null;
        int minDistance = int.MaxValue;

        foreach (DragonTerrainColor terrain in DragonTerrainColors)
        {
            int distance = GetColorDistance(terrain.Color, pickedColor.Value);

            if (distance < minDistance)
            {
                minDistance = distance;
                match = terrain;

                if (minDistance == 0)
                {
                    break;
                }
            }
        }

        if (match == null)
        {
            DragonMapStatusText = $"No terrain match at X:{x} Y:{y}.";
            return;
        }

        SelectedDragonTerrainColor = match;
        SelectedDragonMapTool = new UoMapMakerToolItem(DragonMapTool.Paint);
    }

    private static int GetColorDistance(Avalonia.Media.Color a, Avalonia.Media.Color b)
    {
        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;

        return (dr * dr) + (dg * dg) + (db * db);
    }

    partial void OnDragonMapBitmapChanged(WriteableBitmap? value)
    {
        OnPropertyChanged(nameof(DragonMapImageWidth));
        OnPropertyChanged(nameof(DragonMapImageHeight));
    }

    partial void OnDragonMapZoomChanged(double value)
    {
        OnPropertyChanged(nameof(DragonMapZoomText));
        OnPropertyChanged(nameof(DragonMapImageWidth));
        OnPropertyChanged(nameof(DragonMapImageHeight));
    }

    [RelayCommand]
    public void InitializeMapMaker()
    {
        DragonTerrainColors.Clear();
        InitializeDragonMapStamps();

        string appBase = AppContext.BaseDirectory;
        string dragonScriptsFolder = Path.Combine(appBase, "Scripts", "Dragon");

        string maptransPath = Path.Combine(dragonScriptsFolder, "maptrans105.txt");
        string groupsPath = Path.Combine(dragonScriptsFolder, "groups.txt");

        string paletteFolder = Path.Combine(dragonScriptsFolder, "Palettes");
        string palettePath = FindDragonPalettePath(paletteFolder);

        if (!File.Exists(maptransPath))
        {
            maptransPath = Path.Combine(dragonScriptsFolder, "maptrans.txt");
        }

        if (File.Exists(maptransPath))
        {
            foreach (DragonTerrainColor color in dragonScriptService.LoadTerrainColors(maptransPath, groupsPath, palettePath))
            {
                DragonTerrainColors.Add(color);
            }

            string paletteText = string.IsNullOrWhiteSpace(palettePath)
                ? "no palette"
                : Path.GetFileName(palettePath);

            DragonMapStatusText =
                "已从 " +
                Path.GetFileName(maptransPath) +
                " 加载 Dragon 地形，使用 " +
                paletteText +
                "。";
        }
        else
        {
            foreach (DragonTerrainColor color in dragonMapService.BuildDefaultTerrainPalette())
            {
                DragonTerrainColors.Add(color);
            }

            DragonMapStatusText = "未找到 Dragon 脚本。已加载备用地形调色板。";
        }

        if (DragonTerrainColors.Count > 0)
        {
            SelectedDragonTerrainColor = DragonTerrainColors[0];
        }
    }

    private static string FindDragonPalettePath(string paletteFolder)
    {
        if (string.IsNullOrWhiteSpace(paletteFolder) || !Directory.Exists(paletteFolder))
        {
            return string.Empty;
        }

        string[] preferred =
        {
        "dragon-mod.act",
        "dragon-mod9.act",
        "dragon-mod.pal",
        "dragon-mod9.pal",
        "colortable.ACO"
    };

        foreach (string fileName in preferred)
        {
            string path = Path.Combine(paletteFolder, fileName);

            if (File.Exists(path))
            {
                return path;
            }
        }

        string? firstPalette = Directory
            .GetFiles(paletteFolder)
            .FirstOrDefault(x =>
                x.EndsWith(".act", StringComparison.OrdinalIgnoreCase) ||
                x.EndsWith(".pal", StringComparison.OrdinalIgnoreCase));

        return firstPalette ?? string.Empty;
    }

    [RelayCommand]
    private void NewDragonMap()
    {
        if (DragonTerrainColors.Count == 0)
        {
            InitializeMapMaker();
        }

        DragonTerrainColor? water = DragonTerrainColors.FirstOrDefault(x =>
            x.GroupName.Equals("Water", StringComparison.OrdinalIgnoreCase) &&
            x.Z == -5);

        water ??= DragonTerrainColors.FirstOrDefault();

        if (water == null)
        {
            DragonMapStatusText = "未加载地形颜色。";
            return;
        }

        DragonMapBitmap = dragonMapService.CreateBlankMap(water.Color);
        DragonMapZoom = 1.0;
        DragonMapRefreshKey++;

        DragonMapStatusText =
            $"Created new {DragonMapBitmap.PixelSize.Width} x {DragonMapBitmap.PixelSize.Height} Dragon map filled with {water.Name}.";
    }

    public void BeginDragonMapPaintStroke()
    {
        PushDragonMapUndo();
    }

    public void PaintDragonMapAt(int x, int y)
    {
        if (DragonMapBitmap == null || SelectedDragonTerrainColor == null)
        {
            return;
        }

        dragonMapService.PaintBrush(
            DragonMapBitmap,
            x,
            y,
            DragonBrushSize,
            SelectedDragonBrushShapeValue,
            SelectedDragonTerrainColor.Color);
    }

    public void RefreshDragonMapAfterStroke()
    {
        OnPropertyChanged(nameof(DragonMapBitmap));
        DragonMapRefreshKey++;
    }

    [RelayCommand]
    private void FillDragonMapCanvas()
    {
        if (DragonMapBitmap == null || SelectedDragonTerrainColor == null)
        {
            return;
        }
        PushDragonMapUndo();

        WriteableBitmap bitmap = DragonMapBitmap;

        dragonMapService.FillCanvas(bitmap, SelectedDragonTerrainColor.Color);

        DragonMapBitmap = null;
        DragonMapBitmap = bitmap;

        DragonMapRefreshKey++;

        DragonMapStatusText = "用 " + SelectedDragonTerrainColor.Name + " 填充画布。";
    }

    public void OpenDragonMapFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            DragonMapStatusText = "龙图文件未找到。";
            return;
        }

        DragonMapBitmap = dragonMapService.LoadBitmap(filePath);
        DragonMapZoom = 1.0;
        DragonMapStatusText = "打开地图：" + Path.GetFileName(filePath);
    }

    public void SaveDragonMapToFile(string filePath)
    {
        if (DragonMapBitmap == null)
        {
            DragonMapStatusText = "没有龙图可保存。";
            return;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        dragonMapService.SaveBitmap(DragonMapBitmap, filePath);
        DragonMapStatusText = "保存地图：" + Path.GetFileName(filePath);
    }

    private void PushDragonMapUndo()
    {
        if (DragonMapBitmap == null)
        {
            return;
        }

        dragonMapUndoStack.Push(CloneDragonMapBitmap(DragonMapBitmap));

        if (dragonMapUndoStack.Count > 20)
        {
            dragonMapUndoStack.Clear();
        }
    }

    private static WriteableBitmap CloneDragonMapBitmap(WriteableBitmap source)
    {
        using MemoryStream stream = new();
        source.Save(stream);
        stream.Position = 0;
        return WriteableBitmap.Decode(stream);
    }

    [RelayCommand]
    private void UndoDragonMap()
    {
        if (dragonMapUndoStack.Count == 0)
        {
            DragonMapStatusText = "没有撤销操作。";
            return;
        }

        DragonMapBitmap = dragonMapUndoStack.Pop();
        DragonMapRefreshKey++;

        DragonMapStatusText = "撤销地图编辑。";
    }

    private void FillDragonMapAreaAt(int x, int y)
    {
        if (DragonMapBitmap == null || SelectedDragonTerrainColor == null)
        {
            return;
        }

        PushDragonMapUndo();

        WriteableBitmap bitmap = DragonMapBitmap;

        dragonMapService.FloodFill(
            bitmap,
            x,
            y,
            SelectedDragonTerrainColor.Color);

        DragonMapBitmap = null;
        DragonMapBitmap = bitmap;

        DragonMapRefreshKey++;

        DragonMapStatusText =
            $"填充面积中 {SelectedDragonTerrainColor.Name} 在 X:{x} Y:{y}。";

        SelectedDragonMapTool = new UoMapMakerToolItem(DragonMapTool.Paint);
    }

    public void UpdateDragonMapHoverStatus(int x, int y)
    {
        if (DragonMapBitmap == null)
        {
            DragonMapHoverStatusText = "X: - Y: - | 地形: -";
            return;
        }

        Avalonia.Media.Color? color = dragonMapService.GetPixelColor(DragonMapBitmap, x, y);

        if (color == null)
        {
            DragonMapHoverStatusText = "X: - Y: - | 地形: -";
            return;
        }

        DragonTerrainColor? match = null;
        int minDistance = int.MaxValue;

        foreach (DragonTerrainColor terrain in DragonTerrainColors)
        {
            int distance = GetColorDistance(terrain.Color, color.Value);

            if (distance < minDistance)
            {
                minDistance = distance;
                match = terrain;

                if (minDistance == 0)
                {
                    break;
                }
            }
        }

        if (match == null)
        {
            DragonMapHoverStatusText = $"X: {x} Y: {y} | 未知";
            return;
        }

        DragonMapHoverStatusText =
            $"X: {x} Y: {y} | {match.GroupName} | Z {match.Z} | 色板 {match.PaletteHex}";
    }

    private DragonTerrainColor? GetEraseTerrain()
    {
        return DragonTerrainColors.FirstOrDefault(x =>
            x.GroupName.Equals("Water", StringComparison.OrdinalIgnoreCase) && x.Z == -5)
            ?? DragonTerrainColors.FirstOrDefault();
    }

    public void UpdateDragonBrushPreview(int mapX, int mapY)
    {
        if (DragonMapBitmap == null)
        {
            ShowDragonBrushPreview = false;
            return;
        }

        double size = DragonBrushSize * DragonMapZoom;

        DragonBrushPreviewSize = size;
        DragonBrushPreviewLeft = (mapX * DragonMapZoom) - (size / 2.0);
        DragonBrushPreviewTop = (mapY * DragonMapZoom) - (size / 2.0);

        DragonBrushPreviewRadius = SelectedDragonBrushShapeValue switch
        {
            DragonBrushShape.Circle => "50%",
            DragonBrushShape.Diamond => "0",
            _ => "0"
        };

        ShowDragonBrushPreview =
            SelectedDragonMapToolValue == DragonMapTool.Paint ||
            SelectedDragonMapToolValue == DragonMapTool.Erase;
    }

    private void DrawDragonRectangleAt(int x, int y)
    {
        if (DragonMapBitmap == null || SelectedDragonTerrainColor == null)
        {
            return;
        }

        if (!dragonRectangleHasStart)
        {
            dragonRectangleStartX = x;
            dragonRectangleStartY = y;
            dragonRectangleHasStart = true;

            DragonMapStatusText = $"矩形起点设置在 X:{x} Y:{y}。点击第二个角。";
            return;
        }

        WriteableBitmap bitmap = DragonMapBitmap;

        dragonMapService.PaintRectangle(
            bitmap,
            dragonRectangleStartX,
            dragonRectangleStartY,
            x,
            y,
            SelectedDragonTerrainColor.Color);

        DragonMapBitmap = null;
        DragonMapBitmap = bitmap;

        DragonMapRefreshKey++;

        DragonMapStatusText =
            $"绘制了矩形 {SelectedDragonTerrainColor.Name} 从 X:{dragonRectangleStartX} Y:{dragonRectangleStartY} 到 X:{x} Y:{y}。";

        dragonRectangleHasStart = false;
        SelectedDragonMapTool = new UoMapMakerToolItem(DragonMapTool.Paint);
    }

    private void DrawDragonLineAt(int x, int y)
    {
        if (DragonMapBitmap == null || SelectedDragonTerrainColor == null)
        {
            return;
        }

        if (!dragonLineHasStart)
        {
            dragonLineStartX = x;
            dragonLineStartY = y;
            dragonLineHasStart = true;

            DragonMapStatusText = $"直线起点设置在 X:{x} Y:{y}。点击终点。";
            return;
        }

        WriteableBitmap bitmap = DragonMapBitmap;

        dragonMapService.PaintLine(
            bitmap,
            dragonLineStartX,
            dragonLineStartY,
            x,
            y,
            DragonBrushSize,
            SelectedDragonBrushShapeValue,
            SelectedDragonTerrainColor.Color);

        DragonMapBitmap = null;
        DragonMapBitmap = bitmap;

        DragonMapRefreshKey++;

        DragonMapStatusText =
            $"绘制了直线 {SelectedDragonTerrainColor.Name} 从 X:{dragonLineStartX} Y:{dragonLineStartY} 到 X:{x} Y:{y}。";

        dragonLineHasStart = false;
        SelectedDragonMapTool = new UoMapMakerToolItem(DragonMapTool.Paint);
    }

    private void DrawDragonEllipseAt(int x, int y)
    {
        if (DragonMapBitmap == null || SelectedDragonTerrainColor == null)
        {
            return;
        }

        if (!dragonEllipseHasStart)
        {
            dragonEllipseStartX = x;
            dragonEllipseStartY = y;
            dragonEllipseHasStart = true;

            DragonMapStatusText = $"椭圆起点设置在 X:{x} Y:{y}。点击对角。";
            return;
        }

        WriteableBitmap bitmap = DragonMapBitmap;

        dragonMapService.PaintEllipse(
            bitmap,
            dragonEllipseStartX,
            dragonEllipseStartY,
            x,
            y,
            SelectedDragonTerrainColor.Color);

        DragonMapBitmap = null;
        DragonMapBitmap = bitmap;

        DragonMapRefreshKey++;

        DragonMapStatusText =
            $"绘制了椭圆 {SelectedDragonTerrainColor.Name} 从 X:{dragonEllipseStartX} Y:{dragonEllipseStartY} 到 X:{x} Y:{y}。";

        dragonEllipseHasStart = false;
        SelectedDragonMapTool = new UoMapMakerToolItem(DragonMapTool.Paint);
    }

    [RelayCommand]
    private void ValidateDragonMap()
    {
        if (DragonMapBitmap == null)
        {
            DragonMapStatusText = "未加载龙地图。";
            return;
        }

        DragonMapService.ValidateResult result =
            dragonMapService.ValidateDragonMap(DragonMapBitmap, DragonTerrainColors);

        DragonMapStatusText = result.Message;
    }

    public void ExportDragonMapToUop(string outputPath)
    {
        if (DragonMapBitmap == null)
        {
            DragonMapStatusText = "未加载龙地图。";
            return;
        }

        DragonMapUopExportService.ExportResult result =
            dragonMapUopExportService.ExportMapLegacyUop(
                DragonMapBitmap,
                DragonTerrainColors,
                outputPath,
                0);

        DragonMapStatusText = result.Message;
    }

    public void ExportDragonMapToMul(string outputPath)
    {
        DragonMapUopExportService.ExportResult result =
            dragonMapUopExportService.ExportMapMul(
                DragonMapBitmap!,
                DragonTerrainColors,
                outputPath);

        DragonMapStatusText = result.Message;
    }

    public void ExportDragonMapMulAndUop(string folderPath)
    {
        string mulPath = Path.Combine(folderPath, "map0.mul");
        string uopPath = Path.Combine(folderPath, "map0LegacyMUL.uop");

        DragonMapUopExportService.ExportResult result =
            dragonMapUopExportService.ExportMapMulAndUop(
                DragonMapBitmap!,
                DragonTerrainColors,
                mulPath,
                uopPath,
                0);

        DragonMapStatusText = result.Message;
    }

    private void InitializeDragonMapStamps()
    {
        DragonMapStamps.Clear();

        DragonMapStamps.Add(new DragonMapStamp
        {
            Name = "小岛",
            Pattern = new[,]
            {
            { -1,  5,  5, -1 },
            {  5,  1,  1,  5 },
            {  5,  1,  1,  5 },
            { -1,  5,  5, -1 }
        }
        });

        DragonMapStamps.Add(new DragonMapStamp
        {
            Name = "山脉",
            Pattern = new[,]
            {
            { -1,  6,  6, -1 },
            {  6,  6,  6,  6 },
            {  6,  6,  6,  6 },
            { -1,  6,  6, -1 }
        }
        });

        SelectedDragonMapStamp ??= DragonMapStamps.FirstOrDefault();
    }

    private void PlaceDragonStampAt(int centerX, int centerY)
    {
        if (DragonMapBitmap == null || SelectedDragonMapStamp == null)
        {
            return;
        }

        WriteableBitmap bitmap = DragonMapBitmap;

        dragonMapService.PaintStampPattern(
           bitmap,
           centerX,
           centerY,
           SelectedDragonMapStamp.Pattern,
           DragonTerrainColors,
           DragonBrushSize);

        DragonMapBitmap = null;
        DragonMapBitmap = bitmap;

        DragonMapRefreshKey++;

        DragonMapStatusText =
            $"Placed stamp {SelectedDragonMapStamp.Name} at X:{centerX} Y:{centerY}.";
    }
}