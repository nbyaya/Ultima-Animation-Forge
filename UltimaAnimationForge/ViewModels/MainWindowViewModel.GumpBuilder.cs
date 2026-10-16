using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UltimaAnimationForge.Models;
using UltimaAnimationForge.Services;

namespace UltimaAnimationForge.ViewModels;

public partial class MainWindowViewModel
{
    public ObservableCollection<GumpBuilderElement> GumpBuilderElements { get; } = new();

    public ObservableCollection<GumpEntry> GumpBuilderPickerEntries { get; } = new();

    [ObservableProperty]
    private string gumpBuilderActiveExportKind = string.Empty;

    public bool HasGumpBuilderActiveExport =>
        !string.IsNullOrWhiteSpace(GumpBuilderActiveExportKind);

    public bool IsGumpBuilderUox3ExportVisible =>
        GumpBuilderActiveExportKind == "UOX3";

    public bool IsGumpBuilderCSharpExportVisible =>
        GumpBuilderActiveExportKind == "CSharp";

    public bool IsGumpBuilderSphereExportVisible =>
        GumpBuilderActiveExportKind == "Sphere";

    public bool IsGumpBuilderPolExportVisible =>
        GumpBuilderActiveExportKind == "POL";

    public string GumpBuilderActiveExportTitle
    {
        get
        {
            return GumpBuilderActiveExportKind switch
            {
                "UOX3" => "UOX3 JavaScript 预览",
                "CSharp" => "C# 预览",
                "Sphere" => "Sphere 预览",
                "POL" => "POL 预览",
                _ => string.Empty
            };
        }
    }

    public string GumpBuilderActiveCopyButtonText
    {
        get
        {
            return GumpBuilderActiveExportKind switch
            {
                "UOX3" => "复制 JS",
                "CSharp" => "复制 C#",
                "Sphere" => "复制 Sphere",
                "POL" => "复制 POL",
                _ => "复制"
            };
        }
    }

