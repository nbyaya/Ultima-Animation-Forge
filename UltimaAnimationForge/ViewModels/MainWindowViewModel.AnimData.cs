using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UltimaAnimationForge.Models;
using UltimaAnimationForge.Services;

namespace UltimaAnimationForge.ViewModels;

public partial class MainWindowViewModel
{
    private sealed class AnimDataExportFile
    {
        public List<AnimDataExportEntry> Data { get; set; } = new();
    }

    private sealed class AnimDataExportEntry
    {
        public int Id { get; set; }
        public byte Unknown { get; set; }
        public byte FrameCount { get; set; }
        public byte FrameInterval { get; set; }
        public byte FrameStart { get; set; }
        public sbyte[] FrameOffsets { get; set; } = new sbyte[64];
    }

    private readonly AnimDataMulService animDataMulService = new();

    public ObservableCollection<AnimDataEntry> AnimDataEntries { get; } = new();

    private readonly Dictionary<int, ArtEntry?> animDataStaticArtEntryCache = new();
    [ObservableProperty]
    private AnimDataEntry? selectedAnimDataEntry;

    [ObservableProperty]
    private AnimDataFrameEntry? selectedAnimDataFrame;

    [ObservableProperty]
    private WriteableBitmap? selectedAnimDataBaseBitmap;

    [ObservableProperty]
    private WriteableBitmap? selectedAnimDataFrameBitmap;

    [ObservableProperty]
    private string animDataSearchText = string.Empty;

    [ObservableProperty]
    private bool showAnimDataMissingArt = true;

    [ObservableProperty]
    private bool showAnimDataWithoutAnimationFlag = true;

    [ObservableProperty]
    private string animDataStatusText = "AnimData not loaded.";

    private DispatcherTimer? animDataPlaybackTimer;

    [ObservableProperty]
    private bool animDataIsAnimating;

    [ObservableProperty]
    private string animDataAddFrameGraphicText = string.Empty;

    [ObservableProperty]
    private bool animDataAddFrameRelative;

    public TileDataEntry? SelectedAnimDataTileDataEntry
    {
        get
        {
            if (SelectedAnimDataEntry == null)
            {
                return null;
            }

            return FindStaticTileDataEntry(SelectedAnimDataEntry.Id);
        }
    }

    public TileDataEntry? SelectedAnimDataFrameTileDataEntry
    {
        get
        {
            if (SelectedAnimDataFrame == null)
            {
                return null;
            }

            return FindStaticTileDataEntry(SelectedAnimDataFrame.GraphicId);
        }
    }

    [RelayCommand]
    private void LoadAnimDataTab()
    {
        string folderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            AnimDataStatusText = "请先选择 UO 文件夹。";
            return;
        }

        if (TileDataEntries.Count == 0)
        {
            LoadTileData();
        }

        if (!artDataService.Initialize(folderPath))
        {
            AnimDataStatusText = "Art 未加载。AnimData 仍然可以加载，但预览可能为空。";
        }

