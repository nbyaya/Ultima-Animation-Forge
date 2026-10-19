using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using UltimaAnimationForge.Models;
using UltimaAnimationForge.Services;

namespace UltimaAnimationForge.ViewModels;

public partial class MainWindowViewModel
{

    public ObservableCollection<HueEditorColorSlot> HueOriginalColorSlots { get; } = new();

    public ObservableCollection<string> HuePresetOptions { get; } = new()
{
    "布料",
    "金属",
    "发光",
    "火焰",
    "冰冻",
    "毒素",
    "阴影",
    "黄金",
    "骨头",
    "皮革"
};

    [ObservableProperty]
    private string selectedHuePreset = "布料";

    public ICommand ApplyHuePresetCommand { get; private set; } = null!;

    [ObservableProperty]
    private WriteableBitmap? huePreviewBitmap;

    [ObservableProperty]
    private int huePreviewArtId = 0x0EED;

    [ObservableProperty]
    private int hueEditorReplaceSourceHueId;

    [ObservableProperty]
    private bool hueEditorShowFreeOnly;

    public string HueEditorFreeSlotText => GetFreeHueSlots().Count + " 个空闲槽位";

    private string loadedHueEditorFilePath = string.Empty;
    private List<HueEditorEntry> allHueEditorEntries = new();

    public ObservableCollection<HueEditorEntry> HueEditorEntries { get; } = new();

    [ObservableProperty]
    private HueEditorEntry? selectedHueEditorEntry;

    [ObservableProperty]
    private HueEditorColorSlot? selectedHueEditorColor;

    [ObservableProperty]
    private string hueEditorSearchText = string.Empty;

    [ObservableProperty]
    private string hueEditorStatusText = "打开 UO 文件夹或加载 hues.mul。";

    [ObservableProperty]
    private byte hueEditorRed;

    [ObservableProperty]
    private byte hueEditorGreen;

    [ObservableProperty]
    private byte hueEditorBlue;

    public ICommand CreateGuidedHueCommand { get; private set; } = null!;
    public ICommand ShowHueEditorCommand { get; private set; } = null!;
    public ICommand LoadHueEditorCommand { get; private set; } = null!;
    public ICommand SaveHueEditorCommand { get; private set; } = null!;
    public ICommand RevertHueEditorCommand { get; private set; } = null!;
    public ICommand ApplyHueEditorColorCommand { get; private set; } = null!;
    public ICommand MakeHueEditorGradientCommand { get; private set; } = null!;
    public ICommand CopyHueEditorNameCommand { get; private set; } = null!;
    public ICommand SelectNextFreeHueSlotCommand { get; private set; } = null!;
    public ICommand ExportSelectedHueCommand { get; private set; } = null!;
    public ICommand ImportReplaceSelectedHueCommand { get; private set; } = null!;
    public ICommand ReplaceHueFromIndexCommand { get; private set; } = null!;
    public ICommand RefreshHuePreviewCommand { get; private set; } = null!;

    public string HueEditorLoadedFileText =>
        string.IsNullOrWhiteSpace(loadedHueEditorFilePath)
            ? "未加载 hues.mul"
            : Path.GetFileName(loadedHueEditorFilePath);

    public string HueEditorCountText => HueEditorEntries.Count + " / " + allHueEditorEntries.Count + " 个色调";

    public IBrush HueEditorSelectedColorBrush =>
        SelectedHueEditorColor == null
            ? new SolidColorBrush(Color.Parse("#20242B"))
            : new SolidColorBrush(SelectedHueEditorColor.Color);

