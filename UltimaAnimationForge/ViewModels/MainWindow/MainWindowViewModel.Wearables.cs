using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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
    [ObservableProperty]
    private int wearableWizardPreviewBaseBodyId = 400;

    [ObservableProperty]
    private int wearableWizardPreviewAction = 0;

    [ObservableProperty]
    private int wearableWizardPreviewDirection = 0;

    [ObservableProperty]
    private int wearableWizardPreviewFrameIndex = 0;

    [ObservableProperty]
    private bool wearableWizardOverlayPreviewEnabled = true;

    [ObservableProperty]
    private WriteableBitmap? wearableWizardOverlayPreviewBitmap;

    [ObservableProperty]
    private double wearableWizardOverlayOffsetX = 0;

    [ObservableProperty]
    private double wearableWizardOverlayOffsetY = 0;

    [ObservableProperty]
    private double wearableWizardOverlayScale = 1.0;

    [ObservableProperty]
    private string wearableWizardOverlayPreviewStatus = "No overlay preview loaded.";

    [ObservableProperty]
    private Bitmap? wearableWizardPaperdollPreview;

    [ObservableProperty]
    private Bitmap? wearableWizardFemalePaperdollPreview;

    [ObservableProperty]
    private Bitmap? wearableWizardArtPreview;

    public ObservableCollection<AnimationEntry> WearableWizardSourceAnimations { get; } = new();

    [ObservableProperty]
    private AnimationEntry? wearableWizardSelectedSourceAnimation;

    [ObservableProperty]
    private string wearableWizardSourceAnimationSearchText = string.Empty;

    [ObservableProperty]
    private bool wearableWizardPartialHue = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardHasSlotConflict))]
    private bool wearableWizardMaleGumpConflict;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardHasSlotConflict))]
    private bool wearableWizardFemaleGumpConflict;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardHasSlotConflict))]
    private bool wearableWizardArtConflict;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardHasSlotConflict))]
    private bool wearableWizardAnimationConflict;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardHasSlotConflict))]
    private bool wearableWizardBodyDefConflict;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardHasSlotConflict))]
    private bool wearableWizardHueInvalid;

    public bool WearableWizardHasSlotConflict =>
        WearableWizardMaleGumpConflict ||
        WearableWizardFemaleGumpConflict ||
        WearableWizardArtConflict ||
        WearableWizardAnimationConflict ||
        WearableWizardBodyDefConflict ||
        WearableWizardHueInvalid ||
        WearableWizardHasMissingId;

    private bool WearableWizardHasMissingId =>
        wearableWizardMaleGumpIdMissing ||
        (WearableWizardCreateFemaleVariant && wearableWizardFemaleGumpIdMissing) ||
        wearableWizardArtIdMissing ||
        wearableWizardAnimationIdMissing;

    [ObservableProperty]
    private string wearableWizardConflictText = string.Empty;

    public ObservableCollection<MulSlotEntry> WearableWizardFreeSlots { get; } = new();

    [ObservableProperty]
    private MulSlotEntry? wearableWizardSelectedFreeSlot;

    [ObservableProperty]
    private string wearableWizardSlotSearchText = string.Empty;

    [ObservableProperty]
    private bool wearableWizardSlotShowHFilter = false;

    [ObservableProperty]
    private bool wearableWizardSlotShowLFilter = false;

    [ObservableProperty]
    private bool wearableWizardSlotShowPFilter = true;

    [ObservableProperty]
    private string wearableWizardSelectedSlotFile = "All Files";

    public ObservableCollection<string> WearableWizardSlotFileOptions { get; } = new();

    public ObservableCollection<string> WearableWizardTypes { get; } = new()
    {
        "长袍",
        "衬衣",
        "裤子",
        "披风",
        "帽子 / 头盔",
        "手套",
        "袖子",
        "鞋子 / 靴子",
        "护甲",
        "自定义"
    };

    public ObservableCollection<string> WearableWizardLayers { get; } = new()
{
    "0x01 - 单手",
    "0x02 - 双手 / 盾牌",
    "0x03 - 鞋子 / 靴子",
    "0x04 - 裤子",
    "0x05 - 衬衣 / 胸部衣服",
    "0x06 - 头盔 / 头部",
    "0x07 - 手套",
    "0x08 - 戒指",
    "0x09 - 护身符",
    "0x0A - 脖子",
    "0x0B - 头发",
    "0x0C - 腰部 / 半围裙",
    "0x0D - 内层躯体 / 胸甲",
    "0x0E - 手镯",
    "0x10 - 胡须",
    "0x11 - 中层躯体 / 束腰短衣 / 腰带",
    "0x12 - 耳环",
    "0x13 - 手臂",
    "0x14 - 披风 / 背部",
    "0x15 - 背包",
    "0x16 - 外层躯体 / 长袍",
    "0x17 - 外层腿部 / 裙子 / 苏格兰短裙",
    "0x18 - 内层腿部 / 腿甲"
};

    public ObservableCollection<string> WearableWizardAnimationModes { get; } = new()
    {
        "复用现有动画",
        "稍后导入新 VD",
        "暂无动画"
    };

    [ObservableProperty]
    private string wearableWizardName = "New Wearable";

    [ObservableProperty]
    private string wearableWizardType = "长袍";

    [ObservableProperty]
    private string wearableWizardLayer = "0x16 - 外层躯体 / 长袍";

    [ObservableProperty]
    private string wearableWizardAnimationMode = "复用现有动画";

    [ObservableProperty]
    private string wearableWizardPaperdollImagePath = string.Empty;

    [ObservableProperty]
    private string wearableWizardFemalePaperdollImagePath = string.Empty;

    [ObservableProperty]
    private string wearableWizardArtImagePath = string.Empty;

    [ObservableProperty]
    private bool wearableWizardCreateFemaleVariant = true;

    [ObservableProperty]
    private bool wearableWizardUseSittingSafeRange = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardMaleGumpIdInput))]
    private int wearableWizardMaleGumpId = 50400;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardFemaleGumpIdInput))]
    private int wearableWizardFemaleGumpId = 60400;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardArtIdInput))]
    private int wearableWizardArtId = 400;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WearableWizardAnimationIdInput))]
    private int wearableWizardAnimationId = 400;

    private bool wearableWizardMaleGumpIdMissing;
    private bool wearableWizardFemaleGumpIdMissing;
    private bool wearableWizardArtIdMissing;
    private bool wearableWizardAnimationIdMissing;

    public int? WearableWizardMaleGumpIdInput
    {
        get => wearableWizardMaleGumpIdMissing ? null : WearableWizardMaleGumpId;
        set => SetWearableWizardIdInput(value, ref wearableWizardMaleGumpIdMissing, id => WearableWizardMaleGumpId = id);
    }

    public int? WearableWizardFemaleGumpIdInput
    {
        get => wearableWizardFemaleGumpIdMissing ? null : WearableWizardFemaleGumpId;
        set => SetWearableWizardIdInput(value, ref wearableWizardFemaleGumpIdMissing, id => WearableWizardFemaleGumpId = id);
    }

    public int? WearableWizardArtIdInput
    {
        get => wearableWizardArtIdMissing ? null : WearableWizardArtId;
        set => SetWearableWizardIdInput(value, ref wearableWizardArtIdMissing, id => WearableWizardArtId = id);
    }

    public int? WearableWizardAnimationIdInput
    {
        get => wearableWizardAnimationIdMissing ? null : WearableWizardAnimationId;
        set => SetWearableWizardIdInput(value, ref wearableWizardAnimationIdMissing, id => WearableWizardAnimationId = id);
    }

    [ObservableProperty]
    private int wearableWizardExistingAnimationId = 435;

    [ObservableProperty]
    private string wearableWizardHue = "0";

    [ObservableProperty]
    private string wearableWizardPlanText = "尚未生成规划。";

    [ObservableProperty]
    private string wearableWizardStatusText = "就绪。";

    private Bitmap? LoadWearablePreviewBitmap(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return new Bitmap(path);
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    private async Task BrowseWearablePaperdollImageAsync()
    {
        string? path = await PickWearableImageAsync("选择男性 / 默认纸娃娃 Gump 图片");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }


        WearableWizardPaperdollImagePath = path;
        WearableWizardPaperdollPreview = LoadWearablePreviewBitmap(path);
        RebuildWearableWizardPlan();
        RefreshWearableWizardOverlayPreview();
    }

    [RelayCommand]
    private async Task BrowseWearableFemalePaperdollImageAsync()
    {
        string? path = await PickWearableImageAsync("选择女性纸娃娃 Gump 图片");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        WearableWizardFemalePaperdollImagePath = path;
        WearableWizardFemalePaperdollPreview = LoadWearablePreviewBitmap(path);
        WearableWizardCreateFemaleVariant = true;
        RebuildWearableWizardPlan();
    }

    [RelayCommand]
    private async Task BrowseWearableArtImageAsync()
    {
        string? path = await PickWearableImageAsync("选择库存 / 世界美术图片");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        WearableWizardArtImagePath = path;
        WearableWizardArtPreview = LoadWearablePreviewBitmap(path);
        RebuildWearableWizardPlan();
    }

    [RelayCommand]
    private void AutoPickWearableWizardIds()
    {
        int maleStart = WearableWizardUseSittingSafeRange ? 50400 : 50000;
        int maleEnd = WearableWizardUseSittingSafeRange
            ? 50999
            : WearableWizardCreateFemaleVariant ? 55534 : 59999;

        HashSet<int> usedGumpIds = GumpEntries
            .Where(entry => entry.IsValid)
            .Select(entry => entry.GumpId)
            .ToHashSet();
        int? maleGumpId = FindNextFreeWearableGumpId(
            maleStart,
            maleEnd,
            usedGumpIds,
            WearableWizardCreateFemaleVariant);

        HashSet<int> usedArtIds = LoadUsedWearableArtIds();
        int? artId = FindFirstFreeId(0, ushort.MaxValue, usedArtIds);

        HashSet<int> usedAnimationIds = AnimationEntries
            .Select(entry => entry.BodyId)
            .ToHashSet();
        usedAnimationIds.UnionWith(new BodyDefService()
            .Load(Path.Combine(GetCurrentFolderPath(), "Body.def"))
            .Keys);
        int? animationId = FindFirstFreeId(0, 9999, usedAnimationIds);

        if (maleGumpId == null || artId == null || animationId == null)
        {
            List<string> missingSlots = new();
            if (maleGumpId == null)
            {
                missingSlots.Add("gump");
            }
            if (artId == null)
            {
                missingSlots.Add("art");
            }
            if (animationId == null)
            {
                missingSlots.Add("animation");
            }

            WearableWizardStatusText =
                "在允许范围内未找到空闲的 " + string.Join("/", missingSlots) + " ID。";
            return;
        }

        WearableWizardMaleGumpIdInput = maleGumpId.Value;
        WearableWizardFemaleGumpIdInput = 60000 + (maleGumpId.Value - 50000);
        WearableWizardArtIdInput = artId.Value;
        WearableWizardAnimationIdInput = animationId.Value;

        RebuildWearableWizardPlan();
        WearableWizardStatusText = "已选择空闲的 Gump、美术和动画 ID。";
    }

    [RelayCommand]
    private void RebuildWearableWizardPlan()
    {
        string bodyDefLine = WearableWizardAnimationMode == "复用现有动画"
            ? WearableWizardAnimationId + " {" + WearableWizardExistingAnimationId + "} " + NormalizeWearableHue()
            : "(暂无 Body.def 行)";

        WearableWizardPlanText =
            "装扮: " + WearableWizardName + Environment.NewLine +
            "类型: " + WearableWizardType + Environment.NewLine +
            "层: " + WearableWizardLayer + Environment.NewLine +
            Environment.NewLine +
            "男性/默认 Gump: " + WearableWizardMaleGumpId + " [0x" + WearableWizardMaleGumpId.ToString("X") + "]" + Environment.NewLine +
            "女性 Gump: " + (WearableWizardCreateFemaleVariant ? WearableWizardFemaleGumpId + " [0x" + WearableWizardFemaleGumpId.ToString("X") + "]" : "未创建") + Environment.NewLine +
            "美术/静态 ID: " + WearableWizardArtId + " [0x" + WearableWizardArtId.ToString("X") + "]" + Environment.NewLine +
            "图块数据动画: " + WearableWizardAnimationId + Environment.NewLine +
            Environment.NewLine +
            "Body.def:" + Environment.NewLine +
            bodyDefLine + Environment.NewLine +
            Environment.NewLine +
            "文件:" + Environment.NewLine +
            "纸娃娃: " + DisplayPathOrNone(WearableWizardPaperdollImagePath) + Environment.NewLine +
            "女性纸娃娃: " + DisplayPathOrNone(WearableWizardFemalePaperdollImagePath) + Environment.NewLine +
            "美术: " + DisplayPathOrNone(WearableWizardArtImagePath);

        WearableWizardStatusText = "规划已更新。";
        ValidateWearableWizardConflicts();
    }

    [RelayCommand]
    private void ResetWearableWizard()
    {
        WearableWizardName = "新装扮";
        WearableWizardType = "长袍";
        WearableWizardLayer = "0x16 - 外层躯体 / 长袍";
        WearableWizardAnimationMode = "复用现有动画";
        WearableWizardPaperdollImagePath = string.Empty;
        WearableWizardFemalePaperdollImagePath = string.Empty;
        WearableWizardArtImagePath = string.Empty;
        WearableWizardCreateFemaleVariant = true;
        WearableWizardUseSittingSafeRange = true;
        WearableWizardMaleGumpIdInput = 50400;
        WearableWizardFemaleGumpIdInput = 60400;
        WearableWizardArtIdInput = 400;
        WearableWizardAnimationIdInput = 400;
        WearableWizardExistingAnimationId = 435;
        WearableWizardHue = "0";
        WearableWizardPlanText = "尚未生成规划。";
        WearableWizardStatusText = "就绪。";
        WearableWizardPaperdollPreview = null;
        WearableWizardFemalePaperdollPreview = null;
        WearableWizardArtPreview = null;
    }

    [RelayCommand]
    private void ApplyWearableWizard()
    {
        RebuildWearableWizardPlan();
        ValidateWearableWizardConflicts();

        if (WearableWizardHasSlotConflict)
        {
            WearableWizardStatusText =
                "应用前请解决槽位冲突。";

            return;
        }

        WearableCreationService service = new();

        WearableCreationService.Result result = service.Apply(new WearableCreationService.Request
        {
            FolderPath = GetCurrentFolderPath(),
            Name = WearableWizardName,
            Layer = WearableWizardLayer,

            MaleGumpImagePath = WearableWizardPaperdollImagePath,
            FemaleGumpImagePath = WearableWizardFemalePaperdollImagePath,
            ArtImagePath = WearableWizardArtImagePath,

            CreateFemaleVariant = WearableWizardCreateFemaleVariant,

            MaleGumpId = WearableWizardMaleGumpId,
            FemaleGumpId = WearableWizardFemaleGumpId,
            ArtId = WearableWizardArtId,
            AnimationId = WearableWizardAnimationId,
            ExistingAnimationId = WearableWizardExistingAnimationId,
            Hue = WearableWizardHue,
            PartialHue = WearableWizardPartialHue,
            WriteBodyDef = WearableWizardAnimationMode == "复用现有动画"
        });

        WearableWizardStatusText = result.Message;
        StatusText = result.Message;

        if (result.Success)
        {
            InitializeGumpsForCurrentFolder();
            LoadArtTab();
            LoadTileData();
            RebuildWearableWizardPlan();
        }
    }

    partial void OnWearableWizardNameChanged(string value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardTypeChanged(string value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardLayerChanged(string value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardAnimationModeChanged(string value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardCreateFemaleVariantChanged(bool value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardUseSittingSafeRangeChanged(bool value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardMaleGumpIdChanged(int value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardFemaleGumpIdChanged(int value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardArtIdChanged(int value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardAnimationIdChanged(int value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardExistingAnimationIdChanged(int value)
    {
        RebuildWearableWizardPlan();
    }

    partial void OnWearableWizardHueChanged(string value)
    {
        RebuildWearableWizardPlan();
    }

    private async Task<string?> PickWearableImageAsync(string title)
    {
        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            WearableWizardStatusText = "无法定位主窗口。";
            return null;
        }

        IReadOnlyList<IStorageFile> files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("图片文件")
                    {
                        Patterns = new[] { "*.png", "*.bmp", "*.jpg", "*.jpeg", "*.tif", "*.tiff" }
                    }
                }
            });

        if (files.Count == 0)
        {
            WearableWizardStatusText = "图片选择已取消。";
            return null;
        }

        string? path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            WearableWizardStatusText = "选中的图片没有本地路径。";
            return null;
        }

        WearableWizardStatusText = "已选择 " + Path.GetFileName(path) + "。";
        return path;
    }

    private static int? FindNextFreeWearableGumpId(
        int start,
        int end,
        HashSet<int> usedGumpIds,
        bool requireFemaleVariant)
    {
        if (start < 50000)
        {
            start = 50000;
        }

        if (end > 59999)
        {
            end = 59999;
        }

        for (int id = start; id <= end; id++)
        {
            int femaleId = 60000 + (id - 50000);
            if (!usedGumpIds.Contains(id) &&
                (!requireFemaleVariant || !usedGumpIds.Contains(femaleId)))
            {
                return id;
            }
        }

        return null;
    }

    private static int? FindFirstFreeId(int start, int end, HashSet<int> usedIds)
    {
        for (int id = start; id <= end; id++)
        {
            if (!usedIds.Contains(id))
            {
                return id;
            }
        }

        return null;
    }

    private string NormalizeWearableHue()
    {
        string value = (WearableWizardHue ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return "0";
        }

        return value;
    }

    private void SetWearableWizardIdInput(int? value, ref bool missing, Action<int> assign)
    {
        missing = !value.HasValue;

        if (value.HasValue)
        {
            assign(value.Value);
        }

        OnPropertyChanged(nameof(WearableWizardHasSlotConflict));
        RebuildWearableWizardPlan();
    }

    private HashSet<int> LoadUsedWearableArtIds()
    {
        string folderPath = GetCurrentFolderPath();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return GetVisibleUsedStaticArtIds();
        }

        return LoadCachedOrBuildArtEntries(folderPath)
            .Where(entry =>
                string.Equals(entry.Type, "Static", StringComparison.OrdinalIgnoreCase) &&
                !entry.IsFreeSlot)
            .Select(entry => entry.ArtId)
            .ToHashSet();
    }

    private HashSet<int> GetVisibleUsedStaticArtIds()
    {
        return ArtEntries
            .Where(entry =>
                string.Equals(entry.Type, "Static", StringComparison.OrdinalIgnoreCase) &&
                !entry.IsFreeSlot)
            .Select(entry => entry.ArtId)
            .ToHashSet();
    }

    private static string DisplayPathOrNone(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? "无"
            : path;
    }

    private void ValidateWearableWizardConflicts()
    {
        bool maleConflict =
            !wearableWizardMaleGumpIdMissing &&
            GumpEntries.Any(x => x.GumpId == WearableWizardMaleGumpId && x.IsValid);

        bool femaleConflict =
            WearableWizardCreateFemaleVariant &&
            !wearableWizardFemaleGumpIdMissing &&
            GumpEntries.Any(x => x.GumpId == WearableWizardFemaleGumpId && x.IsValid);

        bool artConflict =
            !wearableWizardArtIdMissing &&
            LoadUsedWearableArtIds().Contains(WearableWizardArtId);

        bool animationConflict =
            !wearableWizardAnimationIdMissing &&
            AnimationEntries.Any(x => x.BodyId == WearableWizardAnimationId);

        bool bodyDefConflict =
            WearableWizardAnimationMode == "复用现有动画" &&
            !wearableWizardAnimationIdMissing &&
            new BodyDefService().EntryExists(
                Path.Combine(GetCurrentFolderPath(), "Body.def"),
                WearableWizardAnimationId);

        bool hueInvalid =
            WearableWizardAnimationMode == "复用现有动画" &&
            !int.TryParse(NormalizeWearableHue(), out _);

        WearableWizardMaleGumpConflict = maleConflict;
        WearableWizardFemaleGumpConflict = femaleConflict;
        WearableWizardArtConflict = artConflict;
        WearableWizardAnimationConflict = animationConflict;
        WearableWizardBodyDefConflict = bodyDefConflict;
        WearableWizardHueInvalid = hueInvalid;

        List<string> warnings = new();

        if (wearableWizardMaleGumpIdMissing)
        {
            warnings.Add("男性/默认 Gump ID 为必填项。");
        }

        if (WearableWizardCreateFemaleVariant && wearableWizardFemaleGumpIdMissing)
        {
            warnings.Add("女性 Gump ID 为必填项。");
        }

        if (wearableWizardArtIdMissing)
        {
            warnings.Add("美术 ID 为必填项。");
        }

        if (wearableWizardAnimationIdMissing)
        {
            warnings.Add("动画 ID 为必填项。");
        }

        if (maleConflict)
        {
            warnings.Add("男性/默认 Gump ID 已存在。");
        }

        if (femaleConflict)
        {
            warnings.Add("女性 Gump ID 已存在。");
        }

        if (artConflict)
        {
            warnings.Add("美术 ID 已存在。");
        }

        if (animationConflict)
        {
            warnings.Add("动画身体 ID 已存在。");
        }

        if (bodyDefConflict)
        {
            warnings.Add("Body.def 已包含此动画身体 ID。");
        }

        if (hueInvalid)
        {
            warnings.Add("Body.def 色调必须是整数。");
        }

        WearableWizardConflictText =
            warnings.Count == 0
                ? "未检测到槽位冲突。"
                : string.Join(Environment.NewLine, warnings);
    }

    [RelayCommand]
    private void RefreshWearableWizardFreeSlots()
    {
        RebuildWearableWizardSlotFileOptions();
        ApplyWearableWizardSlotFilters();
    }

    [RelayCommand]
    private void UseWearableWizardSelectedSlot()
    {
        if (WearableWizardSelectedFreeSlot == null)
        {
            WearableWizardStatusText = "请先选择一个空闲动画槽位。";
            return;
        }

        int bodyId = WearableWizardSelectedFreeSlot.TrueBodyId;

        WearableWizardAnimationIdInput = bodyId;

        WearableWizardStatusText =
            "已选择空闲动画槽位 " +
            WearableWizardSelectedFreeSlot.DisplayText +
            "。Body.def 将把新动画 " +
            WearableWizardAnimationId +
            " 映射到现有动画 " +
            WearableWizardExistingAnimationId +
            "。";

        RebuildWearableWizardPlan();
    }

    private void RebuildWearableWizardSlotFileOptions()
    {
        string previous = WearableWizardSelectedSlotFile;

        WearableWizardSlotFileOptions.Clear();
        WearableWizardSlotFileOptions.Add("All Files");

        foreach (string fileName in allMulSlotEntries
                     .Select(x => x.FileName)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            WearableWizardSlotFileOptions.Add(fileName);
        }

        WearableWizardSelectedSlotFile =
            WearableWizardSlotFileOptions.Contains(previous)
                ? previous
                : "All Files";
    }

    private void ApplyWearableWizardSlotFilters()
    {
        WearableWizardFreeSlots.Clear();

        string search = WearableWizardSlotSearchText?.Trim() ?? string.Empty;
        string selectedFile = WearableWizardSelectedSlotFile ?? "All Files";

        foreach (MulSlotEntry entry in allMulSlotEntries)
        {
            if (!entry.IsEmpty)
            {
                continue;
            }

            if (!string.Equals(selectedFile, "All Files", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entry.FileName, selectedFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(entry.TypeLetter, "P", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                bool matchesSearch =
                    entry.DisplayText.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.FileName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.BodyIndex.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.TrueBodyId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.TypeLetter.Contains(search, StringComparison.OrdinalIgnoreCase);

                if (!matchesSearch)
                {
                    continue;
                }
            }

            WearableWizardFreeSlots.Add(entry);
        }

        if (WearableWizardFreeSlots.Count > 0)
        {
            if (WearableWizardSelectedFreeSlot == null ||
                !WearableWizardFreeSlots.Contains(WearableWizardSelectedFreeSlot))
            {
                WearableWizardSelectedFreeSlot = WearableWizardFreeSlots[0];
            }
        }
        else
        {
            WearableWizardSelectedFreeSlot = null;
        }

        WearableWizardStatusText =
            "显示 " + WearableWizardFreeSlots.Count + " 个装扮动画槽位候选项。";
    }

    partial void OnWearableWizardSlotSearchTextChanged(string value)
    {
        ApplyWearableWizardSlotFilters();
    }

    partial void OnWearableWizardSlotShowHFilterChanged(bool value)
    {
        ApplyWearableWizardSlotFilters();
    }

    partial void OnWearableWizardSlotShowLFilterChanged(bool value)
    {
        ApplyWearableWizardSlotFilters();
    }

    partial void OnWearableWizardSlotShowPFilterChanged(bool value)
    {
        ApplyWearableWizardSlotFilters();
    }

    partial void OnWearableWizardSelectedSlotFileChanged(string value)
    {
        ApplyWearableWizardSlotFilters();
    }

    [RelayCommand]
    private void RefreshWearableWizardSourceAnimations()
    {
        ApplyWearableWizardSourceAnimationFilter();
    }

    [RelayCommand]
    private void UseWearableWizardSourceAnimation()
    {
        if (WearableWizardSelectedSourceAnimation == null)
        {
            WearableWizardStatusText = "请先选择一个源动画。";
            return;
        }

        WearableWizardExistingAnimationId = WearableWizardSelectedSourceAnimation.BodyId;

        WearableWizardStatusText =
            "使用源动画身体 " +
            WearableWizardExistingAnimationId +
            " 进行 Body.def 继承。";

        RebuildWearableWizardPlan();
    }

    private void ApplyWearableWizardSourceAnimationFilter()
    {
        WearableWizardSourceAnimations.Clear();

        string search = WearableWizardSourceAnimationSearchText?.Trim() ?? string.Empty;

        HashSet<int> seenBodies = new();

        foreach (AnimationEntry entry in AnimationEntries.OrderBy(x => x.BodyId))
        {
            // Wearable/equipment animations are P slots / human-style 35-action bodies.
            if (entry.BodyId < 400)
            {
                continue;
            }

            if (!seenBodies.Add(entry.BodyId))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                bool matches =
                    entry.BodyId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.SecondaryText.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.SourceFile.Contains(search, StringComparison.OrdinalIgnoreCase);

                if (!matches)
                {
                    continue;
                }
            }

            WearableWizardSourceAnimations.Add(entry);
        }

        if (WearableWizardSourceAnimations.Count > 0)
        {
            if (WearableWizardSelectedSourceAnimation == null ||
                !WearableWizardSourceAnimations.Contains(WearableWizardSelectedSourceAnimation))
            {
                WearableWizardSelectedSourceAnimation = WearableWizardSourceAnimations[0];
            }
        }
        else
        {
            WearableWizardSelectedSourceAnimation = null;
        }
    }

    partial void OnWearableWizardSourceAnimationSearchTextChanged(string value)
    {
        ApplyWearableWizardSourceAnimationFilter();
    }

    [RelayCommand]
    private void RefreshWearableWizardOverlayPreview()
    {
        if (!WearableWizardOverlayPreviewEnabled)
        {
            WearableWizardOverlayPreviewBitmap = null;
            WearableWizardOverlayPreviewStatus = "Overlay preview disabled.";
            return;
        }

        AnimationEntry? baseEntry = AnimationEntries
            .FirstOrDefault(x => x.BodyId == WearableWizardPreviewBaseBodyId);

        if (baseEntry == null)
        {
            WearableWizardOverlayPreviewBitmap = null;
            WearableWizardOverlayPreviewStatus = "Base body " + WearableWizardPreviewBaseBodyId + " was not found.";
            return;
        }

        int wearableBodyId = WearableWizardExistingAnimationId;

        AnimationEntry? wearableEntry = AnimationEntries
            .FirstOrDefault(x => x.BodyId == wearableBodyId);

        if (wearableEntry == null)
        {
            WearableWizardOverlayPreviewBitmap = null;
            WearableWizardOverlayPreviewStatus = "Wearable animation body " + wearableBodyId + " was not found.";
            return;
        }

        DetachedPreviewLoadResult baseResult = LoadDetachedPreview(
            baseEntry,
            WearableWizardPreviewAction,
            WearableWizardPreviewDirection);

        if (!baseResult.Success || baseResult.FrameData.Count == 0)
        {
            WearableWizardOverlayPreviewBitmap = null;
            WearableWizardOverlayPreviewStatus = "Could not load base body preview: " + baseResult.Message;
            return;
        }

        DetachedPreviewLoadResult wearableResult = LoadDetachedPreview(
            wearableEntry,
            WearableWizardPreviewAction,
            WearableWizardPreviewDirection);

        if (!wearableResult.Success || wearableResult.FrameData.Count == 0)
        {
            WearableWizardOverlayPreviewBitmap = null;
            WearableWizardOverlayPreviewStatus = "Could not load wearable animation preview: " + wearableResult.Message;
            return;
        }

        int frameIndex = WearableWizardPreviewFrameIndex;
        if (frameIndex < 0)
        {
            frameIndex = 0;
        }

        VdFrameData baseFrame = baseResult.FrameData[frameIndex % baseResult.FrameData.Count];
        VdFrameData wearableFrame = wearableResult.FrameData[frameIndex % wearableResult.FrameData.Count];

        WearableWizardOverlayPreviewBitmap = BuildWearableAnimationOverlayPreview(
            baseFrame,
            wearableFrame,
            WearableWizardOverlayOffsetX,
            WearableWizardOverlayOffsetY,
            WearableWizardOverlayScale);

        WearableWizardOverlayPreviewStatus =
            "Previewing base body " + WearableWizardPreviewBaseBodyId +
            " + wearable animation " + wearableBodyId +
            " | Action " + WearableWizardPreviewAction +
            " | Direction " + WearableWizardPreviewDirection +
            " | Frame " + frameIndex + ".";
    }

    private WriteableBitmap BuildWearableAnimationOverlayPreview(
        VdFrameData baseFrame,
        VdFrameData wearableFrame,
        double offsetX,
        double offsetY,
        double scale)
    {
        int padding = 160;

        int baseWidth = baseFrame.Bitmap.PixelSize.Width;
        int baseHeight = baseFrame.Bitmap.PixelSize.Height;

        int wearableWidth = Math.Max(1, (int)(wearableFrame.Bitmap.PixelSize.Width * scale));
        int wearableHeight = Math.Max(1, (int)(wearableFrame.Bitmap.PixelSize.Height * scale));

        int outputWidth = Math.Max(baseWidth, wearableWidth) + padding;
        int outputHeight = Math.Max(baseHeight, wearableHeight) + padding;

        WriteableBitmap output = new WriteableBitmap(
            new Avalonia.PixelSize(outputWidth, outputHeight),
            new Avalonia.Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);

        byte[] outputPixels = new byte[outputWidth * outputHeight * 4];

        int originX = outputWidth / 2;
        int originY = (outputHeight / 2) + 50;

        int baseX = originX - baseFrame.CenterX;
        int baseY = originY - baseFrame.Bitmap.PixelSize.Height - baseFrame.CenterY;

        int wearableX = originX - (int)(wearableFrame.CenterX * scale) + (int)offsetX;
        int wearableY = originY - wearableHeight - (int)(wearableFrame.CenterY * scale) + (int)offsetY;

        CopyBitmapToBuffer(baseFrame.Bitmap, outputPixels, outputWidth, baseX, baseY);
        CopyBitmapScaledToBuffer(
            wearableFrame.Bitmap,
            outputPixels,
            outputWidth,
            wearableX,
            wearableY,
            wearableWidth,
            wearableHeight);

        using Avalonia.Platform.ILockedFramebuffer framebuffer = output.Lock();
        System.Runtime.InteropServices.Marshal.Copy(outputPixels, 0, framebuffer.Address, outputPixels.Length);

        return output;
    }

    private void CopyBitmapToBuffer(
    WriteableBitmap source,
    byte[] destination,
    int destinationWidth,
    int destinationX,
    int destinationY)
    {
        using Avalonia.Platform.ILockedFramebuffer framebuffer = source.Lock();

        int width = framebuffer.Size.Width;
        int height = framebuffer.Size.Height;
        int srcRowBytes = framebuffer.RowBytes;
        byte[] src = new byte[srcRowBytes * height];

        System.Runtime.InteropServices.Marshal.Copy(framebuffer.Address, src, 0, src.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcOffset = (y * srcRowBytes) + (x * 4);
                int dstX = destinationX + x;
                int dstY = destinationY + y;

                if (dstX < 0 || dstY < 0)
                {
                    continue;
                }

                int dstOffset = ((dstY * destinationWidth) + dstX) * 4;
                if (dstOffset < 0 || dstOffset + 3 >= destination.Length)
                {
                    continue;
                }

                destination[dstOffset + 0] = src[srcOffset + 0];
                destination[dstOffset + 1] = src[srcOffset + 1];
                destination[dstOffset + 2] = src[srcOffset + 2];
                destination[dstOffset + 3] = src[srcOffset + 3];
            }
        }
    }

    private void CopyBitmapScaledToBuffer(
        WriteableBitmap source,
        byte[] destination,
        int destinationWidth,
        int destinationX,
        int destinationY,
        int scaledWidth,
        int scaledHeight)
    {
        using Avalonia.Platform.ILockedFramebuffer framebuffer = source.Lock();

        int sourceWidth = framebuffer.Size.Width;
        int sourceHeight = framebuffer.Size.Height;
        int srcRowBytes = framebuffer.RowBytes;
        byte[] src = new byte[srcRowBytes * sourceHeight];

        System.Runtime.InteropServices.Marshal.Copy(framebuffer.Address, src, 0, src.Length);

        for (int y = 0; y < scaledHeight; y++)
        {
            int srcY = Math.Clamp((int)((double)y / scaledHeight * sourceHeight), 0, sourceHeight - 1);

            for (int x = 0; x < scaledWidth; x++)
            {
                int srcX = Math.Clamp((int)((double)x / scaledWidth * sourceWidth), 0, sourceWidth - 1);
                int srcOffset = (srcY * srcRowBytes) + (srcX * 4);

                byte alpha = src[srcOffset + 3];
                if (alpha == 0)
                {
                    continue;
                }

                int dstX = destinationX + x;
                int dstY = destinationY + y;

                if (dstX < 0 || dstY < 0)
                {
                    continue;
                }

                int dstOffset = ((dstY * destinationWidth) + dstX) * 4;
                if (dstOffset < 0 || dstOffset + 3 >= destination.Length)
                {
                    continue;
                }

                double a = alpha / 255.0;
                destination[dstOffset + 0] = (byte)((src[srcOffset + 0] * a) + (destination[dstOffset + 0] * (1.0 - a)));
                destination[dstOffset + 1] = (byte)((src[srcOffset + 1] * a) + (destination[dstOffset + 1] * (1.0 - a)));
                destination[dstOffset + 2] = (byte)((src[srcOffset + 2] * a) + (destination[dstOffset + 2] * (1.0 - a)));
                destination[dstOffset + 3] = 255;
            }
        }
    }

    partial void OnWearableWizardPreviewBaseBodyIdChanged(int value)
    {
        RefreshWearableWizardOverlayPreview();
    }

    partial void OnWearableWizardPreviewActionChanged(int value)
    {
        RefreshWearableWizardOverlayPreview();
    }

    partial void OnWearableWizardPreviewDirectionChanged(int value)
    {
        RefreshWearableWizardOverlayPreview();
    }

    partial void OnWearableWizardPreviewFrameIndexChanged(int value)
    {
        RefreshWearableWizardOverlayPreview();
    }

    partial void OnWearableWizardOverlayOffsetXChanged(double value)
    {
        RefreshWearableWizardOverlayPreview();
    }

    partial void OnWearableWizardOverlayOffsetYChanged(double value)
    {
        RefreshWearableWizardOverlayPreview();
    }

    partial void OnWearableWizardOverlayScaleChanged(double value)
    {
        RefreshWearableWizardOverlayPreview();
    }

    partial void OnWearableWizardOverlayPreviewEnabledChanged(bool value)
    {
        RefreshWearableWizardOverlayPreview();
    }
}