    public string GumpBuilderActiveExportText
    {
        get
        {
            return GumpBuilderActiveExportKind switch
            {
                "UOX3" => GumpBuilderExportText,
                "CSharp" => GumpBuilderCSharpExportText,
                "Sphere" => GumpBuilderSphereExportText,
                "POL" => GumpBuilderPolExportText,
                _ => string.Empty
            };
        }

        set
        {
            switch (GumpBuilderActiveExportKind)
            {
                case "UOX3":
                    GumpBuilderExportText = value;
                    break;

                case "CSharp":
                    GumpBuilderCSharpExportText = value;
                    break;

                case "Sphere":
                    GumpBuilderSphereExportText = value;
                    break;

                case "POL":
                    GumpBuilderPolExportText = value;
                    break;
            }

            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private string gumpBuilderPolExportText = string.Empty;

    [ObservableProperty]
    private string gumpBuilderPolName = "gump";

    [ObservableProperty]
    private bool gumpBuilderPolUseDistro;

    [ObservableProperty]
    private string gumpBuilderSphereExportText = string.Empty;

    [ObservableProperty]
    private bool gumpBuilderSphereRevision = true;

    [ObservableProperty]
    private string gumpBuilderSphereName = "d_default";

    [ObservableProperty]
    private string gumpBuilderCSharpExportText = string.Empty;

    [ObservableProperty]
    private WriteableBitmap? selectedGumpBuilderGumpPickerPreview;

    [ObservableProperty]
    private int gumpBuilderMasterGumpId;

    [ObservableProperty]
    private bool gumpBuilderUseMasterGump;

    [ObservableProperty]
    private bool gumpBuilderNoClose;

    [ObservableProperty]
    private bool gumpBuilderNoDispose;

    [ObservableProperty]
    private bool gumpBuilderNoMove;

    [ObservableProperty]
    private bool gumpBuilderNoResize;

    [ObservableProperty]
    private WriteableBitmap? selectedGumpBuilderArtPickerPreview;

    [ObservableProperty]
    private GumpEntry? selectedGumpBuilderPickerEntry;

    [ObservableProperty]
    private string gumpBuilderPickerSearchText = string.Empty;

    [ObservableProperty]
    private string gumpBuilderExportText = string.Empty;

    [ObservableProperty]
    private bool gumpBuilderSnapToGrid = true;

    [ObservableProperty]
    private int gumpBuilderGridSize = 5;

    [ObservableProperty]
    private GumpBuilderElement? selectedGumpBuilderElement;

    [ObservableProperty]
    private int gumpBuilderWidth = 1100;

    [ObservableProperty]
    private int gumpBuilderHeight = 750;

    [ObservableProperty]
    private string gumpBuilderStatusText = "Gump 构建器就绪。";

    public ObservableCollection<ArtEntry> GumpBuilderArtPickerEntries { get; } = new();

    [ObservableProperty]
    private ArtEntry? selectedGumpBuilderArtPickerEntry;

    [ObservableProperty]
    private string gumpBuilderArtPickerSearchText = string.Empty;

    private GumpBuilderElement? exportWatchedGumpBuilderElement;

    partial void OnGumpBuilderActiveExportKindChanged(string value)
    {
        OnPropertyChanged(nameof(HasGumpBuilderActiveExport));
        OnPropertyChanged(nameof(IsGumpBuilderUox3ExportVisible));
        OnPropertyChanged(nameof(IsGumpBuilderCSharpExportVisible));
        OnPropertyChanged(nameof(IsGumpBuilderSphereExportVisible));
        OnPropertyChanged(nameof(IsGumpBuilderPolExportVisible));
        OnPropertyChanged(nameof(GumpBuilderActiveExportTitle));
        OnPropertyChanged(nameof(GumpBuilderActiveCopyButtonText));
        OnPropertyChanged(nameof(GumpBuilderActiveExportText));
    }

    partial void OnSelectedGumpBuilderPickerEntryChanged(GumpEntry? value)
    {
        SelectedGumpBuilderGumpPickerPreview = null;

        if (value == null)
        {
            return;
        }

        SelectedGumpBuilderGumpPickerPreview = LoadBuilderGumpBitmap(value.GumpId);
    }

    private void SortGumpBuilderElementsByZ()
    {
        GumpBuilderElement? selected = SelectedGumpBuilderElement;

        List<GumpBuilderElement> sorted = GumpBuilderElements
            .OrderBy(x => x.Z)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            int oldIndex = GumpBuilderElements.IndexOf(sorted[i]);

            if (oldIndex >= 0 && oldIndex != i)
            {
                GumpBuilderElements.Move(oldIndex, i);
            }
        }

        SelectedGumpBuilderElement = selected;
    }

    partial void OnSelectedGumpBuilderArtPickerEntryChanged(ArtEntry? value)
    {
        SelectedGumpBuilderArtPickerPreview = null;

        if (value == null)
        {
            return;
        }

        SelectedGumpBuilderArtPickerPreview = artDataService.LoadBitmap(value);
    }

    private bool EnsureGumpBuilderGumpsLoaded()
    {
        if (gumpDataService.Entries.Count > 0)
        {
            return true;
        }

        InitializeGumpsForCurrentFolder();

        if (gumpDataService.Entries.Count == 0)
        {
            GumpBuilderStatusText = "未加载任何 Gump。请先打开 UO 文件夹。";
            return false;
        }

        return true;
    }

    private WriteableBitmap? LoadBuilderGumpBitmap(int gumpId)
    {
        if (!EnsureGumpBuilderGumpsLoaded())
        {
            return null;
        }

        GumpEntry? entry = gumpDataService.Entries
            .FirstOrDefault(x => x.GumpId == gumpId && x.IsValid);

        if (entry == null)
        {
            GumpBuilderStatusText = "未找到 Gump " + gumpId + "。";
            return null;
        }

        GumpLoadResult result = gumpDataService.LoadGump(entry);

        if (!result.Success || result.Bitmap == null)
        {
            GumpBuilderStatusText = result.Message;
            return null;
        }

        return result.Bitmap;
    }

    [RelayCommand]
    private void AddGumpBuilderBackground()
    {
        if (gumpDataService.Entries.Count == 0)
        {
            InitializeGumpsForCurrentFolder();
        }

        if (gumpDataService.Entries.Count == 0)
        {
            GumpBuilderStatusText = "未加载任何 Gump。请先打开 UO 文件夹。";
            return;
        }

        List<WriteableBitmap> parts = new();

        for (int gumpId = 9200; gumpId <= 9208; gumpId++)
        {
            GumpEntry? entry = gumpDataService.Entries
                .FirstOrDefault(x => x.GumpId == gumpId && x.IsValid);

            if (entry == null)
            {
                GumpBuilderStatusText = "缺少背景 Gump 部分 " + gumpId + "。";
                return;
            }

            GumpLoadResult result = gumpDataService.LoadGump(entry);

            if (!result.Success || result.Bitmap == null)
            {
                GumpBuilderStatusText = "加载背景 Gump 部分 " + gumpId + " 失败。" + result.Message;
                return;
            }

            parts.Add(result.Bitmap);
        }

        const int width = 100;
        const int height = 100;

        WriteableBitmap rendered =
            GumpBuilderRenderService.BuildBackgroundBitmap(parts, width, height);

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Background,
            Name = "AddBackground 1",
            X = 364,
            Y = 216,
            Width = width,
            Height = height,
            GumpId = 9200,
            BackgroundParts = parts,
            Bitmap = rendered
        });