    private void InitializeHueEditorCommands()
    {
        ShowHueEditorCommand = new RelayCommand(() => ActiveToolTab = MainToolTab.Hues);
        LoadHueEditorCommand = new AsyncRelayCommand(LoadHueEditorAsync);
        SaveHueEditorCommand = new RelayCommand(SaveHueEditor);
        RevertHueEditorCommand = new RelayCommand(RevertHueEditor);
        ApplyHueEditorColorCommand = new RelayCommand(ApplyHueEditorColor);
        MakeHueEditorGradientCommand = new RelayCommand(MakeHueEditorGradient);
        CopyHueEditorNameCommand = new RelayCommand(CopyHueEditorNameToSearch);
        CreateGuidedHueCommand = new RelayCommand(CreateGuidedHue);
        SelectNextFreeHueSlotCommand = new RelayCommand(SelectNextFreeHueSlot);
        ExportSelectedHueCommand = new AsyncRelayCommand(ExportSelectedHueAsync);
        ImportReplaceSelectedHueCommand = new AsyncRelayCommand(ImportReplaceSelectedHueAsync);
        ReplaceHueFromIndexCommand = new RelayCommand(ReplaceHueFromIndex);
        RefreshHuePreviewCommand = new RelayCommand(UpdateHueArtPreview);
        ApplyHuePresetCommand = new RelayCommand(ApplyHuePreset);
        
        // 初始化计算属性的显示值
        OnPropertyChanged(nameof(HueEditorFreeSlotText));
        OnPropertyChanged(nameof(HueEditorLoadedFileText));
        OnPropertyChanged(nameof(HueEditorCountText));
    }

    private void CaptureOriginalHuePreview()
    {
        HueOriginalColorSlots.Clear();

        if (SelectedHueEditorEntry == null)
        {
            return;
        }

        foreach (HueEditorColorSlot slot in SelectedHueEditorEntry.Colors)
        {
            HueOriginalColorSlots.Add(new HueEditorColorSlot
            {
                Index = slot.Index,
                RawValue = slot.RawValue,
                Color = slot.Color
            });
        }
    }

    partial void OnHueEditorShowFreeOnlyChanged(bool value) => RefreshHueEditorFilter();

    private async Task LoadHueEditorAsync()
    {
        Window? owner = GetMainWindow();
        if (owner == null)
        {
            StatusText = "无法定位主窗口。";
            return;
        }

        string hueFilePath = await FindOrPromptForHueFileAsync(owner);
        if (string.IsNullOrWhiteSpace(hueFilePath) || !File.Exists(hueFilePath))
        {
            HueEditorStatusText = "色调加载已取消。";
            return;
        }

        LoadHueEditorFromPath(hueFilePath);
    }

    private void LoadHueEditorFromPath(string hueFilePath)
    {
        try
        {
            List<HueEditorEntry> loaded = hueDataService.LoadHueEditorEntries(hueFilePath);
            if (loaded.Count == 0)
            {
                HueEditorStatusText = "hues.mul 中未找到任何色调。";
                return;
            }

            loadedHueEditorFilePath = hueFilePath;
            currentHueFilePath = hueFilePath;
            cachedHueEntries = hueDataService.LoadHueEntries(hueFilePath);
            allHueEditorEntries = loaded;

            RefreshHueEditorFilter();

            SelectedHueEditorEntry = HueEditorEntries.FirstOrDefault();
            SelectedHueEditorColor = SelectedHueEditorEntry?.Colors.FirstOrDefault();

            HueEditorStatusText = "已从 " + Path.GetFileName(hueFilePath) + " 加载 " + allHueEditorEntries.Count + " 个色调。";
            OnPropertyChanged(nameof(HueEditorLoadedFileText));
            OnPropertyChanged(nameof(HueEditorCountText));
        }
        catch (Exception exception)
        {
            HueEditorStatusText = "加载 hues.mul 失败: " + exception.Message;
        }
    }

