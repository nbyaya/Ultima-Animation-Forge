using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using UltimaAnimationForge.Models;

namespace UltimaAnimationForge.ViewModels;

public partial class MainWindowViewModel
{
    private async Task ReportLoadingProgressAsync(double percent, string message)
    {
        LoadingProgress = percent;
        LoadingText = message;

        OnPropertyChanged(nameof(LoadingProgress));
        OnPropertyChanged(nameof(LoadingText));

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
    }

    private void ApplyLoadedData(
        List<AnimationEntry> animationEntries,
        List<MulSlotEntry> mulSlotEntries)
    {
        allAnimationEntries.Clear();
        AnimationEntries.Clear();
        allMulSlotEntries.Clear();
        allUopBodyEntries.Clear();

        allAnimationEntries.AddRange(animationEntries);
        allMulSlotEntries.AddRange(mulSlotEntries);

        RebuildCompareFileOptions();
        ApplyCompareAnimationFilters();
        RebuildCompareDirectionList();

        RebuildAnimationFileOptions();
        RebuildUopBodyEntries();
        ApplyAnimationFilters();
        ApplyMulSlotFilters();
        ApplyUopBodyFilters();
    }

    private async Task OpenFolderAsync()
    {
        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "无法找到主窗口。";
            return;
        }

        FolderPickerOpenOptions folderPickerOpenOptions = new FolderPickerOpenOptions
        {
            Title = "选择 Ultima Online 文件夹",
            AllowMultiple = false
        };

        string currentFolderPath = GetCurrentFolderPath();

        if (!string.IsNullOrWhiteSpace(currentFolderPath) && Directory.Exists(currentFolderPath))
        {
            folderPickerOpenOptions.SuggestedStartLocation =
                await mainWindow.StorageProvider.TryGetFolderFromPathAsync(currentFolderPath);
        }

        IReadOnlyList<IStorageFolder> selectedFolders =
            await mainWindow.StorageProvider.OpenFolderPickerAsync(folderPickerOpenOptions);

        if (selectedFolders.Count == 0)
        {
            StatusText = "文件夹选择已取消。";
            return;
        }