        GumpBuilderStatusText = "添加了使用 9 个 Gump 部分的 AddBackground 9200。";
    }

    [RelayCommand]
    private void AddGumpBuilderImage()
    {
        RefreshGumpBuilderArtPicker();

        GumpBuilderStatusText =
            "美术拾取器已打开。已加载条目：" +
            GumpBuilderArtPickerEntries.Count +
            "。向下滚动到美术拾取器，选择美术，然后单击\"添加所选美术\"。";
    }

    [RelayCommand]
    private void AddGumpBuilderButton()
    {
        WriteableBitmap? normalBitmap = LoadBuilderGumpBitmap(4005);
        WriteableBitmap? pressedBitmap = LoadBuilderGumpBitmap(4006);

        if (normalBitmap == null)
        {
            GumpBuilderStatusText = "无法加载按钮 Gump 4005。";
            return;
        }

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Button,
            Name = "AddButton 1",
            X = 80,
            Y = 80,
            Width = normalBitmap.PixelSize.Width,
            Height = normalBitmap.PixelSize.Height,
            GumpId = 4005,
            PressedGumpId = 4006,
            ButtonId = 1,
            Bitmap = normalBitmap,
            PressedBitmap = pressedBitmap
        });

        GumpBuilderStatusText = "已添加按钮 4005 / 4006。";
    }

    [RelayCommand]
    private void RefreshGumpBuilderPicker()
    {
        GumpBuilderPickerEntries.Clear();

        if (!EnsureGumpBuilderGumpsLoaded())
        {
            return;
        }

        string search = (GumpBuilderPickerSearchText ?? string.Empty).Trim();

        IEnumerable<GumpEntry> entries = gumpDataService.Entries
            .Where(x => x.IsValid);

        if (!string.IsNullOrWhiteSpace(search))
        {
            entries = entries.Where(x =>
                x.GumpId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.GumpId.ToString("X").Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (GumpEntry entry in entries.Take(500))
        {
            GumpBuilderPickerEntries.Add(entry);
        }

        GumpBuilderStatusText = "在拾取器中加载了 " + GumpBuilderPickerEntries.Count + " 个 Gump。";
    }

    [RelayCommand]
    private void ApplySelectedPickerGumpToElement()
    {
        if (SelectedGumpBuilderElement == null)
        {
            GumpBuilderStatusText = "未选择构建器元素。";
            return;
        }

        if (SelectedGumpBuilderPickerEntry == null)
        {
            GumpBuilderStatusText = "未选择拾取器 Gump。";
            return;
        }

        WriteableBitmap? bitmap = LoadBuilderGumpBitmap(SelectedGumpBuilderPickerEntry.GumpId);

        if (bitmap == null)
        {
            return;
        }

        SelectedGumpBuilderElement.Type = GumpBuilderElementType.Image;
        SelectedGumpBuilderElement.Name = "Gump " + SelectedGumpBuilderPickerEntry.GumpId +
                                          " [0x" + SelectedGumpBuilderPickerEntry.GumpId.ToString("X") + "]";

        SelectedGumpBuilderElement.GumpId = SelectedGumpBuilderPickerEntry.GumpId;
        SelectedGumpBuilderElement.PressedGumpId = 0;
        SelectedGumpBuilderElement.ButtonId = 0;

        SelectedGumpBuilderElement.BackgroundParts.Clear();
        SelectedGumpBuilderElement.SourceBitmap = null;
        SelectedGumpBuilderElement.Bitmap = bitmap;

        SelectedGumpBuilderElement.Width = bitmap.PixelSize.Width;
        SelectedGumpBuilderElement.Height = bitmap.PixelSize.Height;

        ExportGumpBuilderUox3JavaScript();

        GumpBuilderStatusText = "应用了 Gump " +
                                SelectedGumpBuilderPickerEntry.GumpId +
                                "，原生尺寸为 " +
                                bitmap.PixelSize.Width +
                                "x" +
                                bitmap.PixelSize.Height +
                                "。";
    }

    [RelayCommand]
    private void AddGumpBuilderLabel()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Text,
            Name = "AddText 1",
            X = 30,
            Y = 30,
            Width = 120,
            Height = 20,
            Hue = 0,
            Text = "Test text",
            Z = 10
        });

        GumpBuilderStatusText = "已添加 AddText。";
    }

    [RelayCommand]
    private void AddGumpBuilderTextEntry()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.TextEntry,
            Name = "AddTextEntry 1",
            X = 62,
            Y = 20,
            Width = 200,
            Height = 20,
            Hue = 10,
            ButtonId = 1, // unk4
            TextId = 1,
            Text = "Default text",
            MaxLength = 20,
            LimitedTextEntry = false,
            Z = 10
        });

        GumpBuilderStatusText = "已添加 AddTextEntry。";
    }

    [RelayCommand]
    private void AddGumpBuilderCroppedText()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.CroppedText,
            Name = "AddCroppedText 1",
            X = 10,
            Y = 10,
            Width = 50,
            Height = 20,
            Hue = 2706,
            Text = "Hello world, this text is cut off somewhere!",
            Z = 10
        });

        GumpBuilderStatusText = "已添加 AddCroppedText。";
    }

    [RelayCommand]
    private void DeleteSelectedGumpBuilderElement()
    {
        if (SelectedGumpBuilderElement == null)
        {
            GumpBuilderStatusText = "未选择构建器元素。";
            return;
        }

        GumpBuilderElements.Remove(SelectedGumpBuilderElement);
        SelectedGumpBuilderElement = null;
        GumpBuilderStatusText = "已删除构建器元素。";
        ExportGumpBuilderUox3JavaScript();
    }

    [RelayCommand]
    private void MoveSelectedGumpBuilderElementUp()
    {
        if (SelectedGumpBuilderElement == null)
        {
            return;
        }

        int index = GumpBuilderElements.IndexOf(SelectedGumpBuilderElement);
        if (index < 0 || index >= GumpBuilderElements.Count - 1)
        {
            return;
        }

        GumpBuilderElements.Move(index, index + 1);
        SelectedGumpBuilderElement.Z++;
        ExportGumpBuilderUox3JavaScript();
    }

    [RelayCommand]
    private void MoveSelectedGumpBuilderElementDown()
    {
        if (SelectedGumpBuilderElement == null)
        {
            return;
        }

        int index = GumpBuilderElements.IndexOf(SelectedGumpBuilderElement);
        if (index <= 0)
        {
            return;
        }

        GumpBuilderElements.Move(index, index - 1);
        SelectedGumpBuilderElement.Z--;
        ExportGumpBuilderUox3JavaScript();
    }

    private void AddGumpBuilderElement(GumpBuilderElement element)
    {
        foreach (GumpBuilderElement existing in GumpBuilderElements)
        {
            existing.IsSelected = false;
        }

        element.IsSelected = true;
        GumpBuilderElements.Add(element);
        SelectedGumpBuilderElement = element;
        WatchGumpBuilderElementForExport(element);
        GumpBuilderStatusText = "已添加 " + element.Type + "。";
        ExportGumpBuilderUox3JavaScript();
    }

    partial void OnSelectedGumpBuilderElementChanged(GumpBuilderElement? value)
    {
        foreach (GumpBuilderElement element in GumpBuilderElements)
        {
            element.IsSelected = ReferenceEquals(element, value);
        }

        WatchGumpBuilderElementForExport(value);
    }

    [RelayCommand]
    private void SelectGumpBuilderElement(GumpBuilderElement? element)
    {
        if (element == null)
        {
            return;
        }

        SelectedGumpBuilderElement = element;
        GumpBuilderStatusText = "已选择 " + element.Type + "。";
    }

    public int SnapGumpBuilderValue(int value)
    {
        if (!GumpBuilderSnapToGrid || GumpBuilderGridSize <= 1)
        {
            return value;
        }

        int gridSize = Math.Max(1, GumpBuilderGridSize);

        return value / gridSize * gridSize;
    }

    [RelayCommand]
    private void ExportGumpBuilderUox3JavaScript()
    {
        GumpBuilderExportText = GumpBuilderExportService.ExportUox3JavaScript(
            GumpBuilderElements,
            GumpBuilderUseMasterGump,
            GumpBuilderMasterGumpId,
            GumpBuilderNoClose,
            GumpBuilderNoDispose,
            GumpBuilderNoMove,
            GumpBuilderNoResize);
    }

    [RelayCommand]
    private void ExportGumpBuilderCSharp()
    {
        GumpBuilderCSharpExportText = GumpBuilderExportService.ExportCSharp(GumpBuilderElements);
        GumpBuilderStatusText = "已生成 C# Gump 代码。";
    }

    [RelayCommand]
    private async Task CopyGumpBuilderCSharpToClipboard()
    {
        if (string.IsNullOrWhiteSpace(GumpBuilderCSharpExportText))
        {
            ExportGumpBuilderCSharp();
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow == null)
        {
            GumpBuilderStatusText = "无法访问剪贴板。";
            return;
        }

        await desktop.MainWindow.Clipboard!.SetTextAsync(GumpBuilderCSharpExportText);
        GumpBuilderStatusText = "已将 C# Gump 代码复制到剪贴板。";
    }

    private void WatchGumpBuilderElementForExport(GumpBuilderElement? element)
    {
        if (exportWatchedGumpBuilderElement != null)
        {
            exportWatchedGumpBuilderElement.PropertyChanged -= GumpBuilderElement_PropertyChanged;
        }

        exportWatchedGumpBuilderElement = element;

        if (exportWatchedGumpBuilderElement != null)
        {
            exportWatchedGumpBuilderElement.PropertyChanged += GumpBuilderElement_PropertyChanged;
        }
    }

    private void GumpBuilderElement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is GumpBuilderElement picInPicElement &&
    picInPicElement.Type == GumpBuilderElementType.PicInPic &&
    picInPicElement.SourceBitmap != null &&
    (e.PropertyName == nameof(GumpBuilderElement.SpriteX) ||
     e.PropertyName == nameof(GumpBuilderElement.SpriteY) ||
     e.PropertyName == nameof(GumpBuilderElement.Width) ||
     e.PropertyName == nameof(GumpBuilderElement.Height)))
        {
            picInPicElement.Bitmap = GumpBuilderRenderService.BuildPicInPicBitmap(
                picInPicElement.SourceBitmap,
                picInPicElement.SpriteX,
                picInPicElement.SpriteY,
                picInPicElement.Width,
                picInPicElement.Height);
        }

        if (sender is GumpBuilderElement tiledElement &&
    tiledElement.Type == GumpBuilderElementType.TiledImage &&
    tiledElement.TileSourceBitmap != null &&
    (e.PropertyName == nameof(GumpBuilderElement.Width) ||
     e.PropertyName == nameof(GumpBuilderElement.Height)))
        {
            tiledElement.Bitmap = GumpBuilderRenderService.BuildTiledGumpBitmap(
                tiledElement.TileSourceBitmap,
                tiledElement.Width,
                tiledElement.Height);
        }

        if (sender is GumpBuilderElement element &&
            element.Type == GumpBuilderElementType.Background &&
            element.BackgroundParts.Count >= 9 &&
            (e.PropertyName == nameof(GumpBuilderElement.Width) ||
             e.PropertyName == nameof(GumpBuilderElement.Height)))
        {
            element.Bitmap = GumpBuilderRenderService.BuildBackgroundBitmap(
                element.BackgroundParts,
                element.Width,
                element.Height);
        }

        if (e.PropertyName == nameof(GumpBuilderElement.Z))
        {
            SortGumpBuilderElementsByZ();
        }

        ExportGumpBuilderUox3JavaScript();
    }

    [RelayCommand]
    private async Task CopyGumpBuilderExportToClipboard()
    {
        if (string.IsNullOrWhiteSpace(GumpBuilderExportText))
        {
            ExportGumpBuilderUox3JavaScript();
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard == null)
        {
            GumpBuilderStatusText = "剪贴板不可用。";
            return;
        }

        await desktop.MainWindow.Clipboard.SetTextAsync(GumpBuilderExportText);
        GumpBuilderStatusText = "已将 UOX3 JavaScript Gump 代码复制到剪贴板。";
    }

    [RelayCommand]
    private void AddGumpBuilderHtml()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Html,
            Name = "AddHTMLGump 1",
            X = 40,
            Y = 15,
            Width = 110,
            Height = 20,
            HasBackground = false,
            HasScrollbar = false,
            Text = "<B>This is a text with HTML</B>",
            Z = 10
        });

        GumpBuilderStatusText = "已添加 AddHTMLGump。";
    }

    [RelayCommand]
    private void AddGumpBuilderCheckbox()
    {
        WriteableBitmap? checkboxBitmap = LoadBuilderGumpBitmap(2706);

        if (checkboxBitmap == null)
        {
            GumpBuilderStatusText = "无法加载复选框 Gump 2706。";
            return;
        }

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Checkbox,
            Name = "AddCheckbox 1",
            X = 10,
            Y = 10,
            Width = checkboxBitmap.PixelSize.Width,
            Height = checkboxBitmap.PixelSize.Height,
            GumpId = 2706,
            ButtonId = 1,
            DefaultStatus = false,
            Bitmap = checkboxBitmap
        });

        GumpBuilderStatusText = "已添加复选框 2706。";
    }

    [RelayCommand]
    private void AddGumpBuilderRadio()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Radio,
            Name = "Radio",
            X = 130,
            Y = 170,
            Width = 20,
            Height = 20,
            GumpId = 208,
            PressedGumpId = 209,
            ButtonId = 1
        });
    }

    [RelayCommand]
    private void AddGumpBuilderTiledImage()
    {
        WriteableBitmap? tileBitmap = LoadBuilderGumpBitmap(2624);

        if (tileBitmap == null)
        {
            GumpBuilderStatusText = "无法加载平铺 Gump 2624。";
            return;
        }

        const int width = 200;
        const int height = 80;

        WriteableBitmap rendered =
            GumpBuilderRenderService.BuildTiledGumpBitmap(tileBitmap, width, height);

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.TiledImage,
            Name = "AddTiledGump 1",
            X = 20,
            Y = 20,
            Width = width,
            Height = height,
            GumpId = 2624,
            TileSourceBitmap = tileBitmap,
            Bitmap = rendered
        });

        GumpBuilderStatusText = "已添加平铺 Gump 2624。";
    }

    [RelayCommand]
    private void LoadSelectedGumpIntoBuilder()
    {
        if (SelectedGump == null || !SelectedGump.IsValid)
        {
            GumpBuilderStatusText = "请先从\"Gump\"标签中选择有效的 Gump。";
            return;
        }

        if (SelectedGumpBitmap == null)
        {
            LoadSelectedGump();
        }

        if (SelectedGumpBitmap == null)
        {
            GumpBuilderStatusText = "选定的 Gump 位图未加载。";
            return;
        }

        int width = SelectedGumpBitmap.PixelSize.Width;
        int height = SelectedGumpBitmap.PixelSize.Height;

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Image,
            Name = "Gump " + SelectedGump.GumpId + " [0x" + SelectedGump.GumpId.ToString("X") + "]",
            X = 20,
            Y = 20,
            Width = width,
            Height = height,
            GumpId = SelectedGump.GumpId,
            Bitmap = SelectedGumpBitmap
        });

        GumpBuilderStatusText = "已添加所选 Gump " + SelectedGump.GumpId + "，原生尺寸为 " + width + "x" + height + "。";
    }

    [RelayCommand]
    private void RefreshGumpBuilderArtPicker()
    {
        GumpBuilderArtPickerEntries.Clear();

        if (ArtEntries.Count == 0)
        {
            LoadArtTab();
        }

        if (ArtEntries.Count == 0)
        {
            GumpBuilderStatusText = "未加载任何美术条目。请先打开 UO 文件夹。";
            return;
        }

        string search = (GumpBuilderArtPickerSearchText ?? string.Empty).Trim();

        IEnumerable<ArtEntry> entries = ArtEntries
            .Where(x => !x.IsFreeSlot);

        if (!string.IsNullOrWhiteSpace(search))
        {
            entries = entries.Where(x =>
                x.ArtId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.ArtId.ToString("X").Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (ArtEntry entry in entries.Take(500))
        {
            GumpBuilderArtPickerEntries.Add(entry);
        }

        GumpBuilderStatusText = "已加载 " + GumpBuilderArtPickerEntries.Count + " 个静态美术条目。";
    }

    [RelayCommand]
    private void AddSelectedArtToGumpBuilder()
    {
        if (SelectedGumpBuilderArtPickerEntry == null)
        {
            GumpBuilderStatusText = "未选择美术。";
            return;
        }

        WriteableBitmap? bitmap = artDataService.LoadBitmap(SelectedGumpBuilderArtPickerEntry);

        if (bitmap == null)
        {
            GumpBuilderStatusText = "加载美术 " + SelectedGumpBuilderArtPickerEntry.ArtId + " 失败。";
            return;
        }

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Item,
            Name = "AddPicture " + SelectedGumpBuilderArtPickerEntry.ArtId +
                   " [0x" + SelectedGumpBuilderArtPickerEntry.ArtId.ToString("X") + "]",
            X = 80,
            Y = 80,
            Width = bitmap.PixelSize.Width,
            Height = bitmap.PixelSize.Height,
            GumpId = SelectedGumpBuilderArtPickerEntry.ArtId,
            Hue = 0,
            Bitmap = bitmap
        });

        ExportGumpBuilderUox3JavaScript();

        GumpBuilderStatusText = "已添加美术 " + SelectedGumpBuilderArtPickerEntry.ArtId + "。";
    }

    [RelayCommand]
    private void AddGumpBuilderButtonTileArt()
    {
        WriteableBitmap? buttonBitmap = LoadBuilderGumpBitmap(5050);

        if (buttonBitmap == null)
        {
            GumpBuilderStatusText = "无法加载按钮 Gump 5050。";
            return;
        }

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.ButtonTileArt,
            Name = "AddButtonTileArt 1",
            X = 290,
            Y = 13,
            Width = buttonBitmap.PixelSize.Width,
            Height = buttonBitmap.PixelSize.Height,
            GumpId = 5050,
            PressedGumpId = 5050,
            ButtonId = 1,
            TileId = 1193,
            Hue = 0,
            TileX = 10,
            TileY = 10,
            Bitmap = buttonBitmap
        });

        GumpBuilderStatusText = "已添加 ButtonTileArt 5050，tile 为 1193。";
    }

    [RelayCommand]
    private void AddGumpBuilderCheckerTrans()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.CheckerTrans,
            Name = "AddCheckerTrans 1",
            X = 0,
            Y = 0,
            Width = 300,
            Height = 220,
            Z = 100
        });

        GumpBuilderStatusText = "已添加棋盘透明区域。";
    }

    partial void OnGumpBuilderMasterGumpIdChanged(int value)
    {
        ExportGumpBuilderUox3JavaScript();
    }

    partial void OnGumpBuilderUseMasterGumpChanged(bool value)
    {
        ExportGumpBuilderUox3JavaScript();
    }

    partial void OnGumpBuilderNoCloseChanged(bool value)
    {
        ExportGumpBuilderUox3JavaScript();
    }

    partial void OnGumpBuilderNoDisposeChanged(bool value)
    {
        ExportGumpBuilderUox3JavaScript();
    }

    partial void OnGumpBuilderNoMoveChanged(bool value)
    {
        ExportGumpBuilderUox3JavaScript();
    }

    partial void OnGumpBuilderNoResizeChanged(bool value)
    {
        ExportGumpBuilderUox3JavaScript();
    }

    [RelayCommand]
    private void AddGumpBuilderGroupStart()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.GroupStart,
            Name = "AddGroup 1",
            X = 20,
            Y = 20,
            Width = 120,
            Height = 24,
            GroupNumber = 1,
            Z = 0
        });

        GumpBuilderStatusText = "已添加单选组开始。";
    }

    [RelayCommand]
    private void AddGumpBuilderGroupEnd()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.GroupEnd,
            Name = "EndGroup",
            X = 20,
            Y = 50,
            Width = 120,
            Height = 24,
            Z = 0
        });

        GumpBuilderStatusText = "已添加单选组结束。";
    }

    [RelayCommand]
    private void AddGumpBuilderPage()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Page,
            Name = "AddPage 1",
            X = 20,
            Y = 80,
            Width = 120,
            Height = 24,
            PageNumber = 1
        });

        GumpBuilderStatusText = "已添加页面标记。";
    }

    [RelayCommand]
    private void AddGumpBuilderItemProperty()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.ItemProperty,
            Name = "AddItemProperty",
            X = 20,
            Y = 110,
            Width = 160,
            Height = 24,
            ItemPropertyObject = "myItem"
        });

        GumpBuilderStatusText = "已添加物品属性标记。";
    }

    [RelayCommand]
    private void AddGumpBuilderClilocToolTip()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.ClilocToolTip,
            Name = "AddToolTip 1015094",
            X = 20,
            Y = 140,
            Width = 180,
            Height = 24,
            ClilocNumber = 1015094
        });

        GumpBuilderStatusText = "已添加 cliloc 工具提示标记。";
    }

    [RelayCommand]
    private void AddGumpBuilderOpenGumpPicker()
    {
        RefreshGumpBuilderPicker();

        GumpBuilderStatusText =
            "Gump 拾取器已打开。已加载条目：" +
            GumpBuilderPickerEntries.Count +
            "。选择一个 Gump，然后单击\"添加所选 Gump\"。";
    }

    [RelayCommand]
    private void AddSelectedPickerGumpToBuilder()
    {
        if (SelectedGumpBuilderPickerEntry == null)
        {
            GumpBuilderStatusText = "未选择拾取器 Gump。";
            return;
        }

        WriteableBitmap? bitmap = LoadBuilderGumpBitmap(SelectedGumpBuilderPickerEntry.GumpId);

        if (bitmap == null)
        {
            return;
        }

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.Image,
            Name = "AddGump " + SelectedGumpBuilderPickerEntry.GumpId +
                   " [0x" + SelectedGumpBuilderPickerEntry.GumpId.ToString("X") + "]",
            X = 80,
            Y = 80,
            Width = bitmap.PixelSize.Width,
            Height = bitmap.PixelSize.Height,
            GumpId = SelectedGumpBuilderPickerEntry.GumpId,
            Hue = 0,
            Bitmap = bitmap
        });

        ExportGumpBuilderUox3JavaScript();

        GumpBuilderStatusText = "已添加 Gump " + SelectedGumpBuilderPickerEntry.GumpId + "。";
    }

    [RelayCommand]
    private void AddGumpBuilderPicInPic()
    {
        WriteableBitmap? sourceBitmap = LoadBuilderGumpBitmap(0x9D3B);

        if (sourceBitmap == null)
        {
            GumpBuilderStatusText = "无法加载 PicInPic 源 Gump 0x9D3B。";
            return;
        }

        const int width = 30;
        const int height = 30;

        WriteableBitmap previewBitmap =
            GumpBuilderRenderService.BuildPicInPicBitmap(sourceBitmap, 0, 0, width, height);

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.PicInPic,
            Name = "AddPicInPic 1",
            X = 10,
            Y = 10,
            Width = width,
            Height = height,
            GumpId = 0x9D3B,
            SpriteX = 0,
            SpriteY = 0,
            SourceBitmap = sourceBitmap,
            Bitmap = previewBitmap
        });

        GumpBuilderStatusText = "已添加 PicInPic Gump 0x9D3B。";
    }

    [RelayCommand]
    private void AddGumpBuilderPageButton()
    {
        WriteableBitmap? normalBitmap = LoadBuilderGumpBitmap(5050);
        WriteableBitmap? pressedBitmap = LoadBuilderGumpBitmap(5051);

        if (normalBitmap == null)
        {
            GumpBuilderStatusText = "无法加载页面按钮 Gump 5050。";
            return;
        }

        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.PageButton,
            Name = "AddPageButton 1",
            X = 290,
            Y = 13,
            Width = normalBitmap.PixelSize.Width,
            Height = normalBitmap.PixelSize.Height,
            GumpId = 5050,
            PressedGumpId = pressedBitmap == null ? 5051 : 5051,
            PageNumber = 1,
            Bitmap = normalBitmap,
            PressedBitmap = pressedBitmap
        });

        GumpBuilderStatusText = "已添加页面按钮 5050 到第 1 页。";
    }

    [RelayCommand]
    private void AddGumpBuilderXmfHtml()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.XmfHtml,
            Name = "AddXMFHTMLGump 1",
            X = 10,
            Y = 10,
            Width = 200,
            Height = 300,
            ClilocNumber = 1,
            HasBorder = true,
            HasScrollbar = true,
            Z = 10
        });

        GumpBuilderStatusText = "已添加 AddXMFHTMLGump。";
    }

    [RelayCommand]
    private void AddGumpBuilderXmfHtmlColor()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.XmfHtmlColor,
            Name = "AddXMFHTMLGumpColor 1",
            X = 10,
            Y = 10,
            Width = 200,
            Height = 300,
            ClilocNumber = 1,
            HasBorder = true,
            HasScrollbar = true,
            RgbColour = 0xFFFFFF,
            Z = 10
        });

        GumpBuilderStatusText = "已添加 AddXMFHTMLGumpColor。";
    }

    [RelayCommand]
    private void AddGumpBuilderXmfHtmlTok()
    {
        AddGumpBuilderElement(new GumpBuilderElement
        {
            Type = GumpBuilderElementType.XmfHtmlTok,
            Name = "AddXMFHTMLTok 1",
            X = 10,
            Y = 10,
            Width = 200,
            Height = 300,
            ClilocNumber = 1060658,
            HasBorder = true,
            HasScrollbar = true,
            RgbColour = 1,
            ClilocArg1 = "99",
            ClilocArg2 = "BottlesOfBeer",
            ClilocArg3 = string.Empty,
            Z = 10
        });

        GumpBuilderStatusText = "已添加 AddXMFHTMLTok。";
    }

    [RelayCommand]
    private async Task CopyGumpBuilderCSharpExportToClipboard()
    {
        if (string.IsNullOrWhiteSpace(GumpBuilderCSharpExportText))
        {
            ExportGumpBuilderCSharp();
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard == null)
        {
            GumpBuilderStatusText = "剪贴板不可用。";
            return;
        }

        await desktop.MainWindow.Clipboard.SetTextAsync(GumpBuilderCSharpExportText);
        GumpBuilderStatusText = "已将 C# Gump 代码复制到剪贴板。";
    }

    [RelayCommand]
    private void ExportGumpBuilderSphere()
    {
        GumpBuilderSphereExportText = GumpBuilderExportService.ExportSphere(
            GumpBuilderElements,
            GumpBuilderSphereName,
            GumpBuilderSphereRevision,
            GumpBuilderNoClose,
            GumpBuilderNoDispose,
            GumpBuilderNoMove);

        GumpBuilderStatusText = "已生成 Sphere Gump 代码。";
    }

    [RelayCommand]
    private async Task CopyGumpBuilderSphereExportToClipboard()
    {
        if (string.IsNullOrWhiteSpace(GumpBuilderSphereExportText))
        {
            ExportGumpBuilderSphere();
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard == null)
        {
            GumpBuilderStatusText = "剪贴板不可用。";
            return;
        }

        await desktop.MainWindow.Clipboard.SetTextAsync(GumpBuilderSphereExportText);
        GumpBuilderStatusText = "已将 Sphere Gump 代码复制到剪贴板。";
    }

    [RelayCommand]
    private void ExportGumpBuilderPol()
    {
        GumpBuilderPolExportText = GumpBuilderExportService.ExportPol(
            GumpBuilderElements,
            GumpBuilderPolName,
            GumpBuilderPolUseDistro,
            GumpBuilderNoClose,
            GumpBuilderNoDispose,
            GumpBuilderNoMove);

        GumpBuilderStatusText = "已生成 POL Gump 代码。";
    }

    [RelayCommand]
    private async Task CopyGumpBuilderPolExportToClipboard()
    {
        if (string.IsNullOrWhiteSpace(GumpBuilderPolExportText))
        {
            ExportGumpBuilderPol();
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard == null)
        {
            GumpBuilderStatusText = "剪贴板不可用。";
            return;
        }

        await desktop.MainWindow.Clipboard.SetTextAsync(GumpBuilderPolExportText);
        GumpBuilderStatusText = "已将 POL Gump 代码复制到剪贴板。";
    }

    [RelayCommand]
    private void ShowGumpBuilderUox3Export()
    {
        ExportGumpBuilderUox3JavaScript();
        GumpBuilderActiveExportKind = "UOX3";
        OnPropertyChanged(nameof(GumpBuilderActiveExportText));
    }

    [RelayCommand]
    private void ShowGumpBuilderCSharpExport()
    {
        ExportGumpBuilderCSharp();
        GumpBuilderActiveExportKind = "CSharp";
        OnPropertyChanged(nameof(GumpBuilderActiveExportText));
    }

    [RelayCommand]
    private void ShowGumpBuilderSphereExport()
    {
        ExportGumpBuilderSphere();
        GumpBuilderActiveExportKind = "Sphere";
        OnPropertyChanged(nameof(GumpBuilderActiveExportText));
    }

    [RelayCommand]
    private void ShowGumpBuilderPolExport()
    {
        ExportGumpBuilderPol();
        GumpBuilderActiveExportKind = "POL";
        OnPropertyChanged(nameof(GumpBuilderActiveExportText));
    }

    [RelayCommand]
    private async Task CopyGumpBuilderActiveExport()
    {
        switch (GumpBuilderActiveExportKind)
        {
            case "UOX3":
                await CopyGumpBuilderExportToClipboard();
                break;

            case "CSharp":
                await CopyGumpBuilderCSharpToClipboard();
                break;

            case "Sphere":
                await CopyGumpBuilderSphereExportToClipboard();
                break;

            case "POL":
                await CopyGumpBuilderPolExportToClipboard();
                break;

            default:
                GumpBuilderStatusText = "未选择导出预览。";
                break;
        }
    }
}