    private void RefreshHueEditorFilter()
    {
        string search = (HueEditorSearchText ?? string.Empty).Trim();

        IEnumerable<HueEditorEntry> filtered = allHueEditorEntries;

        if (HueEditorShowFreeOnly)
        {
            filtered = filtered.Where(IsFreeHueSlot);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(entry =>
                entry.HueId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                entry.HexText.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(entry.Name) && entry.Name.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        HueEditorEntries.Clear();
        foreach (HueEditorEntry entry in filtered)
        {
            HueEditorEntries.Add(entry);
        }

        if (SelectedHueEditorEntry == null || !HueEditorEntries.Contains(SelectedHueEditorEntry))
        {
            SelectedHueEditorEntry = HueEditorEntries.FirstOrDefault();
        }

        OnPropertyChanged(nameof(HueEditorFreeSlotText));
        OnPropertyChanged(nameof(HueEditorCountText));
    }

    private void SaveHueEditor()
    {
        if (string.IsNullOrWhiteSpace(loadedHueEditorFilePath))
        {
            HueEditorStatusText = "未加载 hues.mul。";
            return;
        }

        try
        {
            hueDataService.SaveHueEditorEntries(loadedHueEditorFilePath, allHueEditorEntries);
            cachedHueEntries = hueDataService.LoadHueEntries(loadedHueEditorFilePath);
            HueEditorStatusText = "已保存 hues.mul。如果不存在备份文件，已创建 hues.mul.bak。";
        }
        catch (Exception exception)
        {
            HueEditorStatusText = "保存 hues.mul 失败: " + exception.Message;
        }
    }

    private void RevertHueEditor()
    {
        if (string.IsNullOrWhiteSpace(loadedHueEditorFilePath) || !File.Exists(loadedHueEditorFilePath))
        {
            HueEditorStatusText = "未加载 hues.mul。";
            return;
        }

        LoadHueEditorFromPath(loadedHueEditorFilePath);
    }

    private void ApplyHueEditorColor()
    {
        if (SelectedHueEditorColor == null)
        {
            return;
        }

        Color color = Color.FromRgb(HueEditorRed, HueEditorGreen, HueEditorBlue);
        SelectedHueEditorColor.Color = color;
        SelectedHueEditorColor.RawValue = HueDataService.ConvertAvaloniaColorToUoColor(color);
        OnPropertyChanged(nameof(HueEditorSelectedColorBrush));
        HueEditorStatusText = "已更新颜色 " + SelectedHueEditorColor.IndexText + "。";
    }

    private void MakeHueEditorGradient()
    {
        if (SelectedHueEditorEntry == null)
        {
            return;
        }

        int count = SelectedHueEditorEntry.Colors.Count;
        if (count == 0)
        {
            return;
        }

        Color end = Color.FromRgb(HueEditorRed, HueEditorGreen, HueEditorBlue);

        for (int i = 0; i < count; i++)
        {
            double amount = count <= 1 ? 1.0 : i / (double)(count - 1);
            byte r = (byte)Math.Round(end.R * amount);
            byte g = (byte)Math.Round(end.G * amount);
            byte b = (byte)Math.Round(end.B * amount);
            Color color = Color.FromRgb(r, g, b);

            SelectedHueEditorEntry.Colors[i].Color = color;
            SelectedHueEditorEntry.Colors[i].RawValue = HueDataService.ConvertAvaloniaColorToUoColor(color);
        }

        HueEditorStatusText = "已为色调 " + SelectedHueEditorEntry.HueId + " 生成梯度。";
    }

    private void CopyHueEditorNameToSearch()
    {
        if (SelectedHueEditorEntry != null)
        {
            HueEditorSearchText = SelectedHueEditorEntry.Name;
        }
    }

    partial void OnHueEditorSearchTextChanged(string value) => RefreshHueEditorFilter();

    partial void OnSelectedHueEditorEntryChanged(HueEditorEntry? value)
    {
        SelectedHueEditorColor = value?.Colors.FirstOrDefault();
        CaptureOriginalHuePreview();
        UpdateHueArtPreview();
    }

    partial void OnSelectedHueEditorColorChanged(HueEditorColorSlot? value)
    {
        if (value == null)
        {
            return;
        }

        HueEditorRed = value.Color.R;
        HueEditorGreen = value.Color.G;
        HueEditorBlue = value.Color.B;
        OnPropertyChanged(nameof(HueEditorSelectedColorBrush));
    }

    partial void OnHuePreviewArtIdChanged(int value)
    {
        UpdateHueArtPreview();
    }

    private void CreateGuidedHue()
    {
        if (SelectedHueEditorEntry == null)
        {
            HueEditorStatusText = "请先选择一个空/自定义色调槽位。";
            return;
        }

        Color baseColor = Color.FromRgb(HueEditorRed, HueEditorGreen, HueEditorBlue);

        SelectedHueEditorEntry.Name = string.IsNullOrWhiteSpace(SelectedHueEditorEntry.Name)
            ? "自定义色调 " + SelectedHueEditorEntry.HueId
            : SelectedHueEditorEntry.Name;

        SelectedHueEditorEntry.TableStart = 1;
        SelectedHueEditorEntry.TableEnd = 31;

        for (int i = 0; i < SelectedHueEditorEntry.Colors.Count; i++)
        {
            double t = i / 31.0;

            // Better in-game ramp:
            // starts dark, builds body color, then slightly softens highlights
            double brightness = 0.10 + (t * 0.90);
            double highlightSoftener = t > 0.75 ? 1.0 - ((t - 0.75) * 0.20) : 1.0;

            byte r = ClampByte(baseColor.R * brightness * highlightSoftener);
            byte g = ClampByte(baseColor.G * brightness * highlightSoftener);
            byte b = ClampByte(baseColor.B * brightness * highlightSoftener);

            // Keep the first few slots from becoming dead black in-game.
            if (i > 0)
            {
                r = (byte)Math.Max((int)r, 8);
                g = (byte)Math.Max((int)g, 8);
                b = (byte)Math.Max((int)b, 8);
            }

            Color color = Color.FromRgb(r, g, b);

            SelectedHueEditorEntry.Colors[i].Color = color;
            SelectedHueEditorEntry.Colors[i].RawValue = HueDataService.ConvertAvaloniaColorToUoColor(color);
        }

        SelectedHueEditorColor = SelectedHueEditorEntry.Colors.FirstOrDefault();
        HueEditorStatusText = "已为色调 " + SelectedHueEditorEntry.HueId + " 创建导引色相。准备就绪后保存 hues.mul。";
    }

    private static byte ClampByte(double value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)Math.Round(value);
    }

    private List<HueEditorEntry> GetFreeHueSlots()
    {
        return allHueEditorEntries
            .Where(IsFreeHueSlot)
            .ToList();
    }

    private bool IsFreeHueSlot(HueEditorEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(entry.Name))
        {
            return false;
        }

        return entry.Colors.All(slot =>
            slot.RawValue == 0 ||
            slot.RawValue == 1 ||
            slot.RawValue == 0x0001 ||
            slot.RawValue == 0x8000);
    }

