using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using UltimaAnimationForge.Models;
using UltimaAnimationForge.Services;

namespace UltimaAnimationForge.ViewModels;

public partial class EquipmentBinderToolViewModel : ViewModelBase
{
    private readonly EquipmentBinderToolHostContext hostContext;

    private readonly EquipmentBinderService equipmentBinderService = new();
    private readonly UoIndexedMulImageService equipmentImageService = new();
    private readonly List<EquipmentArtPickerEntry> allEquipmentArtPickerEntries = new();
    private readonly List<EquipmentGumpPickerEntry> allEquipmentGumpPickerEntries = new();
    private const int EquipmentPickerPageSize = 100;

    public EquipmentBinderToolViewModel(EquipmentBinderToolHostContext hostContext)
    {
        this.hostContext = hostContext;

        AutoFillEquipmentBinderDisplayBodyCommand = new RelayCommand(AutoFillEquipmentBinderDisplayBody);
        LoadEquipmentArtPickerCommand = new RelayCommand(LoadEquipmentArtPicker);
        LoadEquipmentGumpPickerCommand = new RelayCommand(LoadEquipmentGumpPicker);
        BuildEquipmentBinderPreviewCommand = new RelayCommand(BuildEquipmentBinderPreview);
        ApplyEquipmentBinderCommand = new RelayCommand(ApplyEquipmentBinder);
    }

    public ICommand AutoFillEquipmentBinderDisplayBodyCommand { get; }
    public ICommand LoadEquipmentArtPickerCommand { get; }
    public ICommand LoadEquipmentGumpPickerCommand { get; }
    public ICommand BuildEquipmentBinderPreviewCommand { get; }
    public ICommand ApplyEquipmentBinderCommand { get; }

    [ObservableProperty] private string equipmentArtPickerSearchText = string.Empty;
    [ObservableProperty] private string equipmentGumpPickerSearchText = string.Empty;
    [ObservableProperty] private int equipmentBinderItemArtId;
    [ObservableProperty] private int equipmentBinderDisplayBodyId;
    [ObservableProperty] private int equipmentBinderAnimationBodyId;
    [ObservableProperty] private string equipmentBinderName = string.Empty;
    [ObservableProperty] private string equipmentBinderBodyDefLine = string.Empty;
    [ObservableProperty] private string equipmentBinderBodyConvLine = string.Empty;
    [ObservableProperty] private string equipmentBinderMobTypeLine = string.Empty;
    [ObservableProperty] private string equipmentBinderSummary = "选择一个空闲的 MUL body ID，填写装备字段，然后构建预览。";
    [ObservableProperty] private bool equipmentBinderBodyDefExists;
    [ObservableProperty] private bool equipmentBinderBodyConvExists;
    [ObservableProperty] private bool equipmentBinderMobTypeExists;
    [ObservableProperty] private bool equipmentBinderOverwriteBodyDef = true;
    [ObservableProperty] private bool equipmentBinderOverwriteMobType = true;
    [ObservableProperty] private bool equipmentBinderUpdateTileDataAnimation = true;
    [ObservableProperty] private string equipmentBinderTileDataLine = string.Empty;
    [ObservableProperty] private EquipmentArtPickerEntry? selectedEquipmentArtPickerEntry;
    [ObservableProperty] private EquipmentGumpPickerEntry? selectedEquipmentGumpPickerEntry;
    [ObservableProperty] private string equipmentPickerStatusText = "美术/Gump 拾取器未加载。";

    public ObservableCollection<EquipmentArtPickerEntry> EquipmentArtPickerEntries { get; } = new();
    public ObservableCollection<EquipmentGumpPickerEntry> EquipmentGumpPickerEntries { get; } = new();

    public string EquipmentBinderTargetSlotText
    {
        get
        {
            MulSlotEntry? selectedMulSlot = hostContext.SelectedMulSlotProvider();
            return selectedMulSlot == null
                ? "未选择空闲的 MUL body ID。"
                : selectedMulSlot.FileName + " | " + selectedMulSlot.TypeLetter + " | Slot " + selectedMulSlot.BodyIndex +
                  " | FileType " + selectedMulSlot.FileType + " | AnimLength " + selectedMulSlot.AnimLength;
        }
    }

    public string EquipmentBinderExistsText =>
        "body.def: " + (EquipmentBinderBodyDefExists ? "存在/更新" : "新增") +
        " | bodyconv.def: " + (EquipmentBinderBodyConvExists ? "存在/跳过" : "新增") +
        " | mobtypes.txt: " + (EquipmentBinderMobTypeExists ? "存在/更新" : "新增");

    private void SetStatus(string message)
    {
        hostContext.StatusCallback?.Invoke(message);
    }

    public void RefreshHostSelection()
    {
        OnPropertyChanged(nameof(EquipmentBinderTargetSlotText));
        BuildEquipmentBinderPreview();
    }


    private void AutoFillEquipmentBinderDisplayBody()
    {
        string currentFolderPath = hostContext.CurrentFolderPathProvider();
        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            SetStatus("请先打开有效的 UO 文件夹。");
            return;
        }