        string? localPath = selectedFolders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            StatusText = "所选文件夹不具有本地路径。";
            return;
        }

        if (activeProfile == null)
        {
            activeProfile = GetActiveProfile();
        }

        if (activeProfile == null)
        {
            activeProfile = new AnimationViewerProfile
            {
                ProfileName = "Default",
                UoFolderPath = localPath
            };

            appSettings.Profiles.Add(activeProfile);
            appSettings.LastActiveProfileId = activeProfile.ProfileId;
        }
        else
        {
            activeProfile.UoFolderPath = localPath;
            appSettings.LastActiveProfileId = activeProfile.ProfileId;
        }

        SaveActiveProfileUiState();
        RebuildProfileOptions();

        await LoadFolderAsync(localPath);
    }

    private async Task LoadFolderAsync(string localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath) || !Directory.Exists(localPath))
        {
            StatusText = "请先打开有效的 UO 文件夹。";
            return;
        }

        IsLoading = true;
        LoadingProgress = 0;
        LoadingText = "正在加载动画文件...";
        SetRandomLoadingTip();

        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(LoadingProgress));
        OnPropertyChanged(nameof(LoadingText));
        OnPropertyChanged(nameof(LoadingTipText));

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        string profileId = activeProfile?.ProfileId ?? "default";

        AnimationCacheData? cacheData = animationCacheService.LoadCache(profileId);

        if (animationCacheService.IsCacheValid(cacheData, localPath))
        {
            SelectedSourceText = localPath;
            activeAnimationDataSource = null;
            radarColService.Load(localPath, out _);

            await ReportLoadingProgressAsync(10, "正在初始化缓存数据的动画源...");

            bool shouldLoadUop = ShouldLoadUopForActiveProfile();

            bool mulReady = await Task.Run(() => mulAnimationDataSource.Initialize(localPath));
            bool uopReady = shouldLoadUop &&
                            await Task.Run(() => uopAnimationDataSource.Initialize(localPath));
            bool vdReady = await Task.Run(() => vdFolderAnimationDataSource.Initialize(localPath));

            UpdatePreferredAnimationSources();

            if (!mulReady && !uopReady && !vdReady)
            {
                StatusText = "找到了缓存数据，但无法初始化动画源。";
                IsLoading = false;
                OnPropertyChanged(nameof(IsLoading));
                return;
            }

            await ReportLoadingProgressAsync(40, "正在加载缓存的动画数据...");

            List<AnimationEntry> combinedEntries =
                cacheData!.AnimationEntries ?? new List<AnimationEntry>();

            if (uopReady)
            {
                await ReportLoadingProgressAsync(65, "正在重建 UOP 动画条目...");

                const int uopBodiesToScan = 65536;

                List<AnimationEntry> liveUopEntries =
                    await Task.Run(() => uopAnimationDataSource.BuildAnimationEntries(uopBodiesToScan));

                foreach (AnimationEntry entry in liveUopEntries)
                {
                    entry.SourceMode = "UOP";

                    if (!entry.SecondaryText.Contains("| UOP |", StringComparison.OrdinalIgnoreCase) &&
                        !entry.SecondaryText.EndsWith("| UOP", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.SecondaryText += " | UOP";
                    }
                }

                combinedEntries = combinedEntries
                    .Concat(liveUopEntries)
                    .ToList();
            }

            if (vdReady)
            {
                await ReportLoadingProgressAsync(80, "正在重建 VD 动画条目...");

                List<AnimationEntry> liveVdEntries =
                    await Task.Run(() => vdFolderAnimationDataSource.BuildAnimationEntries(0));

                foreach (AnimationEntry entry in liveVdEntries)
                {
                    entry.SourceMode = "VD";

                    if (!entry.SecondaryText.Contains("| VD |", StringComparison.OrdinalIgnoreCase) &&
                        !entry.SecondaryText.EndsWith("| VD", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.SecondaryText += " | VD";
                    }
                }

                combinedEntries = combinedEntries
                    .Where(x => !string.Equals(x.SourceMode, "VD", StringComparison.OrdinalIgnoreCase))
                    .Concat(liveVdEntries)
                    .ToList();
            }

            ApplyLoadedData(
                combinedEntries,
                cacheData.MulSlotEntries ?? new List<MulSlotEntry>());

            await ReportLoadingProgressAsync(100, "加载的缓存动画数据。");

            StatusText =
                "已加载 " + allAnimationEntries.Count +
                " 个动画条目（使用配置文件 " +
                (activeProfile?.ProfileName ?? "Default") + " 的缓存数据）。";

            IsLoading = false;
            OnPropertyChanged(nameof(IsLoading));
            return;
        }

        try
        {
            SelectedSourceText = localPath;
            activeAnimationDataSource = null;

            allAnimationEntries.Clear();
            AnimationEntries.Clear();
            allMulSlotEntries.Clear();
            allUopBodyEntries.Clear();

            const int mulBodiesToScan = 6000;
            const int uopBodiesToScan = 65536;

            FolderLoadResult loadResult = new FolderLoadResult();

            await ReportLoadingProgressAsync(5, "正在初始化动画源...");

            bool shouldLoadUop = ShouldLoadUopForActiveProfile();

            loadResult.MulReady = await Task.Run(() => mulAnimationDataSource.Initialize(localPath));
            await ReportLoadingProgressAsync(15, "MUL 动画源已初始化。");

            if (shouldLoadUop)
            {
                loadResult.UopReady = await Task.Run(() => uopAnimationDataSource.Initialize(localPath));
                await ReportLoadingProgressAsync(25, "UOP 动画源已初始化。");
            }
            else
            {
                loadResult.UopReady = false;
                await ReportLoadingProgressAsync(25, "在此配置中跳过了 UOP 动画源。");
            }

            loadResult.VdReady = await Task.Run(() => vdFolderAnimationDataSource.Initialize(localPath));
            await ReportLoadingProgressAsync(30, "VD 文件夹动画源已初始化。");

            UpdatePreferredAnimationSources();

            if (!loadResult.MulReady && !loadResult.UopReady && !loadResult.VdReady)
            {
                StatusText = "未找到受支持的动画数据源。";
                return;
            }

            if (loadResult.MulReady)
            {
                await ReportLoadingProgressAsync(35, "正在扫描 MUL 动画条目...");

                loadResult.MulEntries =
                    await Task.Run(() => mulAnimationDataSource.BuildAnimationEntries(mulBodiesToScan));

                foreach (AnimationEntry entry in loadResult.MulEntries)
                {
                    entry.SourceMode = "MUL";

                    if (!entry.SecondaryText.Contains("| MUL |", StringComparison.OrdinalIgnoreCase) &&
                        !entry.SecondaryText.EndsWith("| MUL", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.SecondaryText += " | MUL";
                    }
                }

                await ReportLoadingProgressAsync(55, "完成 MUL 动画条目扫描。");
                loadResult.MulSlots = new List<MulSlotEntry>();
            }

            if (loadResult.UopReady)
            {
                await ReportLoadingProgressAsync(60, "正在扫描 UOP 动画条目...");

                loadResult.UopEntries =
                    await Task.Run(() => uopAnimationDataSource.BuildAnimationEntries(uopBodiesToScan));

                foreach (AnimationEntry entry in loadResult.UopEntries)
                {
                    entry.SourceMode = "UOP";

                    if (!entry.SecondaryText.Contains("| UOP |", StringComparison.OrdinalIgnoreCase) &&
                        !entry.SecondaryText.EndsWith("| UOP", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.SecondaryText += " | UOP";
                    }
                }

                await ReportLoadingProgressAsync(78, "完成 UOP 动画条目扫描。");
            }

            if (loadResult.VdReady)
            {
                await ReportLoadingProgressAsync(80, "正在扫描 VD 动画条目...");

                loadResult.VdEntries =
                    await Task.Run(() => vdFolderAnimationDataSource.BuildAnimationEntries(0));

                foreach (AnimationEntry entry in loadResult.VdEntries)
                {
                    entry.SourceMode = "VD";

                    if (!entry.SecondaryText.Contains("| VD |", StringComparison.OrdinalIgnoreCase) &&
                        !entry.SecondaryText.EndsWith("| VD", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.SecondaryText += " | VD";
                    }
                }

                await ReportLoadingProgressAsync(88, "完成 VD 动画条目扫描。");
            }

            await ReportLoadingProgressAsync(90, "正在构建文件列表...");

            List<AnimationEntry> combinedEntries = new List<AnimationEntry>();
            combinedEntries.AddRange(loadResult.MulEntries);
            combinedEntries.AddRange(loadResult.UopEntries);
            combinedEntries.AddRange(loadResult.VdEntries);

            ApplyLoadedData(combinedEntries, loadResult.MulSlots);

            await ReportLoadingProgressAsync(100, "完成加载动画。");

            AnimationCacheData newCache = animationCacheService.BuildCacheData(
                profileId,
                localPath,
                allAnimationEntries.ToList(),
                allMulSlotEntries.ToList());

            await Task.Run(() => animationCacheService.SaveCache(newCache));

            List<string> loadedSources = new List<string>();

            if (loadResult.MulReady)
            {
                loadedSources.Add("MUL");
            }

            if (loadResult.UopReady)
            {
                loadedSources.Add("UOP");
            }

            if (loadResult.VdReady)
            {
                loadedSources.Add("VD");
            }

            StatusText =
                "已加载 " + allAnimationEntries.Count +
                " 个动画条目，来自 " +
                string.Join("、", loadedSources) + "。";
        }
        catch (Exception exception)
        {
            StatusText = "加载动画数据失败: " + exception.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    private async Task LoadCurrentProfileFolderAsync()
    {
        if (isProfileLoadInProgress)
        {
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath))
        {
            SelectedSourceText = "未加载源";
            StatusText = "所选配置还没有文件夹。";
            return;
        }

        if (!Directory.Exists(currentFolderPath))
        {
            SelectedSourceText = currentFolderPath;
            StatusText = "未找到所选配置的文件夹。";
            return;
        }

        try
        {
            isProfileLoadInProgress = true;
            await LoadFolderAsync(currentFolderPath);
        }
        finally
        {
            isProfileLoadInProgress = false;
        }
    }

    public async Task InitializeAsync()
    {
        LoadActiveProfileIntoUi();
        await LoadCurrentProfileFolderAsync();
    }

    private async Task UpdateLoadingProgressAsync(int currentStep, int totalSteps, string message)
    {
        double percent = 0;

        if (totalSteps > 0)
        {
            percent = (double)currentStep / totalSteps * 100.0;
        }

        loadingProgress = percent;
        loadingText = message + " (" + percent.ToString("0") + "%)";

        OnPropertyChanged(nameof(LoadingProgress));
        OnPropertyChanged(nameof(LoadingText));

        await Dispatcher.UIThread.InvokeAsync(() => { });
    }

    private void ReloadAnimationSourcesAndLists()
    {
        const int mulBodiesToScan = 6000;
        const int uopBodiesToScan = 65536;

        string currentFolderPath = GetCurrentFolderPath();

        FolderLoadResult loadResult = LoadAnimationDataForFolder(
            currentFolderPath,
            mulBodiesToScan,
            uopBodiesToScan);

        List<AnimationEntry> combinedEntries = new List<AnimationEntry>();
        combinedEntries.AddRange(loadResult.MulEntries);
        combinedEntries.AddRange(loadResult.UopEntries);

        ApplyLoadedData(combinedEntries, loadResult.MulSlots);

        string profileId = activeProfile?.ProfileId ?? "default";

        AnimationCacheData cacheData = animationCacheService.BuildCacheData(
            profileId,
            currentFolderPath,
            allAnimationEntries.ToList(),
            allMulSlotEntries.ToList());

        animationCacheService.SaveCache(cacheData);
    }
}