        if (!animDataMulService.Initialize(folderPath))
        {
            AnimDataEntries.Clear();
            SelectedAnimDataEntry = null;
            SelectedAnimDataFrame = null;
            SelectedAnimDataBaseBitmap = null;
            SelectedAnimDataFrameBitmap = null;
            AnimDataStatusText = "找不到 animdata.mul。";
            return;
        }
        animDataStaticArtEntryCache.Clear();
        RebuildAnimDataEntries();
    }

    [RelayCommand]
    private void RebuildAnimDataEntries()
    {
        AnimDataEntries.Clear();

        foreach (AnimDataEntry entry in animDataMulService.AllLoadedEntries)
        {
            DecorateAnimDataEntry(entry);

            if (!ShouldShowAnimDataEntry(entry))
            {
                continue;
            }

            entry.RebuildFrames(
    GetStaticTileName,
    StaticArtExists,
    graphicId => LoadStaticArtBitmap(graphicId));
            AnimDataEntries.Add(entry);
        }

        if (AnimDataEntries.Count > 0)
        {
            SelectedAnimDataEntry = AnimDataEntries[0];
        }
        else
        {
            SelectedAnimDataEntry = null;
            SelectedAnimDataFrame = null;
            SelectedAnimDataBaseBitmap = null;
            SelectedAnimDataFrameBitmap = null;
        }

        AnimDataStatusText = "加载了 " + AnimDataEntries.Count + " 个 AnimData 条目。";
    }

    [RelayCommand]
    private void SaveAnimData()
    {
        string folderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            AnimDataStatusText = "请先选择 UO 文件夹。";
            return;
        }

        try
        {
            animDataMulService.Save(folderPath, animDataMulService.AllLoadedEntries);
            AnimDataStatusText = "已保存 animdata.mul。";
        }
        catch (UnauthorizedAccessException)
        {
            AnimDataStatusText = "保存 animdata.mul 时被拒绝访问。";
        }
        catch (IOException exception)
        {
            AnimDataStatusText = "保存 animdata.mul 时出现 I/O 错误：" + exception.Message;
        }
        catch (Exception exception)
        {
            AnimDataStatusText = "保存 animdata.mul 失败：" + exception.Message;
        }
    }

    [RelayCommand]
    private void CheckAllAnimData()
    {
        foreach (AnimDataEntry entry in AnimDataEntries)
        {
            entry.IsChecked = true;
        }

        AnimDataStatusText = "已勾选 " + AnimDataEntries.Count + " 个 AnimData 条目。";
    }

    [RelayCommand]
    private void UncheckAllAnimData()
    {
        foreach (AnimDataEntry entry in AnimDataEntries)
        {
            entry.IsChecked = false;
        }

        AnimDataStatusText = "已取消勾选所有 AnimData 条目。";
    }

    partial void OnSelectedAnimDataEntryChanged(AnimDataEntry? value)
    {
        SelectedAnimDataBaseBitmap = LoadStaticArtBitmap(value?.Id);

        if (value != null)
        {
            value.RebuildFrames(
    GetStaticTileName,
    StaticArtExists,
    graphicId => LoadStaticArtBitmap(graphicId));
            SelectedAnimDataFrame = value.Frames.FirstOrDefault();
            AnimDataStatusText = value.DisplayText;
        }
        else
        {
            SelectedAnimDataFrame = null;
            SelectedAnimDataFrameBitmap = null;
        }

        OnPropertyChanged(nameof(SelectedAnimDataTileDataEntry));
    }

    partial void OnSelectedAnimDataFrameChanged(AnimDataFrameEntry? value)
    {
        SelectedAnimDataFrameBitmap = LoadStaticArtBitmap(value?.GraphicId);
        OnPropertyChanged(nameof(SelectedAnimDataFrameTileDataEntry));
    }

    partial void OnAnimDataSearchTextChanged(string value)
    {
        RebuildAnimDataEntries();
    }

    partial void OnShowAnimDataMissingArtChanged(bool value)
    {
        RebuildAnimDataEntries();
    }

    partial void OnShowAnimDataWithoutAnimationFlagChanged(bool value)
    {
        RebuildAnimDataEntries();
    }

    private bool ShouldShowAnimDataEntry(AnimDataEntry entry)
    {
        if (!ShowAnimDataMissingArt && !entry.ArtExists)
        {
            return false;
        }

        if (!ShowAnimDataWithoutAnimationFlag && !entry.HasAnimationTileFlag)
        {
            return false;
        }

        string search = AnimDataSearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        if (entry.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (entry.Id.ToString("X").Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (entry.DisplayText.Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return entry.SecondaryText.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void DecorateAnimDataEntry(AnimDataEntry entry)
    {
        TileDataEntry? tileDataEntry = FindStaticTileDataEntry(entry.Id);

        entry.TileName = tileDataEntry?.Name ?? string.Empty;
        entry.ArtExists = StaticArtExists(entry.Id);
        entry.HasAnimationTileFlag = tileDataEntry != null && (tileDataEntry.Flags & 0x01000000UL) != 0;
    }

    private TileDataEntry? FindStaticTileDataEntry(int artId)
    {
        return TileDataEntries.FirstOrDefault(entry =>
            !entry.IsLand &&
            entry.Id == artId);
    }

    private string GetStaticTileName(int artId)
    {
        TileDataEntry? entry = FindStaticTileDataEntry(artId);
        return entry?.Name ?? string.Empty;
    }

    private bool StaticArtExists(int artId)
    {
        return GetAnimDataStaticArtEntry(artId) != null;
    }

    private WriteableBitmap? LoadStaticArtBitmap(int? artId)
    {
        if (!artId.HasValue || artId.Value < 0)
        {
            return null;
        }

        ArtEntry? artEntry = GetAnimDataStaticArtEntry(artId.Value);

        return artEntry == null
            ? null
            : artDataService.LoadBitmap(artEntry);
    }

    private ArtEntry? GetAnimDataStaticArtEntry(int artId)
    {
        if (artId < 0)
        {
            return null;
        }

        if (animDataStaticArtEntryCache.TryGetValue(artId, out ArtEntry? cachedEntry))
        {
            return cachedEntry;
        }

        ArtEntry? artEntry = artDataService.GetStaticEntryById(artId);
        animDataStaticArtEntryCache[artId] = artEntry;

        return artEntry;
    }

    [RelayCommand]
    private void ToggleAnimDataAnimation()
    {
        if (SelectedAnimDataEntry == null || SelectedAnimDataEntry.Frames.Count == 0)
        {
            AnimDataStatusText = "没有 AnimData 帧可以播放。";
            return;
        }

        if (animDataPlaybackTimer == null)
        {
            animDataPlaybackTimer = new DispatcherTimer();
            animDataPlaybackTimer.Tick += AnimDataPlaybackTimer_Tick;
        }

        if (AnimDataIsAnimating)
        {
            animDataPlaybackTimer.Stop();
            AnimDataIsAnimating = false;
            return;
        }

        int delay = Math.Max(1, (int)SelectedAnimDataEntry.FrameInterval) * 100;
        animDataPlaybackTimer.Interval = TimeSpan.FromMilliseconds(delay);
        animDataPlaybackTimer.Start();
        AnimDataIsAnimating = true;
    }

    private void AnimDataPlaybackTimer_Tick(object? sender, EventArgs e)
    {
        if (SelectedAnimDataEntry == null || SelectedAnimDataEntry.Frames.Count == 0)
        {
            animDataPlaybackTimer?.Stop();
            AnimDataIsAnimating = false;
            return;
        }

        int currentIndex = SelectedAnimDataFrame == null
            ? -1
            : SelectedAnimDataEntry.Frames.IndexOf(SelectedAnimDataFrame);

        currentIndex++;

        if (currentIndex >= SelectedAnimDataEntry.Frames.Count)
        {
            currentIndex = 0;
        }

        SelectedAnimDataFrame = SelectedAnimDataEntry.Frames[currentIndex];
    }

    [RelayCommand]
    private void MoveAnimDataFrameUp()
    {
        MoveSelectedAnimDataFrame(-1);
    }

    [RelayCommand]
    private void MoveAnimDataFrameDown()
    {
        MoveSelectedAnimDataFrame(1);
    }

    private void MoveSelectedAnimDataFrame(int direction)
    {
        if (SelectedAnimDataEntry == null || SelectedAnimDataFrame == null)
        {
            return;
        }

        int index = SelectedAnimDataFrame.FrameIndex;
        int targetIndex = index + direction;

        if (index < 0 || targetIndex < 0 || targetIndex >= SelectedAnimDataEntry.FrameCount)
        {
            return;
        }

        (SelectedAnimDataEntry.FrameOffsets[index], SelectedAnimDataEntry.FrameOffsets[targetIndex]) =
            (SelectedAnimDataEntry.FrameOffsets[targetIndex], SelectedAnimDataEntry.FrameOffsets[index]);

        SelectedAnimDataEntry.RebuildFrames(
    GetStaticTileName,
    StaticArtExists,
    graphicId => LoadStaticArtBitmap(graphicId));
        SelectedAnimDataFrame = SelectedAnimDataEntry.Frames[targetIndex];
    }

    [RelayCommand]
    private void RemoveAnimDataFrame()
    {
        if (SelectedAnimDataEntry == null || SelectedAnimDataFrame == null)
        {
            return;
        }

        int removeIndex = SelectedAnimDataFrame.FrameIndex;

        for (int i = removeIndex; i < SelectedAnimDataEntry.FrameCount - 1; i++)
        {
            SelectedAnimDataEntry.FrameOffsets[i] = SelectedAnimDataEntry.FrameOffsets[i + 1];
        }

        SelectedAnimDataEntry.FrameOffsets[SelectedAnimDataEntry.FrameCount - 1] = 0;
        SelectedAnimDataEntry.FrameCount--;

        SelectedAnimDataEntry.RebuildFrames(
     GetStaticTileName,
     StaticArtExists,
     graphicId => LoadStaticArtBitmap(graphicId));

        if (SelectedAnimDataEntry.Frames.Count > 0)
        {
            SelectedAnimDataFrame = SelectedAnimDataEntry.Frames[Math.Min(removeIndex, SelectedAnimDataEntry.Frames.Count - 1)];
        }
        else
        {
            SelectedAnimDataFrame = null;
        }
    }

    [RelayCommand]
    private void AddAnimDataFrame()
    {
        if (SelectedAnimDataEntry == null)
        {
            return;
        }

        if (SelectedAnimDataEntry.FrameCount >= 64)
        {
            AnimDataStatusText = "AnimData 已有 64 个帧。";
            return;
        }

        if (!TryParseAnimDataNumber(AnimDataAddFrameGraphicText, out int value))
        {
            AnimDataStatusText = "输入有效的 Graphic ID 或偏移值。";
            return;
        }

        int offset = AnimDataAddFrameRelative
            ? value
            : value - SelectedAnimDataEntry.Id;

        if (offset < sbyte.MinValue || offset > sbyte.MaxValue)
        {
            AnimDataStatusText = "帧偏移必须在 -128 到 127 之间。";
            return;
        }

        int newIndex = SelectedAnimDataEntry.FrameCount;
        SelectedAnimDataEntry.FrameOffsets[newIndex] = (sbyte)offset;
        SelectedAnimDataEntry.FrameCount++;

        SelectedAnimDataEntry.RebuildFrames(
    GetStaticTileName,
    StaticArtExists,
    graphicId => LoadStaticArtBitmap(graphicId));
        SelectedAnimDataFrame = SelectedAnimDataEntry.Frames[newIndex];
    }

    private static bool TryParseAnimDataNumber(string text, out int value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string clean = text.Trim();

        if (clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(
                clean[2..],
                System.Globalization.NumberStyles.HexNumber,
                null,
                out value);
        }

        return int.TryParse(clean, out value);
    }

    [RelayCommand]
    private async Task ExportAnimData()
    {
        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            AnimDataStatusText = "无法定位主窗口。";
            return;
        }

        List<AnimDataEntry> entriesToExport = AnimDataEntries
            .Where(entry => entry.IsChecked)
            .ToList();

        if (entriesToExport.Count == 0 && SelectedAnimDataEntry != null)
        {
            entriesToExport.Add(SelectedAnimDataEntry);
        }

        if (entriesToExport.Count == 0)
        {
            AnimDataStatusText = "没有选中要导出的 AnimData 条目。";
            return;
        }

        IStorageFile? file = await mainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "导出 AnimData",
                SuggestedFileName = "animdata_export.json",
                FileTypeChoices = new[]
                {
                new FilePickerFileType("AnimData JSON")
                {
                    Patterns = new[] { "*.json" }
                }
                }
            });

        string? path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            AnimDataStatusText = "AnimData 导出已取消。";
            return;
        }

        List<AnimDataExportEntry> exportEntries = entriesToExport
            .Select(entry => new AnimDataExportEntry
            {
                Id = entry.Id,
                Unknown = entry.Unknown,
                FrameCount = entry.FrameCount,
                FrameInterval = entry.FrameInterval,
                FrameStart = entry.FrameStart,
                FrameOffsets = entry.FrameOffsets.ToArray()
            })
            .ToList();

        AnimDataExportFile exportFile = new AnimDataExportFile
        {
            Data = exportEntries
        };

        string json = JsonSerializer.Serialize(
            exportFile,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(path, json);

        AnimDataStatusText = "已导出 " + exportEntries.Count + " 个 AnimData 条目。";
    }

    [RelayCommand]
    private async Task ImportAnimData()
    {
        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            AnimDataStatusText = "无法定位主窗口。";
            return;
        }

        IReadOnlyList<IStorageFile> files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "导入 AnimData",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                new FilePickerFileType("AnimData JSON")
                {
                    Patterns = new[] { "*.json" }
                }
                }
            });

        if (files.Count == 0)
        {
            AnimDataStatusText = "AnimData 导入已取消。";
            return;
        }

        string? path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            AnimDataStatusText = "无效的 AnimData 导入文件。";
            return;
        }

        string json = await File.ReadAllTextAsync(path);

        AnimDataExportFile? importFile;

        try
        {
            importFile = JsonSerializer.Deserialize<AnimDataExportFile>(json);
        }
        catch (Exception exception)
        {
            AnimDataStatusText = "解析 AnimData 导入文件失败：" + exception.Message;
            return;
        }

        List<AnimDataExportEntry>? importedEntries = importFile?.Data;

        if (importedEntries == null || importedEntries.Count == 0)
        {
            AnimDataStatusText = "导入文件中没有找到 AnimData 条目。";
            return;
        }

        int importedCount = 0;

        foreach (AnimDataExportEntry imported in importedEntries)
        {
            if (imported.Id < 0)
            {
                continue;
            }

            AnimDataEntry? existing = animDataMulService.AllLoadedEntries
                .FirstOrDefault(entry => entry.Id == imported.Id);

            if (existing == null)
            {
                existing = new AnimDataEntry
                {
                    Id = imported.Id
                };

                animDataMulService.AddOrReplaceEntry(existing);
            }

            existing.Unknown = imported.Unknown;
            existing.FrameCount = imported.FrameCount > 64 ? (byte)64 : imported.FrameCount;
            existing.FrameInterval = imported.FrameInterval;
            existing.FrameStart = imported.FrameStart;
            existing.FrameOffsets = new sbyte[64];

            if (imported.FrameOffsets != null)
            {
                for (int i = 0; i < existing.FrameOffsets.Length && i < imported.FrameOffsets.Length; i++)
                {
                    existing.FrameOffsets[i] = imported.FrameOffsets[i];
                }
            }

            DecorateAnimDataEntry(existing);
            existing.RebuildFrames(
    GetStaticTileName,
    StaticArtExists,
    graphicId => LoadStaticArtBitmap(graphicId));

            importedCount++;
        }

        RebuildAnimDataEntries();

        AnimDataStatusText = "已导入 " + importedCount + " 个 AnimData 条目。点击「保存 AnimData」以写入 animdata.mul。";
    }
}