        int nextFree = equipmentBinderService.FindNextFreeDisplayBodyId(currentFolderPath, 4000);
        if (nextFree < 0)
        {
            SetStatus("无法找到 65534 以下的空闲显示 Body/Gump ID。");
            return;
        }

        EquipmentBinderDisplayBodyId = nextFree;
        if (EquipmentBinderAnimationBodyId <= 0)
        {
            EquipmentBinderAnimationBodyId = nextFree;
        }

        SetStatus("自动填充显示 Body/Gump ID " + nextFree + "。");
        BuildEquipmentBinderPreview();
    }

    private void BuildEquipmentBinderPreview()
    {
        string currentFolderPath = hostContext.CurrentFolderPathProvider();
        MulSlotEntry? selectedMulSlot = hostContext.SelectedMulSlotProvider();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            EquipmentBinderSummary = "请先打开有效的 UO 文件夹。";
            SetStatus(EquipmentBinderSummary);
            return;
        }

        if (selectedMulSlot == null)
        {
            EquipmentBinderSummary = "请先选择一个空闲的 MUL body ID。";
            SetStatus(EquipmentBinderSummary);
            return;
        }

        try
        {
            EquipmentBinderPreview preview = equipmentBinderService.BuildPreview(
                currentFolderPath,
                EquipmentBinderItemArtId,
                EquipmentBinderDisplayBodyId,
                EquipmentBinderAnimationBodyId,
                selectedMulSlot,
                EquipmentBinderName);

            EquipmentBinderBodyDefLine = preview.BodyDefLine;
            EquipmentBinderBodyConvLine = preview.BodyConvLine;
            EquipmentBinderMobTypeLine = preview.MobTypeLine;
            EquipmentBinderBodyDefExists = preview.BodyDefExists;
            EquipmentBinderBodyConvExists = preview.BodyConvExists;
            EquipmentBinderMobTypeExists = preview.MobTypeExists;
            EquipmentBinderTileDataLine = preview.TileDataLine;
            EquipmentBinderSummary = preview.Summary;

            OnPropertyChanged(nameof(EquipmentBinderExistsText));
            OnPropertyChanged(nameof(EquipmentBinderTargetSlotText));
            SetStatus("装备绑定工具预览已构建。");
        }
        catch (Exception exception)
        {
            EquipmentBinderSummary = "装备绑定工具预览失败: " + exception.Message;
            SetStatus(EquipmentBinderSummary);
        }
    }

    private void ApplyEquipmentBinder()
    {
        string currentFolderPath = hostContext.CurrentFolderPathProvider();
        MulSlotEntry? selectedMulSlot = hostContext.SelectedMulSlotProvider();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            SetStatus("请先打开有效的 UO 文件夹。");
            return;
        }

        if (selectedMulSlot == null)
        {
            SetStatus("请先选择一个空闲的 MUL body ID。");
            return;
        }

        EquipmentBinderResult result = equipmentBinderService.Apply(
            currentFolderPath,
            EquipmentBinderItemArtId,
            EquipmentBinderDisplayBodyId,
            EquipmentBinderAnimationBodyId,
            selectedMulSlot,
            EquipmentBinderName,
            EquipmentBinderOverwriteBodyDef,
            EquipmentBinderOverwriteMobType,
            EquipmentBinderUpdateTileDataAnimation);

        EquipmentBinderSummary = result.Message;
        SetStatus(result.Message);

        if (!result.Success)
        {
            return;
        }

        hostContext.ApplySuccessCallback?.Invoke();
        BuildEquipmentBinderPreview();
        OnPropertyChanged(nameof(EquipmentBinderTargetSlotText));
    }

    private async void LoadEquipmentArtPicker()
    {
        string currentFolderPath = hostContext.CurrentFolderPathProvider();
        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            EquipmentPickerStatusText = "请先打开有效的 UO 文件夹。";
            SetStatus(EquipmentPickerStatusText);
            return;
        }

        EquipmentArtPickerEntries.Clear();
        allEquipmentArtPickerEntries.Clear();
        EquipmentPickerStatusText = "正在加载美术索引...";

        List<EquipmentArtPickerEntry> artIndex = await Task.Run(() => equipmentImageService.LoadArtIndexEntries(currentFolderPath));
        allEquipmentArtPickerEntries.AddRange(artIndex);
        EquipmentPickerStatusText = "美术索引已加载: " + allEquipmentArtPickerEntries.Count + " 项。";
        SetStatus(EquipmentPickerStatusText);

        EquipmentArtPickerSearchText = string.Empty;
        ApplyEquipmentArtPickerFilter();
    }

    private async void LoadEquipmentGumpPicker()
    {
        string currentFolderPath = hostContext.CurrentFolderPathProvider();
        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            EquipmentPickerStatusText = "请先打开有效的 UO 文件夹。";
            SetStatus(EquipmentPickerStatusText);
            return;
        }

        EquipmentGumpPickerEntries.Clear();
        allEquipmentGumpPickerEntries.Clear();
        EquipmentPickerStatusText = "正在加载 Gump 索引...";

        List<EquipmentGumpPickerEntry> gumpIndex = await Task.Run(() => equipmentImageService.LoadGumpIndexEntries(currentFolderPath));
        allEquipmentGumpPickerEntries.AddRange(gumpIndex);
        EquipmentPickerStatusText = "Gump 索引已加载: " + allEquipmentGumpPickerEntries.Count + " 项。";
        SetStatus(EquipmentPickerStatusText);

        EquipmentGumpPickerSearchText = string.Empty;
        ApplyEquipmentGumpPickerFilter();
    }

    private void ApplyEquipmentArtPickerFilter()
    {
        EquipmentArtPickerEntries.Clear();
        string search = (EquipmentArtPickerSearchText ?? string.Empty).Trim();

        foreach (EquipmentArtPickerEntry entry in allEquipmentArtPickerEntries)
        {
            if (!MatchesPickerSearch(entry.ArtId, entry.DisplayText, search)) continue;
            EquipmentArtPickerEntries.Add(entry);
            if (EquipmentArtPickerEntries.Count >= EquipmentPickerPageSize) break;
        }

        _ = LoadVisibleEquipmentArtThumbnailsAsync();
    }

    private void ApplyEquipmentGumpPickerFilter()
    {
        EquipmentGumpPickerEntries.Clear();
        string search = (EquipmentGumpPickerSearchText ?? string.Empty).Trim();

        foreach (EquipmentGumpPickerEntry entry in allEquipmentGumpPickerEntries)
        {
            if (!MatchesPickerSearch(entry.GumpId, entry.DisplayText, search)) continue;
            EquipmentGumpPickerEntries.Add(entry);
            if (EquipmentGumpPickerEntries.Count >= EquipmentPickerPageSize) break;
        }

        _ = LoadVisibleEquipmentGumpThumbnailsAsync();
    }

    private async Task LoadVisibleEquipmentArtThumbnailsAsync()
    {
        string path = hostContext.CurrentFolderPathProvider();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        List<EquipmentArtPickerEntry> targets = EquipmentArtPickerEntries.Where(x => x.Thumbnail == null).Take(EquipmentPickerPageSize).ToList();
        foreach (EquipmentArtPickerEntry entry in targets)
        {
            WriteableBitmap? bitmap = await Task.Run(() => equipmentImageService.LoadArtThumbnail(path, entry.ArtId));
            if (bitmap != null && EquipmentArtPickerEntries.Contains(entry)) entry.Thumbnail = bitmap;
        }
    }

    private async Task LoadVisibleEquipmentGumpThumbnailsAsync()
    {
        string path = hostContext.CurrentFolderPathProvider();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        List<EquipmentGumpPickerEntry> targets = EquipmentGumpPickerEntries.Where(x => x.Thumbnail == null).Take(EquipmentPickerPageSize).ToList();
        foreach (EquipmentGumpPickerEntry entry in targets)
        {
            WriteableBitmap? bitmap = await Task.Run(() => equipmentImageService.LoadGumpThumbnail(path, entry.GumpId));
            if (bitmap != null && EquipmentGumpPickerEntries.Contains(entry)) entry.Thumbnail = bitmap;
        }
    }

    private static bool MatchesPickerSearch(int id, string displayText, string search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        if (displayText.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        string hex = id.ToString("X");
        if (hex.Contains(search.TrimStart('0', 'x', 'X'), StringComparison.OrdinalIgnoreCase)) return true;
        return ("0x" + id.ToString("X4")).Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnEquipmentBinderItemArtIdChanged(int value) => BuildEquipmentBinderPreview();
    partial void OnEquipmentBinderDisplayBodyIdChanged(int value) => BuildEquipmentBinderPreview();
    partial void OnEquipmentBinderAnimationBodyIdChanged(int value) => BuildEquipmentBinderPreview();
    partial void OnEquipmentBinderNameChanged(string value) => BuildEquipmentBinderPreview();
    partial void OnEquipmentArtPickerSearchTextChanged(string value) => ApplyEquipmentArtPickerFilter();
    partial void OnEquipmentGumpPickerSearchTextChanged(string value) => ApplyEquipmentGumpPickerFilter();

    partial void OnSelectedEquipmentArtPickerEntryChanged(EquipmentArtPickerEntry? value)
    {
        if (value == null) return;
        EquipmentBinderItemArtId = value.ArtId;
        BuildEquipmentBinderPreview();
    }

    partial void OnSelectedEquipmentGumpPickerEntryChanged(EquipmentGumpPickerEntry? value)
    {
        if (value == null) return;
        EquipmentBinderDisplayBodyId = value.GumpId;
        if (EquipmentBinderAnimationBodyId <= 0) EquipmentBinderAnimationBodyId = value.GumpId;
        BuildEquipmentBinderPreview();
    }
}