    private void SelectNextFreeHueSlot()
    {
        HueEditorEntry? freeSlot = GetFreeHueSlots().FirstOrDefault();

        if (freeSlot == null)
        {
            HueEditorStatusText = "未找到空闲色调槽位。";
            return;
        }

        HueEditorShowFreeOnly = false;
        HueEditorSearchText = freeSlot.HueId.ToString();

        RefreshHueEditorFilter();

        SelectedHueEditorEntry = freeSlot;
        SelectedHueEditorColor = freeSlot.Colors.FirstOrDefault();

        HueEditorStatusText = "已选择空闲色调槽位 " + freeSlot.HueId + "。";
    }

    private async Task ExportSelectedHueAsync()
    {
        if (SelectedHueEditorEntry == null)
        {
            HueEditorStatusText = "请选择要导出的色调。";
            return;
        }

        Window? owner = GetMainWindow();
        if (owner == null)
        {
            HueEditorStatusText = "无法定位主窗口。";
            return;
        }

        IStorageFile? file = await owner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "导出色调",
                SuggestedFileName = "Hue " + SelectedHueEditorEntry.HueId + ".txt",
                FileTypeChoices = new[]
                {
                new FilePickerFileType("色调文本文件")
                {
                    Patterns = new[] { "*.txt" }
                }
                }
            });

        string? path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            HueEditorStatusText = "色调导出已取消。";
            return;
        }

        hueDataService.ExportHueText(path, SelectedHueEditorEntry);
        HueEditorStatusText = "已将色调 " + SelectedHueEditorEntry.HueId + " 导出到 " + Path.GetFileName(path) + "。";
    }

    private async Task ImportReplaceSelectedHueAsync()
    {
        if (SelectedHueEditorEntry == null)
        {
            HueEditorStatusText = "请先选择要替换的色调槽位。";
            return;
        }

        Window? owner = GetMainWindow();
        if (owner == null)
        {
            HueEditorStatusText = "无法定位主窗口。";
            return;
        }

        IReadOnlyList<IStorageFile> files = await owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "导入色调文本",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                new FilePickerFileType("色调文本文件")
                {
                    Patterns = new[] { "*.txt" }
                }
                }
            });

        if (files.Count == 0)
        {
            HueEditorStatusText = "色调导入已取消。";
            return;
        }

        string? path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            HueEditorStatusText = "选中的色调文件无效。";
            return;
        }

        HueEditorEntry imported = hueDataService.ImportHueText(path);
        CopyHueData(imported, SelectedHueEditorEntry, keepTargetHueId: true);

        SelectedHueEditorColor = SelectedHueEditorEntry.Colors.FirstOrDefault();
        HueEditorStatusText = "已从 " + Path.GetFileName(path) + " 替换色调 " + SelectedHueEditorEntry.HueId + "。准备就绪后保存 hues.mul。";
    }

    private void ReplaceHueFromIndex()
    {
        if (SelectedHueEditorEntry == null)
        {
            HueEditorStatusText = "请先选择要替换的色调槽位。";
            return;
        }

        HueEditorEntry? source = allHueEditorEntries.FirstOrDefault(x => x.HueId == HueEditorReplaceSourceHueId);
        if (source == null)
        {
            HueEditorStatusText = "未找到源色调 " + HueEditorReplaceSourceHueId + "。";
            return;
        }

        if (ReferenceEquals(source, SelectedHueEditorEntry))
        {
            HueEditorStatusText = "源色调和目标色调相同。";
            return;
        }

        CopyHueData(source, SelectedHueEditorEntry, keepTargetHueId: true);

        SelectedHueEditorColor = SelectedHueEditorEntry.Colors.FirstOrDefault();
        HueEditorStatusText = "已将色调 " + source.HueId + " 复制到色调 " + SelectedHueEditorEntry.HueId + "。准备就绪后保存 hues.mul。";
    }

    private static void CopyHueData(HueEditorEntry source, HueEditorEntry target, bool keepTargetHueId)
    {
        int targetHueId = target.HueId;

        target.Name = source.Name;
        target.TableStart = source.TableStart;
        target.TableEnd = source.TableEnd;

        target.Colors.Clear();

        foreach (HueEditorColorSlot slot in source.Colors)
        {
            target.Colors.Add(new HueEditorColorSlot
            {
                Index = slot.Index,
                RawValue = slot.RawValue,
                Color = slot.Color
            });
        }

        if (keepTargetHueId)
        {
            target.HueId = targetHueId;
        }
    }

    private void UpdateHueArtPreview()
    {
        try
        {
            if (SelectedHueEditorEntry == null)
            {
                return;
            }

            if (!artDataService.Initialize(GetCurrentFolderPath()))
            {
                HueEditorStatusText = "艺术文件未加载。请先打开 UO 文件夹。";
                return;
            }

            ArtEntry? artEntry = artDataService.GetStaticEntryById(HuePreviewArtId);
            if (artEntry == null)
            {
                HueEditorStatusText = "无法找到静态艺术 0x" + HuePreviewArtId.ToString("X4") + "。";
                return;
            }

            WriteableBitmap? source = artDataService.LoadBitmap(artEntry);
            if (source == null)
            {
                HueEditorStatusText = "无法加载静态艺术 0x" + HuePreviewArtId.ToString("X4") + "。";
                return;
            }

            HuePreviewBitmap = ApplyHueEditorEntryToBitmap(source, SelectedHueEditorEntry);
            HueEditorStatusText = "已使用静态艺术 0x" + HuePreviewArtId.ToString("X4") + " 更新艺术预览。";
        }
        catch (Exception exception)
        {
            HueEditorStatusText = "预览失败: " + exception.Message;
        }
    }

    private static WriteableBitmap BuildHuePreviewTestBitmap(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = ((y * width) + x) * 4;

                byte value = (byte)Math.Clamp((x + y) * 255 / Math.Max(1, width + height - 2), 0, 255);

                pixels[offset + 0] = value;
                pixels[offset + 1] = value;
                pixels[offset + 2] = value;
                pixels[offset + 3] = 255;
            }
        }

        return CreateHuePreviewBitmap(width, height, pixels);
    }

    private static WriteableBitmap ApplyHueEditorEntryToBitmap(WriteableBitmap sourceBitmap, HueEditorEntry hueEntry)
    {
        HuePreviewPixels pixels = ReadHuePreviewPixels(sourceBitmap);
        byte[] outputPixels = new byte[pixels.Pixels.Length];

        Buffer.BlockCopy(pixels.Pixels, 0, outputPixels, 0, pixels.Pixels.Length);

        for (int y = 0; y < pixels.Height; y++)
        {
            for (int x = 0; x < pixels.Width; x++)
            {
                int offset = ((y * pixels.Width) + x) * 4;

                byte blue = outputPixels[offset + 0];
                byte green = outputPixels[offset + 1];
                byte red = outputPixels[offset + 2];
                byte alpha = outputPixels[offset + 3];

                if (alpha == 0)
                {
                    continue;
                }

                int brightness = (red + green + blue) / 3;
                int hueIndex = (int)Math.Round((brightness / 255.0) * 31.0);
                hueIndex = Math.Clamp(hueIndex, 0, 31);

                if (hueIndex >= hueEntry.Colors.Count)
                {
                    continue;
                }

                Color hueColor = hueEntry.Colors[hueIndex].Color;

                outputPixels[offset + 0] = hueColor.B;
                outputPixels[offset + 1] = hueColor.G;
                outputPixels[offset + 2] = hueColor.R;
                outputPixels[offset + 3] = alpha;
            }
        }

        return CreateHuePreviewBitmap(pixels.Width, pixels.Height, outputPixels);
    }

    private static HuePreviewPixels ReadHuePreviewPixels(WriteableBitmap bitmap)
    {
        using ILockedFramebuffer framebuffer = bitmap.Lock();

        int width = framebuffer.Size.Width;
        int height = framebuffer.Size.Height;
        int sourceRowBytes = framebuffer.RowBytes;
        int targetRowBytes = width * 4;

        byte[] sourceBytes = new byte[sourceRowBytes * height];
        Marshal.Copy(framebuffer.Address, sourceBytes, 0, sourceBytes.Length);

        byte[] packedBytes = new byte[targetRowBytes * height];

        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(
                sourceBytes,
                y * sourceRowBytes,
                packedBytes,
                y * targetRowBytes,
                targetRowBytes);
        }

        return new HuePreviewPixels(width, height, packedBytes);
    }

    private static WriteableBitmap CreateHuePreviewBitmap(int width, int height, byte[] pixels)
    {
        WriteableBitmap bitmap = new(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using ILockedFramebuffer framebuffer = bitmap.Lock();
        Marshal.Copy(pixels, 0, framebuffer.Address, pixels.Length);

        return bitmap;
    }

    private sealed class HuePreviewPixels
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; }

        public HuePreviewPixels(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }
    }

    private void ApplyHuePreset()
    {
        if (SelectedHueEditorEntry == null)
        {
            HueEditorStatusText = "请先选择一个色调槽位。";
            return;
        }

        Color baseColor = GetPresetBaseColor(SelectedHuePreset);
        if (SelectedHuePreset == "布料" || SelectedHuePreset == "金属" || SelectedHuePreset == "发光")
        {
            baseColor = Color.FromRgb(HueEditorRed, HueEditorGreen, HueEditorBlue);
        }

        SelectedHueEditorEntry.Name = string.IsNullOrWhiteSpace(SelectedHueEditorEntry.Name)
            ? SelectedHuePreset + " 色调 " + SelectedHueEditorEntry.HueId
            : SelectedHueEditorEntry.Name;

        SelectedHueEditorEntry.TableStart = 1;
        SelectedHueEditorEntry.TableEnd = 31;

        for (int i = 0; i < SelectedHueEditorEntry.Colors.Count; i++)
        {
            double t = i / 31.0;
            Color color = BuildPresetColor(baseColor, SelectedHuePreset, t);

            SelectedHueEditorEntry.Colors[i].Color = color;
            SelectedHueEditorEntry.Colors[i].RawValue = HueDataService.ConvertAvaloniaColorToUoColor(color);
        }

        SelectedHueEditorColor = SelectedHueEditorEntry.Colors.FirstOrDefault();
        UpdateHueArtPreview();

        HueEditorStatusText = "已将 " + SelectedHuePreset + " 预设应用到色调 " + SelectedHueEditorEntry.HueId + "。";
    }

    private static Color GetPresetBaseColor(string preset)
    {
        return preset switch
        {
            "火焰" => Color.FromRgb(235, 72, 18),
            "冰冻" => Color.FromRgb(90, 190, 255),
            "毒素" => Color.FromRgb(70, 210, 65),
            "阴影" => Color.FromRgb(95, 45, 135),
            "黄金" => Color.FromRgb(235, 175, 45),
            "骨头" => Color.FromRgb(210, 195, 150),
            "皮革" => Color.FromRgb(130, 75, 35),
            _ => Color.FromRgb(160, 160, 160)
        };
    }

    private static Color BuildPresetColor(Color baseColor, string preset, double t)
    {
        double curve = preset switch
        {
            "金属" => Math.Pow(t, 1.35),
            "发光" => Math.Pow(t, 0.55),
            "阴影" => Math.Pow(t, 1.8),
            "骨头" => Math.Pow(t, 0.85),
            _ => t
        };

        double min = preset switch
        {
            "发光" => 0.18,
            "阴影" => 0.05,
            "金属" => 0.08,
            _ => 0.10
        };

        double brightness = min + (curve * (1.0 - min));

        double desaturate = preset switch
        {
            "金属" => 0.25 + (t * 0.35),
            "骨头" => 0.35,
            "皮革" => 0.10,
            _ => 0.0
        };

        double r = baseColor.R * brightness;
        double g = baseColor.G * brightness;
        double b = baseColor.B * brightness;

        if (desaturate > 0)
        {
            double gray = (r + g + b) / 3.0;
            r = Lerp(r, gray, desaturate);
            g = Lerp(g, gray, desaturate);
            b = Lerp(b, gray, desaturate);
        }

        if (preset == "火焰" && t > 0.72)
        {
            r = Lerp(r, 255, (t - 0.72) * 1.2);
            g = Lerp(g, 220, (t - 0.72) * 0.8);
        }

        if (preset == "发光" && t > 0.65)
        {
            r = Lerp(r, 255, (t - 0.65) * 0.8);
            g = Lerp(g, 255, (t - 0.65) * 0.8);
            b = Lerp(b, 255, (t - 0.65) * 0.8);
        }

        return Color.FromRgb(ClampByte(r), ClampByte(g), ClampByte(b));
    }

    private static double Lerp(double a, double b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return a + ((b - a) * t);
    }
}
