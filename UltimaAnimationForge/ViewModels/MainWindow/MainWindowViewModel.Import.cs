using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UltimaAnimationForge.Models;
using UltimaAnimationForge.Services;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Bgra32>;

namespace UltimaAnimationForge.ViewModels;

public partial class MainWindowViewModel
{
    public bool CanAcceptDroppedVdForSelectedMulSlot()
    {
        return ShowMulSlotView &&
               !IsSelectedAnimationFileUop() &&
               SelectedMulSlot != null;
    }

    private sealed class CreateLegacyMulIdxDialogResult
    {
        public bool Confirmed { get; set; }
        public string BaseFileName { get; set; } = string.Empty;
        public char TypeLetter { get; set; } = 'H';
        public int StartBody { get; set; }
        public int EndBody { get; set; } = 500;
    }

    private sealed class BodyAssignmentDialogResult
    {
        public bool Confirmed { get; set; }
        public int BodyId { get; set; }
        public string MobType { get; set; } = "MONSTER";
        public string Comment { get; set; } = string.Empty;
    }

    private sealed class SpriteSheetImportDialogResult
    {
        public bool Confirmed { get; set; }
        public int ActionIndex { get; set; } = 0;
        public int DirectionCount { get; set; } = 5;
        public bool DirectionsInRows { get; set; } = true;
        public int CellWidth { get; set; }
        public int CellHeight { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int HorizontalSpacing { get; set; }
        public int VerticalSpacing { get; set; }
        public bool TrimTransparentBorder { get; set; } = true;
        public int TrimPadding { get; set; } = 0;
        public bool UseMagentaTransparency { get; set; } = true;
        public bool AnchorBottomCenter { get; set; } = true;
    }

    private async Task ImportVdAsync()
    {
        if (!ShowMulSlotView)
        {
            StatusText = "VD 导入仅在空闲 MUL 槽位视图中可用。";
            return;
        }

        if (SelectedMulSlot == null)
        {
            StatusText = "先选择一个空闲的 MUL 槽位。";
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return;
        }

        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "找不到主窗口。";
            return;
        }

        IReadOnlyList<IStorageFile> files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择 VD 文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("VD 文件")
                    {
                        Patterns = new[] { "*.vd" }
                    }
                }
            });

        if (files.Count == 0)
        {
            StatusText = "VD 导入已取消。";
            return;
        }

        string? vdPath = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(vdPath))
        {
            StatusText = "所选 VD 文件没有本地路径。";
            return;
        }

        await ImportVdFromPathAsync(vdPath, false);
    }

    public async Task ImportVdToUopAsync()
    {
        if (!ShowMulSlotView)
        {
            StatusText = "先切换到槽位/目标视图。";
            return;
        }

        if (!IsSelectedAnimationFileUop())
        {
            StatusText = "先选择一个 UOP 动画文件。";
            return;
        }

        if (SelectedUopBodySlot == null)
        {
            StatusText = "先选择一个 UOP 身体目标。";
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return;
        }

        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "找不到主窗口。";
            return;
        }

        IReadOnlyList<IStorageFile> files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择 VD 文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("VD 文件")
                    {
                        Patterns = new[] { "*.vd" }
                    }
                }
            });

        if (files.Count == 0)
        {
            StatusText = "VD -> UOP 导入已取消。";
            return;
        }

        string? vdPath = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(vdPath))
        {
            StatusText = "所选 VD 文件没有本地路径。";
            return;
        }

        await ImportVdToUopFromPathAsync(vdPath);
    }

    private async Task CreateEmptyLegacyMulIdxAsync()
    {
        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return;
        }

        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "找不到主窗口。";
            return;
        }

        CreateLegacyMulIdxDialogResult? dialogResult = await ShowCreateLegacyMulIdxDialogAsync(mainWindow);

        if (dialogResult == null || !dialogResult.Confirmed)
        {
            StatusText = "创建空 MUL/IDX 已取消。";
            return;
        }

        LegacyMulIdxCreationService.CreateResult createResult =
            legacyMulIdxCreationService.CreateEmptyLegacyMulIdx(
                currentFolderPath,
                dialogResult.BaseFileName,
                dialogResult.TypeLetter,
                dialogResult.StartBody,
                dialogResult.EndBody);

        StatusText = createResult.Message;

        if (!createResult.Success)
        {
            return;
        }

        ReloadAnimationSourcesAndLists();
    }

    private int GetFileTypeFromSourceFile(string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(sourceFile))
        {
            return 1;
        }

        string fileName = Path.GetFileName(sourceFile);

        if (string.Equals(fileName, "anim.mul", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (fileName.StartsWith("anim", StringComparison.OrdinalIgnoreCase) &&
            fileName.EndsWith(".mul", StringComparison.OrdinalIgnoreCase))
        {
            string numberPart = fileName.Substring(4, fileName.Length - 8);

            if (int.TryParse(numberPart, out int parsedFileType) && parsedFileType >= 2)
            {
                return parsedFileType;
            }
        }

        return 1;
    }

    private async Task<bool> ConfirmDeleteAnimationAsync(Window owner, AnimationEntry entry, int fileType, int bodyIndex)
    {
        Window dialog = new Window
        {
            Title = "Delete Animation",
            Width = 460,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        bool confirmed = false;

        TextBlock text = new TextBlock
        {
            Text =
                "Delete this animation and free its slot?" + Environment.NewLine + Environment.NewLine +
                "Body ID: " + entry.BodyId + Environment.NewLine +
                "Source file: " + entry.SourceFile + Environment.NewLine +
                "Body slot index: " + bodyIndex + Environment.NewLine +
                "Animation type length: " + GetGroupCountForBody(entry.BodyId),
            TextWrapping = TextWrapping.Wrap
        };

        Button deleteButton = new Button
        {
            Content = "Delete",
            Width = 90
        };

        Button cancelButton = new Button
        {
            Content = "Cancel",
            Width = 90
        };

        deleteButton.Click += (_, _) =>
        {
            confirmed = true;
            dialog.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            confirmed = false;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                text,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children =
                    {
                        cancelButton,
                        deleteButton
                    }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return confirmed;
    }

    private async Task DeleteAnimationAsync()
    {
        if (ShowMulSlotView)
        {
            StatusText = "删除动画仅在动画列表视图中可用。";
            return;
        }

        if (SelectedAnimation == null)
        {
            StatusText = "先选择一个动画。";
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return;
        }

        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "找不到主窗口。";
            return;
        }

        AnimationEntry entry = SelectedAnimation;
        int fileType = GetFileTypeFromSourceFile(entry.SourceFile);
        int animLength = GetGroupCountForBody(entry.BodyId);

        if (fileType <= 1)
        {
            StatusText = "从槽位删除仅用于导入的 anim2-anim6 bodyconv 动画。";
            return;
        }

        int bodyIndex = entry.BodyId;

        if (mulAnimationDataSource.BodyConvEntries.TryGetValue(entry.BodyId, out BodyConvEntry? bodyConvEntry))
        {
            bodyIndex = bodyConvEntry.NewBodyId;
        }
        else
        {
            StatusText = "无法解析此动画的 bodyconv 映射。";
            return;
        }

        bool confirmed = await ConfirmDeleteAnimationAsync(mainWindow, entry, fileType, bodyIndex);
        if (!confirmed)
        {
            StatusText = "删除已取消。";
            return;
        }

        string idxFileName = entry.SourceFile.EndsWith(".idx", StringComparison.OrdinalIgnoreCase)
            ? entry.SourceFile
            : Path.ChangeExtension(entry.SourceFile, ".idx");

        MulSlotDeleteService.DeleteResult deleteResult =
            mulSlotDeleteService.DeleteBodySlot(
                currentFolderPath,
                idxFileName,
                fileType,
                bodyIndex,
                animLength);

        if (!deleteResult.Success)
        {
            StatusText = deleteResult.Message;
            return;
        }

        string bodyConvPath = Path.Combine(currentFolderPath, "bodyconv.def");

        try
        {
            bodyConvAssignmentService.RemoveBodyId(bodyConvPath, entry.BodyId);

            string mobTypesPath = Path.Combine(currentFolderPath, "mobtypes.txt");
            mobTypeAssignmentService.RemoveBodyId(mobTypesPath, entry.BodyId);
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "动画槽位已清除，但由于访问被拒绝，bodyconv.def 无法更新。";
            ReloadAnimationSourcesAndLists();
            return;
        }
        catch (IOException exception)
        {
            StatusText = "动画槽位已清除，但更新 bodyconv.def 失败：" + exception.Message;
            ReloadAnimationSourcesAndLists();
            return;
        }

        ReloadAnimationSourcesAndLists();

        SelectedAnimation = null;
        ClearDecodedFramesAndThumbnails();

        StatusText =
            "Deleted body ID " + entry.BodyId +
            " from " + entry.SourceFile +
            " and freed its slot.";
    }

    private async Task SavePendingChangesAsync()
    {
        if (!QueueCurrentEditedMulAnimation())
        {
            return;
        }

        bool hasQueuedMulImports = pendingMulImportSession.HasUnsavedChanges;

        if (!hasQueuedMulImports)
        {
            StatusText = "没有待保存的更改。";
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return;
        }

        try
        {
            foreach (PendingMulImportSession.PendingMulFileEdit edit in pendingMulImportSession.FileEdits.Values)
            {
                long nextMulOffset;

                using (FileStream mulStream = new FileStream(edit.MulPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    mulStream.Seek(0, SeekOrigin.End);
                    nextMulOffset = mulStream.Position;

                    foreach (PendingMulImportSession.PendingMulBlock block in edit.PendingBlocksBySlotIndex.Values.OrderBy(x => x.SlotIndex))
                    {
                        mulStream.Write(block.BlockData, 0, block.BlockData.Length);

                        edit.WorkingIdxEntries[block.SlotIndex].Offset = checked((int)nextMulOffset);
                        edit.WorkingIdxEntries[block.SlotIndex].Length = block.BlockData.Length;
                        edit.WorkingIdxEntries[block.SlotIndex].Extra = block.Extra;

                        nextMulOffset += block.BlockData.Length;
                    }
                }

                using (BinaryWriter writer = new BinaryWriter(File.Open(edit.IdxPath, FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    foreach (AnimationIdxEntry entry in edit.WorkingIdxEntries)
                    {
                        writer.Write(entry.Offset);
                        writer.Write(entry.Length);
                        writer.Write(entry.Extra);
                    }
                }
            }

            string bodyConvPath = Path.Combine(currentFolderPath, "bodyconv.def");
            foreach (PendingMulImportSession.PendingBodyConvEntry entry in pendingMulImportSession.PendingBodyConvEntries.Values.OrderBy(x => x.BodyId))
            {
                if (!bodyConvAssignmentService.BodyIdExists(bodyConvPath, entry.BodyId))
                {
                    bodyConvAssignmentService.AddNewEntry(bodyConvPath, entry.BodyId, entry.FileType, entry.SlotBodyIndex, entry.Comment);
                }
            }

            string mobTypesPath = Path.Combine(currentFolderPath, "mobtypes.txt");
            foreach (PendingMulImportSession.PendingMobTypeEntry entry in pendingMulImportSession.PendingMobTypeEntries.Values.OrderBy(x => x.BodyId))
            {
                mobTypeAssignmentService.AddOrUpdateEntry(mobTypesPath, entry.BodyId, entry.MobType, entry.Comment);
            }

            pendingMulImportSession.Clear();

            ReloadAnimationSourcesAndLists();
            RefreshUnsavedChangesState();

            StatusText = "已保存所有排队的 MUL 更改。";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "保存待处理更改时访问被拒绝。";
        }
        catch (IOException exception)
        {
            StatusText = "保存待处理更改时出现 I/O 错误：" + exception.Message;
        }
        catch (Exception exception)
        {
            StatusText = "保存待处理更改失败：" + exception.Message;
        }

        await Task.CompletedTask;
    }

    private bool QueueCurrentEditedMulAnimation()
    {
        if (!hasFrameEdits)
        {
            return true;
        }

        if (SelectedAnimation == null || currentResolvedAnimationBlock == null)
        {
            StatusText = "未选择编辑的动画。";
            return false;
        }

        if (currentResolvedAnimationBlock.IsUop)
        {
            StatusText = "排队的多编辑保存更改对于已编辑的 UOP 动画尚未实现。";
            return false;
        }

        if (editableFrames.Count == 0)
        {
            StatusText = "没有编辑的帧要排队。";
            return false;
        }

        byte[] blockData = mulEditedFrameSaveService.BuildEditedDirectionBlock(editableFrames);
        if (blockData.Length == 0)
        {
            StatusText = "无法生成编辑的 MUL 块数据。";
            return false;
        }

        List<AnimationIdxEntry> workingIdxEntries;

        if (pendingMulImportSession.TryGetFileEdit(
                currentResolvedAnimationBlock.IdxPath,
                out PendingMulImportSession.PendingMulFileEdit? existingFileEdit))
        {
            workingIdxEntries = existingFileEdit.WorkingIdxEntries;
        }
        else
        {
            workingIdxEntries = ReadIdxEntriesForPendingEdit(currentResolvedAnimationBlock.IdxPath);
        }

        PendingMulImportSession.PendingMulFileEdit fileEdit =
            pendingMulImportSession.GetOrCreateFileEdit(
                currentResolvedAnimationBlock.MulPath,
                currentResolvedAnimationBlock.IdxPath,
                workingIdxEntries);

        fileEdit.PendingBlocksBySlotIndex[currentResolvedAnimationBlock.SlotIndex] =
            new PendingMulImportSession.PendingMulBlock
            {
                SlotIndex = currentResolvedAnimationBlock.SlotIndex,
                Extra = currentResolvedAnimationBlock.Extra,
                BlockData = blockData
            };

        hasFrameEdits = false;
        OnPropertyChanged(nameof(HasFrameEdits));
        RefreshUnsavedChangesState();

        StatusText =
            "Queued edited frames for body " + SelectedAnimation.BodyId +
            ", action " + GetSelectedActionIndex() +
            ", direction " + GetSelectedDirectionIndex() +
            ". Click Save Changes to write all queued edits.";

        return true;
    }

    private List<AnimationIdxEntry> ReadIdxEntriesForPendingEdit(string idxPath)
    {
        List<AnimationIdxEntry> result = new List<AnimationIdxEntry>();

        if (string.IsNullOrWhiteSpace(idxPath) || !File.Exists(idxPath))
        {
            return result;
        }

        using FileStream stream = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using BinaryReader reader = new BinaryReader(stream);

        int index = 0;

        while (reader.BaseStream.Position + 12 <= reader.BaseStream.Length)
        {
            result.Add(new AnimationIdxEntry
            {
                Offset = reader.ReadInt32(),
                Length = reader.ReadInt32(),
                Extra = reader.ReadInt32(),
                Index = index
            });

            index++;
        }

        return result;
    }

    private void EnsureMulSlotEntriesLoaded()
    {
        if (allMulSlotEntries.Count > 0)
        {
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();
        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            return;
        }

        if (activeAnimationDataSource == null && allAnimationEntries.Count == 0)
        {
            return;
        }

        List<MulSlotEntry> slots = mulAnimationDataSource.GetMulSlotEntries();

        allMulSlotEntries.Clear();
        allMulSlotEntries.AddRange(slots);

        RebuildAnimationFileOptions();
        ApplyMulSlotFilters();
        ApplyUopBodyFilters();
    }

    private bool IsAnimSeriesFileName(string? baseFileName)
    {
        string normalizedBaseName = (baseFileName ?? string.Empty).Trim();

        if (normalizedBaseName.EndsWith(".mul", StringComparison.OrdinalIgnoreCase))
        {
            normalizedBaseName = normalizedBaseName[..^4];
        }
        else if (normalizedBaseName.EndsWith(".idx", StringComparison.OrdinalIgnoreCase))
        {
            normalizedBaseName = normalizedBaseName[..^4];
        }

        return string.Equals(normalizedBaseName, "anim", StringComparison.OrdinalIgnoreCase) ||
               (normalizedBaseName.StartsWith("anim", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(normalizedBaseName.Substring(4), out int parsedNumber) &&
                parsedNumber > 0);
    }

    public async Task HandleDroppedFilesAsync(IReadOnlyList<string> droppedPaths)
    {
        if (droppedPaths == null || droppedPaths.Count == 0)
        {
            StatusText = "未放置任何文件。";
            return;
        }

        string? folderPath = droppedPaths.FirstOrDefault(Directory.Exists);
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            await HandleDroppedFolderAsync(folderPath);
            return;
        }

        string? vdPath = droppedPaths.FirstOrDefault(path =>
            File.Exists(path) &&
            path.EndsWith(".vd", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(vdPath))
        {
            await HandleDroppedVdAsync(vdPath);
            return;
        }

        StatusText = "放置 UO 文件夹、VD 文件或 PNG 文件到预览区域。";
    }

    public async Task HandlePreviewDroppedFilesAsync(IReadOnlyList<string> droppedPaths)
    {
        if (droppedPaths == null || droppedPaths.Count == 0)
        {
            StatusText = "未放置任何文件到预览。";
            return;
        }

        List<string> pngPaths = droppedPaths
            .Where(path =>
                File.Exists(path) &&
                path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), new NaturalFileNameComparer())
            .ToList();

        if (pngPaths.Count == 0)
        {
            StatusText = "预览拖放仅接受 PNG 文件。";
            return;
        }

        if (pngPaths.Count == 1)
        {
            await ReplaceSelectedFrameFromPathAsync(pngPaths[0]);
            return;
        }

        await ImportPngSequenceFromPathsAsync(pngPaths);
    }

    private async Task HandleDroppedFolderAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            StatusText = "放置的文件夹无效。";
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
                UoFolderPath = folderPath
            };

            appSettings.Profiles.Add(activeProfile);
            appSettings.LastActiveProfileId = activeProfile.ProfileId;
        }
        else
        {
            activeProfile.UoFolderPath = folderPath;
            appSettings.LastActiveProfileId = activeProfile.ProfileId;
        }

        SaveActiveProfileUiState();
        RebuildProfileOptions();

        await LoadFolderAsync(folderPath);
    }

    private async Task HandleDroppedVdAsync(string vdPath)
    {
        if (string.IsNullOrWhiteSpace(vdPath) || !File.Exists(vdPath))
        {
            StatusText = "放置的 VD 文件无效。";
            return;
        }

        if (!ShowMulSlotView)
        {
            StatusText = "在放置 VD 文件之前先切换到槽位视图。";
            return;
        }

        if (IsSelectedAnimationFileUop())
        {
            await ImportVdToUopFromPathAsync(vdPath);
        }
        else
        {
            await ImportVdFromPathAsync(vdPath, true);
        }
    }

    private async Task ImportVdFromPathAsync(string vdPath, bool allowFileNameAutoAssign)
    {
        if (!ShowMulSlotView)
        {
            StatusText = "VD 导入仅在空闲 MUL 槽位视图中可用。";
            return;
        }

        if (SelectedMulSlot == null)
        {
            StatusText = "先选择一个空闲的 MUL 槽位。";
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return;
        }

        MulSlotEntry importedTargetSlot = SelectedMulSlot;

        VdImportService.ImportPlan plan = vdImportService.BuildMulImportPlan(currentFolderPath, vdPath, importedTargetSlot);

        StatusText = plan.Message;

        if (!plan.Success)
        {
            return;
        }

        PendingMulImportSession.PendingMulFileEdit fileEdit =
            pendingMulImportSession.GetOrCreateFileEdit(
                plan.MulPath,
                plan.IdxPath,
                plan.WorkingIdxEntries);

        fileEdit.WorkingIdxEntries = plan.WorkingIdxEntries;

        foreach (VdImportService.PendingImportEntry entry in plan.PopulatedEntries)
        {
            fileEdit.PendingBlocksBySlotIndex[entry.TargetIndex] =
                new PendingMulImportSession.PendingMulBlock
                {
                    SlotIndex = entry.TargetIndex,
                    Extra = entry.Extra,
                    BlockData = entry.BlockData
                };
        }

        if (importedTargetSlot.FileType >= 2)
        {
            BodyAssignmentDialogResult? assignmentResult = null;

            if (allowFileNameAutoAssign && TryParseVdFileNameAssignment(vdPath, out VdFileNameAssignment? parsedAssignment) && parsedAssignment != null)
            {
                assignmentResult = new BodyAssignmentDialogResult
                {
                    Confirmed = true,
                    BodyId = parsedAssignment.BodyId,
                    MobType = parsedAssignment.MobType,
                    Comment = parsedAssignment.Comment
                };
            }
            else
            {
                Window? mainWindow = GetMainWindow();
                if (mainWindow == null)
                {
                    StatusText = "找不到主窗口。";
                    return;
                }

                string bodyConvPath = Path.Combine(currentFolderPath, "bodyconv.def");

                assignmentResult =
                    await ShowBodyAssignmentDialogAsync(mainWindow, importedTargetSlot, bodyConvPath);
            }

            if (assignmentResult == null || !assignmentResult.Confirmed)
            {
                StatusText = "VD 导入在身体分配前已取消。";
                return;
            }

            pendingMulImportSession.AddOrReplaceBodyConvEntry(
                assignmentResult.BodyId,
                importedTargetSlot.FileType,
                importedTargetSlot.BodyIndex,
                assignmentResult.Comment);

            pendingMulImportSession.AddOrReplaceMobTypeEntry(
                assignmentResult.BodyId,
                assignmentResult.MobType,
                assignmentResult.Comment);
        }
        LoadQueuedVdImportPreview(importedTargetSlot, plan);
        RefreshUnsavedChangesState();

        StatusText =
            "为 " +
            Path.GetFileName(plan.MulPath) +
            " 槽位 " + importedTargetSlot.BodyIndex +
            " 排队 VD 导入。点击保存更改以写入磁盘。";
    }

    private void LoadQueuedVdImportPreview(MulSlotEntry targetSlot, VdImportService.ImportPlan plan)
    {
        int actionIndex = GetSelectedActionIndex();
        int directionIndex = GetSelectedDirectionIndex();

        if (actionIndex < 0 || actionIndex >= targetSlot.AnimLength)
        {
            actionIndex = 0;
        }

        if (directionIndex < 0 || directionIndex > 4)
        {
            directionIndex = 0;
        }

        int localIndex = (actionIndex * 5) + directionIndex;
        int targetIndex = plan.BaseIndex + localIndex;

        VdImportService.PendingImportEntry? pendingEntry =
            plan.PopulatedEntries.FirstOrDefault(x => x.TargetIndex == targetIndex);

        if (pendingEntry == null || pendingEntry.BlockData.Length == 0)
        {
            StatusText = "VD 导入已排队，但选择的动作/方向没有帧数据。";
            return;
        }

        ClearDecodedFramesAndThumbnails();

        DecodeMulAnimationFromOffset(
            pendingEntry.BlockData,
            512,
            "Queued VD import preview: " + Path.GetFileName(plan.MulPath) +
            " slot " + targetSlot.BodyIndex);

        StatusText =
            "Previewing queued VD import for " +
            Path.GetFileName(plan.MulPath) +
            " slot " + targetSlot.BodyIndex +
            ". Click Save Changes to write to disk.";
    }

    private static bool TryParseVdFileNameAssignment(string vdPath, out VdFileNameAssignment? assignment)
    {
        assignment = null;

        string fileName = Path.GetFileNameWithoutExtension(vdPath).Trim();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        if (TryParseDoubleUnderscoreVdName(fileName, out assignment))
        {
            return true;
        }

        return TryParseSingleUnderscoreVdName(fileName, out assignment);
    }

    private static bool TryParseDoubleUnderscoreVdName(string fileName, out VdFileNameAssignment? assignment)
    {
        assignment = null;

        string[] parts = fileName.Split(
            new[] { "__" },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 3)
        {
            return false;
        }

        string comment = parts[0];
        string mobType = NormalizeParsedMobType(parts[1]);

        if (!IsValidParsedMobType(mobType))
        {
            return false;
        }

        if (!int.TryParse(parts[2], out int bodyId) || bodyId < 1)
        {
            return false;
        }

        assignment = new VdFileNameAssignment
        {
            BodyId = bodyId,
            MobType = mobType,
            Comment = comment
        };

        return !string.IsNullOrWhiteSpace(comment);
    }

    private static bool TryParseSingleUnderscoreVdName(string fileName, out VdFileNameAssignment? assignment)
    {
        assignment = null;

        string[] validMobTypes =
        {
        "SEA_MONSTER",
        "MONSTER",
        "ANIMAL",
        "HUMAN",
        "EQUIPMENT"
    };

        foreach (string mobType in validMobTypes)
        {
            string marker = "_" + mobType + "_";
            int markerIndex = fileName.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (markerIndex < 0)
            {
                continue;
            }

            string comment = fileName.Substring(0, markerIndex).Trim('_', ' ');
            string bodyText = fileName.Substring(markerIndex + marker.Length).Trim();

            if (!int.TryParse(bodyText, out int bodyId) || bodyId < 1)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                continue;
            }

            assignment = new VdFileNameAssignment
            {
                BodyId = bodyId,
                MobType = mobType,
                Comment = comment
            };

            return true;
        }

        return false;
    }

    private static string NormalizeParsedMobType(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Replace(' ', '_')
            .ToUpperInvariant();
    }

    private static bool IsValidParsedMobType(string mobType)
    {
        return
            string.Equals(mobType, "MONSTER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mobType, "SEA_MONSTER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mobType, "ANIMAL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mobType, "HUMAN", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mobType, "EQUIPMENT", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ImportVdToUopFromPathAsync(string vdPath)
    {
        if (!ShowMulSlotView)
        {
            StatusText = "先切换到槽位/目标视图。";
            return;
        }

        if (!IsSelectedAnimationFileUop())
        {
            StatusText = "先选择一个 UOP 动画文件。";
            return;
        }

        if (SelectedUopBodySlot == null)
        {
            StatusText = "先选择一个 UOP 身体目标。";
            return;
        }

        string currentFolderPath = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return;
        }

        UopVdImportService.ImportResult result = new UopVdImportService()
            .ImportVdToUop(
                currentFolderPath,
                vdPath,
                SelectedUopBodySlot.BodyId,
                SelectedUopBodySlot.FileName);

        StatusText = result.Message;

        if (result.Success)
        {
            ReloadAnimationSourcesAndLists();
        }

        await Task.CompletedTask;
    }

    private async Task ImportSpriteSheetAsync()
    {
        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "找不到主窗口。";
            return;
        }

        if (ShowMulSlotView)
        {
            if (IsSelectedAnimationFileUop())
            {
                StatusText = "精灵表导入到空槽位目前仅支持 MUL 目标。";
                return;
            }

            if (SelectedMulSlot == null)
            {
                StatusText = "先选择一个空闲的 MUL 槽位。";
                return;
            }
        }
        else
        {
            if (SelectedAnimation == null || currentResolvedAnimationBlock == null)
            {
                StatusText = "先选择一个动画身体/动作。";
                return;
            }

            if (currentResolvedAnimationBlock.IsUop)
            {
                StatusText = "精灵表导入目前仅支持 MUL 动画编辑。";
                return;
            }
        }

        IReadOnlyList<IStorageFile> files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择雪碧图 PNG",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PNG files")
                    {
                        Patterns = new[] { "*.png" }
                    }
                }
            });

        if (files.Count == 0)
        {
            StatusText = "精灵表导入已取消。";
            return;
        }

        string? filePath = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            StatusText = "所选精灵表没有有效的本地路径。";
            return;
        }

        using ImageSharpImage sheetImage = SixLabors.ImageSharp.Image.Load<Bgra32>(filePath);

        int selectedActionIndex = GetSelectedActionIndex();

        SpriteSheetImportDialogResult? dialogResult =
            await ShowSpriteSheetImportDialogAsync(
                mainWindow,
                sheetImage.Width,
                sheetImage.Height,
                selectedActionIndex);

        if (dialogResult == null || !dialogResult.Confirmed)
        {
            StatusText = "精灵表导入已取消。";
            return;
        }

        if (ShowMulSlotView && SelectedMulSlot != null)
        {
            if (dialogResult.ActionIndex < 0 || dialogResult.ActionIndex >= SelectedMulSlot.AnimLength)
            {
                StatusText =
                    "动作 " + dialogResult.ActionIndex +
                    " 在所选槽位动作范围之外(0-" + (SelectedMulSlot.AnimLength - 1) + ")。";
                return;
            }
        }

        Dictionary<int, List<VdFrameData>> importedByDirection =
            BuildFramesFromSpriteSheetByDirection(sheetImage, dialogResult);

        if (importedByDirection.Count == 0)
        {
            StatusText = "精灵表中未找到任何帧。";
            return;
        }

        if (playbackTimer != null)
        {
            playbackTimer.Stop();
        }

        hasImportedSpriteSheetSession = true;
        importedSpriteSheetLastActionIndex = dialogResult.ActionIndex;
        importedSpriteSheetDirectionCount = dialogResult.DirectionCount;
        importedSpriteSheetSourceName = Path.GetFileName(filePath);

        importedSpriteSheetActions[dialogResult.ActionIndex] =
            importedByDirection.ToDictionary(
                x => x.Key,
                x => x.Value);

        if (ShowMulSlotView)
        {
            RebuildActionListForSelectedMulSlot();
        }
        else
        {
            if (!ActionOptions.Any(x =>
                actionNameToIndex.TryGetValue(x, out int idx) && idx == dialogResult.ActionIndex))
            {
                RebuildActionListForBody(GetEffectiveSelectedBodyId(SelectedAnimation));
            }
        }

        string? selectedActionLabel = ActionOptions.FirstOrDefault(x =>
            actionNameToIndex.TryGetValue(x, out int idx) && idx == dialogResult.ActionIndex);

        if (!string.IsNullOrWhiteSpace(selectedActionLabel))
        {
            suppressActionReload = true;
            SelectedAction = selectedActionLabel;
            suppressActionReload = false;
        }

        int selectedDirectionIndex = GetSelectedDirectionIndex();
        if (!importedByDirection.ContainsKey(selectedDirectionIndex))
        {
            selectedDirectionIndex = importedByDirection.Keys.OrderBy(x => x).First();

            string? selectedDirectionLabel = DirectionOptions.FirstOrDefault(x =>
                directionNameToIndex.TryGetValue(x, out int idx) && idx == selectedDirectionIndex);

            if (!string.IsNullOrWhiteSpace(selectedDirectionLabel))
            {
                suppressDirectionReload = true;
                SelectedDirection = selectedDirectionLabel;
                suppressDirectionReload = false;
            }
        }

        LoadImportedSpriteSheetDirectionPreview(selectedDirectionIndex);

        bool queued = ShowMulSlotView
            ? QueueImportedSpriteSheetIntoSelectedMulSlot()
            : QueueImportedSpriteSheetDirections();

        if (!queued)
        {
            StatusText = "加载精灵表预览，但队列导入方向失败。";
            return;
        }

        if (ShowMulSlotView && SelectedMulSlot != null)
        {
            StatusText =
                "加载精灵表动作 " + dialogResult.ActionIndex +
                "，共 " + importedSpriteSheetDirections.Count +
                " 个导入方向到空槽 " + SelectedMulSlot.FileName +
                " 身体 " + SelectedMulSlot.BodyIndex +
                "。点击保存更改写入。";
        }
        else
        {
            StatusText =
                "加载精灵表动作 " + dialogResult.ActionIndex +
                "，共 " + importedSpriteSheetDirections.Count +
                " 个导入方向自 " + Path.GetFileName(filePath) +
                "。点击保存更改写入。";
        }
    }

    private Dictionary<int, List<VdFrameData>> BuildFramesFromSpriteSheetByDirection(
        ImageSharpImage sheetImage,
        SpriteSheetImportDialogResult options)
    {
        Dictionary<int, List<VdFrameData>> result = new Dictionary<int, List<VdFrameData>>();

        int directionCount = Math.Max(1, Math.Min(5, options.DirectionCount));
        int framesPerDirection = options.DirectionsInRows ? options.Columns : options.Rows;

        for (int directionIndex = 0; directionIndex < directionCount; directionIndex++)
        {
            List<(ImageSharpImage image, int anchorX, int anchorY)> rawFrames =
                new List<(ImageSharpImage image, int anchorX, int anchorY)>();

            for (int frameIndex = 0; frameIndex < framesPerDirection; frameIndex++)
            {
                int cellX;
                int cellY;

                if (options.DirectionsInRows)
                {
                    cellX = options.StartX + (frameIndex * (options.CellWidth + options.HorizontalSpacing));
                    cellY = options.StartY + (directionIndex * (options.CellHeight + options.VerticalSpacing));
                }
                else
                {
                    cellX = options.StartX + (directionIndex * (options.CellWidth + options.HorizontalSpacing));
                    cellY = options.StartY + (frameIndex * (options.CellHeight + options.VerticalSpacing));
                }

                if (cellX < 0 || cellY < 0 ||
                    cellX + options.CellWidth > sheetImage.Width ||
                    cellY + options.CellHeight > sheetImage.Height)
                {
                    continue;
                }

                using ImageSharpImage cropped = sheetImage.Clone(ctx =>
                    ctx.Crop(new SixLabors.ImageSharp.Rectangle(
                        cellX,
                        cellY,
                        options.CellWidth,
                        options.CellHeight)));

                ImageSharpImage working = cropped.Clone();

                if (options.UseMagentaTransparency)
                {
                    KeyOutMagenta(working);
                }

                if (options.TrimTransparentBorder)
                {
                    working = TrimTransparentImage(working, options.TrimPadding, out int trimLeft, out int trimTop);

                    int anchorX = options.AnchorBottomCenter
                        ? (options.CellWidth / 2) - trimLeft
                        : working.Width / 2;

                    int anchorY = options.AnchorBottomCenter
                        ? (options.CellHeight - 1) - trimTop
                        : working.Height / 2;

                    rawFrames.Add((working, anchorX, anchorY));
                }
                else
                {
                    int anchorX = options.AnchorBottomCenter ? (options.CellWidth / 2) : (working.Width / 2);
                    int anchorY = options.AnchorBottomCenter ? (options.CellHeight - 1) : (working.Height / 2);

                    rawFrames.Add((working, anchorX, anchorY));
                }
            }

            List<VdFrameData> normalizedFrames = NormalizeSpriteSheetFrames(rawFrames);
            if (normalizedFrames.Count > 0)
            {
                result[directionIndex] = normalizedFrames;
            }
        }

        return result;
    }

    private List<VdFrameData> NormalizeSpriteSheetFrames(
        List<(ImageSharpImage image, int anchorX, int anchorY)> rawFrames)
    {
        if (rawFrames == null || rawFrames.Count == 0)
        {
            return new List<VdFrameData>();
        }

        int maxLeft = rawFrames.Max(x => x.anchorX);
        int maxTop = rawFrames.Max(x => x.anchorY);
        int maxRight = rawFrames.Max(x => x.image.Width - x.anchorX);
        int maxBottom = rawFrames.Max(x => x.image.Height - x.anchorY);

        int finalWidth = Math.Max(1, maxLeft + maxRight);
        int finalHeight = Math.Max(1, maxTop + maxBottom);

        List<VdFrameData> result = new List<VdFrameData>();

        for (int i = 0; i < rawFrames.Count; i++)
        {
            var raw = rawFrames[i];

            using ImageSharpImage canvas = new ImageSharpImage(
                finalWidth,
                finalHeight,
                new Bgra32(0, 0, 0, 0));

            int drawX = maxLeft - raw.anchorX;
            int drawY = maxTop - raw.anchorY;

            canvas.Mutate(ctx => ctx.DrawImage(
                raw.image,
                new SixLabors.ImageSharp.Point(drawX, drawY),
                1f));

            WriteableBitmap bitmap = ConvertImageSharpToWriteableBitmap(canvas);

            result.Add(new VdFrameData
            {
                Bitmap = bitmap,
                Palette565 = null,
                CenterX = (short)maxLeft,
                CenterY = (short)maxTop,
                Width = (ushort)finalWidth,
                Height = (ushort)finalHeight,
                InitCoordsX = 0,
                InitCoordsY = 0,
                EndCoordsX = 0,
                EndCoordsY = 0,
                FrameId = (ushort)i,
                FrameNumber = (ushort)i,
                DataOffset = 0
            });
        }

        return result;
    }

    private void LoadImportedSpriteSheetDirectionPreview(int directionIndex)
    {
        int actionIndex = GetSelectedActionIndex();

        if (!TryGetImportedSpriteSheetDirectionsForAction(
                actionIndex,
                out Dictionary<int, List<VdFrameData>> directions) ||
            !directions.TryGetValue(directionIndex, out List<VdFrameData>? frames) ||
            frames == null ||
            frames.Count == 0)
        {
            StatusText =
                "动作 " + actionIndex +
                "、方向 " + directionIndex + " 不存在导入的精灵表帧。";
            return;
        }

        ClearDecodedFramesAndThumbnails();

        foreach (VdFrameData frame in frames)
        {
            editableFrames.Add(frame);
            decodedFrames.Add(frame.Bitmap);
        }

        currentFrameIndex = 0;
        PreviewBitmap = decodedFrames[0];

        RebuildFrameThumbnails();

        hasFrameEdits = true;
        OnPropertyChanged(nameof(HasFrameEdits));
        OnPropertyChanged(nameof(HasAnyUnsavedChanges));
        OnPropertyChanged(nameof(CanSaveChanges));
        OnPropertyChanged(nameof(UnsavedChangesText));
    }

    private bool QueueImportedSpriteSheetDirections()
    {
        if (!hasImportedSpriteSheetSession)
        {
            return false;
        }

        if (SelectedAnimation == null)
        {
            StatusText = "不存在所选精灵表导入的动画。";
            return false;
        }

        int bodyId = GetEffectiveSelectedBodyId(SelectedAnimation);
        if (bodyId < 0)
        {
            StatusText = "无法确定精灵表导入的身体 ID。";
            return false;
        }

        IAnimationDataSource? dataSource = GetDataSourceForEntry(SelectedAnimation);
        if (dataSource == null)
        {
            StatusText = "精灵表导入没有可用的 MUL 动画数据源。";
            return false;
        }

        if (currentResolvedAnimationBlock == null || currentResolvedAnimationBlock.IsUop)
        {
            StatusText = "精灵表导入目前仅队列 MUL 方向。";
            return false;
        }

        int actionIndex = GetSelectedActionIndex();

        if (!TryGetImportedSpriteSheetDirectionsForAction(
                actionIndex,
                out Dictionary<int, List<VdFrameData>> directions) ||
            directions.Count == 0)
        {
            StatusText = "动作 " + actionIndex + " 不存在导入的精灵表数据。";
            return false;
        }

        List<AnimationIdxEntry> workingIdxEntries;

        if (pendingMulImportSession.TryGetFileEdit(
                currentResolvedAnimationBlock.IdxPath,
                out PendingMulImportSession.PendingMulFileEdit? existingFileEdit))
        {
            workingIdxEntries = existingFileEdit!.WorkingIdxEntries;
        }
        else
        {
            workingIdxEntries = ReadIdxEntriesForPendingEdit(currentResolvedAnimationBlock.IdxPath);
        }

        PendingMulImportSession.PendingMulFileEdit fileEdit =
            pendingMulImportSession.GetOrCreateFileEdit(
                currentResolvedAnimationBlock.MulPath,
                currentResolvedAnimationBlock.IdxPath,
                workingIdxEntries);

        foreach (KeyValuePair<int, List<VdFrameData>> pair in directions.OrderBy(x => x.Key))
        {
            int directionIndex = pair.Key;
            List<VdFrameData> frames = pair.Value;

            if (frames == null || frames.Count == 0)
            {
                continue;
            }

            if (!dataSource.TryResolveAnimationBlock(
                    bodyId,
                    actionIndex,
                    directionIndex,
                    out ResolvedAnimationBlock resolvedBlock))
            {
                continue;
            }

            if (resolvedBlock.IsUop)
            {
                continue;
            }

            byte[] blockData = mulEditedFrameSaveService.BuildEditedDirectionBlock(frames);
            if (blockData.Length == 0)
            {
                continue;
            }

            fileEdit.PendingBlocksBySlotIndex[resolvedBlock.SlotIndex] =
                new PendingMulImportSession.PendingMulBlock
                {
                    SlotIndex = resolvedBlock.SlotIndex,
                    Extra = resolvedBlock.Extra,
                    BlockData = blockData
                };

            fileEdit.WorkingIdxEntries[resolvedBlock.SlotIndex].Offset = -2;
            fileEdit.WorkingIdxEntries[resolvedBlock.SlotIndex].Length = blockData.Length;
            fileEdit.WorkingIdxEntries[resolvedBlock.SlotIndex].Extra = resolvedBlock.Extra;
        }

        RefreshUnsavedChangesState();
        return true;
    }

    private bool QueueImportedSpriteSheetIntoSelectedMulSlot()
    {
        if (!hasImportedSpriteSheetSession)
        {
            StatusText = "不存在导入的精灵表会话。";
            return false;
        }

        if (!ShowMulSlotView)
        {
            StatusText = "精灵表槽位队列仅在空闲 MUL 槽位视图中有效。";
            return false;
        }

        if (SelectedMulSlot == null)
        {
            StatusText = "未选择空闲的 MUL 槽位。";
            return false;
        }

        if (IsSelectedAnimationFileUop())
        {
            StatusText = "精灵表导入到空槽位目前仅支持 MUL 目标。";
            return false;
        }

        if (!SelectedMulSlot.IsEmpty)
        {
            StatusText = "所选 MUL 槽位不为空。";
            return false;
        }

        int actionIndex = GetSelectedActionIndex();

        if (!TryGetImportedSpriteSheetDirectionsForAction(
                actionIndex,
                out Dictionary<int, List<VdFrameData>> directions) ||
            directions.Count == 0)
        {
            StatusText = "动作 " + actionIndex + " 不存在导入的精灵表数据。";
            return false;
        }

        if (actionIndex < 0 || actionIndex >= SelectedMulSlot.AnimLength)
        {
            StatusText =
                "精灵表动作 " + actionIndex +
                " 在所选槽位动作范围之外(0-" + (SelectedMulSlot.AnimLength - 1) + ")。";
            return false;
        }

        string currentFolderPath = GetCurrentFolderPath();
        if (string.IsNullOrWhiteSpace(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            StatusText = "先打开有效的 UO 文件夹。";
            return false;
        }

        string idxPath = Path.Combine(currentFolderPath, SelectedMulSlot.FileName);
        string mulPath = Path.Combine(currentFolderPath, Path.ChangeExtension(SelectedMulSlot.FileName, ".mul"));

        if (!File.Exists(idxPath))
        {
            StatusText = "未找到目标 IDX 文件：" + Path.GetFileName(idxPath);
            return false;
        }

        if (!File.Exists(mulPath))
        {
            StatusText = "未找到目标 MUL 文件：" + Path.GetFileName(mulPath);
            return false;
        }

        List<AnimationIdxEntry> workingIdxEntries;

        if (pendingMulImportSession.TryGetFileEdit(
                idxPath,
                out PendingMulImportSession.PendingMulFileEdit? existingFileEdit))
        {
            workingIdxEntries = existingFileEdit!.WorkingIdxEntries;
        }
        else
        {
            workingIdxEntries = ReadIdxEntriesForPendingEdit(idxPath);
        }

        int baseIndex = VdImportService.GetLegacyBaseIndexForBody(SelectedMulSlot.FileType, SelectedMulSlot.BodyIndex);
        int requiredEntryCount = baseIndex + (SelectedMulSlot.AnimLength * 5);

        while (workingIdxEntries.Count < requiredEntryCount)
        {
            workingIdxEntries.Add(new AnimationIdxEntry
            {
                Offset = -1,
                Length = -1,
                Extra = -1,
                Index = workingIdxEntries.Count
            });
        }

        PendingMulImportSession.PendingMulFileEdit fileEdit =
            pendingMulImportSession.GetOrCreateFileEdit(
                mulPath,
                idxPath,
                workingIdxEntries);

        int queuedDirections = 0;

        foreach (KeyValuePair<int, List<VdFrameData>> pair in directions.OrderBy(x => x.Key))
        {
            int directionIndex = pair.Key;
            List<VdFrameData> frames = pair.Value;

            if (directionIndex < 0 || directionIndex >= 5)
            {
                continue;
            }

            if (frames == null || frames.Count == 0)
            {
                continue;
            }

            byte[] blockData = mulEditedFrameSaveService.BuildEditedDirectionBlock(frames);
            if (blockData.Length == 0)
            {
                continue;
            }

            int slotIndex = baseIndex + (actionIndex * 5) + directionIndex;

            fileEdit.PendingBlocksBySlotIndex[slotIndex] =
                new PendingMulImportSession.PendingMulBlock
                {
                    SlotIndex = slotIndex,
                    Extra = 0,
                    BlockData = blockData
                };

            fileEdit.WorkingIdxEntries[slotIndex].Offset = -2;
            fileEdit.WorkingIdxEntries[slotIndex].Length = blockData.Length;
            fileEdit.WorkingIdxEntries[slotIndex].Extra = 0;

            queuedDirections++;
        }

        if (queuedDirections == 0)
        {
            StatusText = "无法将精灵表方向队列到所选 MUL 槽位。";
            return false;
        }

        hasFrameEdits = false;
        OnPropertyChanged(nameof(HasFrameEdits));
        RefreshUnsavedChangesState();

        StatusText =
            "精灵表已队列到 " + SelectedMulSlot.FileName +
            " 身体 " + SelectedMulSlot.BodyIndex +
            "，动作 " + actionIndex +
            " (" + queuedDirections + " 个方向)。点击保存更改写入。";

        return true;
    }

    private List<VdFrameData> BuildFramesFromSpriteSheet(
        ImageSharpImage sheetImage,
        SpriteSheetImportDialogResult options)
    {
        List<(ImageSharpImage image, int anchorX, int anchorY)> rawFrames =
            new List<(ImageSharpImage image, int anchorX, int anchorY)>();

        int maxFrames = options.DirectionsInRows ? options.Columns : options.Rows;

        for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
        {
            int cellX = options.StartX + (frameIndex * (options.CellWidth + options.HorizontalSpacing));
            int cellY = options.StartY + (0 * (options.CellHeight + options.VerticalSpacing));

            if (cellX < 0 || cellY < 0 ||
                cellX + options.CellWidth > sheetImage.Width ||
                cellY + options.CellHeight > sheetImage.Height)
            {
                continue;
            }

            using ImageSharpImage cropped = sheetImage.Clone(ctx =>
                ctx.Crop(new SixLabors.ImageSharp.Rectangle(
                    cellX,
                    cellY,
                    options.CellWidth,
                    options.CellHeight)));

            ImageSharpImage working = cropped.Clone();

            if (options.UseMagentaTransparency)
            {
                KeyOutMagenta(working);
            }

            if (options.TrimTransparentBorder)
            {
                working = TrimTransparentImage(working, options.TrimPadding, out int trimLeft, out int trimTop);

                int anchorX = options.AnchorBottomCenter
                    ? (options.CellWidth / 2) - trimLeft
                    : working.Width / 2;

                int anchorY = options.AnchorBottomCenter
                    ? (options.CellHeight - 1) - trimTop
                    : working.Height / 2;

                rawFrames.Add((working, anchorX, anchorY));
            }
            else
            {
                int anchorX = options.AnchorBottomCenter ? (options.CellWidth / 2) : (working.Width / 2);
                int anchorY = options.AnchorBottomCenter ? (options.CellHeight - 1) : (working.Height / 2);

                rawFrames.Add((working, anchorX, anchorY));
            }
        }

        if (rawFrames.Count == 0)
        {
            return new List<VdFrameData>();
        }

        int maxLeft = rawFrames.Max(x => x.anchorX);
        int maxTop = rawFrames.Max(x => x.anchorY);
        int maxRight = rawFrames.Max(x => x.image.Width - x.anchorX);
        int maxBottom = rawFrames.Max(x => x.image.Height - x.anchorY);

        int finalWidth = maxLeft + maxRight;
        int finalHeight = maxTop + maxBottom;

        List<VdFrameData> result = new List<VdFrameData>();

        for (int i = 0; i < rawFrames.Count; i++)
        {
            var raw = rawFrames[i];

            using ImageSharpImage canvas = new ImageSharpImage(
                finalWidth,
                finalHeight,
                new Bgra32(0, 0, 0, 0));

            int drawX = maxLeft - raw.anchorX;
            int drawY = maxTop - raw.anchorY;

            canvas.Mutate(ctx => ctx.DrawImage(
                raw.image,
                new SixLabors.ImageSharp.Point(drawX, drawY),
                1f));

            WriteableBitmap bitmap = ConvertImageSharpToWriteableBitmap(canvas);

            result.Add(new VdFrameData
            {
                Bitmap = bitmap,
                Palette565 = null,
                CenterX = (short)maxLeft,
                CenterY = (short)maxTop,
                Width = (ushort)finalWidth,
                Height = (ushort)finalHeight,
                InitCoordsX = 0,
                InitCoordsY = 0,
                EndCoordsX = 0,
                EndCoordsY = 0,
                FrameId = (ushort)i,
                FrameNumber = (ushort)i,
                DataOffset = 0
            });
        }

        return result;
    }

    private void KeyOutMagenta(ImageSharpImage image)
    {
        if (image == null)
        {
            return;
        }

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Bgra32> row = accessor.GetRowSpan(y);

                for (int x = 0; x < row.Length; x++)
                {
                    Bgra32 pixel = row[x];

                    bool isMagentaKey =
                        pixel.R >= 245 &&
                        pixel.B >= 245 &&
                        pixel.G <= 10;

                    if (isMagentaKey)
                    {
                        row[x] = new Bgra32(0, 0, 0, 0);
                    }
                }
            }
        });
    }

    private ImageSharpImage TrimTransparentImage(
        ImageSharpImage source,
        int padding,
        out int trimLeft,
        out int trimTop)
    {
        trimLeft = 0;
        trimTop = 0;

        int minX = source.Width;
        int minY = source.Height;
        int maxX = -1;
        int maxY = -1;

        source.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Bgra32> row = accessor.GetRowSpan(y);

                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].A <= 16)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        });

        if (maxX < minX || maxY < minY)
        {
            return new ImageSharpImage(1, 1);
        }

        minX = Math.Max(0, minX - padding);
        minY = Math.Max(0, minY - padding);
        maxX = Math.Min(source.Width - 1, maxX + padding);
        maxY = Math.Min(source.Height - 1, maxY + padding);

        trimLeft = minX;
        trimTop = minY;

        return source.Clone(ctx => ctx.Crop(new SixLabors.ImageSharp.Rectangle(
            minX,
            minY,
            (maxX - minX) + 1,
            (maxY - minY) + 1)));
    }

    private async Task<SpriteSheetImportDialogResult?> ShowSpriteSheetImportDialogAsync(
        Window owner,
        int imageWidth,
        int imageHeight,
        int defaultActionIndex)
    {
        Window dialog = new Window
        {
            Title = "Import Sprite Sheet",
            Width = 560,
            Height = 760,
            MinWidth = 560,
            MinHeight = 760,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        NumericUpDown actionIndexBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999,
            Increment = 1,
            Value = defaultActionIndex,
            Width = 120
        };

        NumericUpDown directionCountBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 5,
            Increment = 1,
            Value = 5,
            Width = 120
        };

        CheckBox directionsInRowsCheckBox = new CheckBox
        {
            Content = "Directions are arranged by rows",
            IsChecked = true
        };

        NumericUpDown cellWidthBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = Math.Max(1, imageWidth),
            Increment = 1,
            Value = Math.Max(1, imageWidth / 14),
            Width = 120
        };

        NumericUpDown cellHeightBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = Math.Max(1, imageHeight),
            Increment = 1,
            Value = Math.Max(1, imageHeight / 8),
            Width = 120
        };

        NumericUpDown startXBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = Math.Max(0, imageWidth - 1),
            Increment = 1,
            Value = 0,
            Width = 120
        };

        NumericUpDown startYBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = Math.Max(0, imageHeight - 1),
            Increment = 1,
            Value = 0,
            Width = 120
        };

        NumericUpDown columnsBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 500,
            Increment = 1,
            Value = 14,
            Width = 120
        };

        NumericUpDown rowsBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 500,
            Increment = 1,
            Value = 8,
            Width = 120
        };

        NumericUpDown horizontalSpacingBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 256,
            Increment = 1,
            Value = 0,
            Width = 120
        };

        NumericUpDown verticalSpacingBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 256,
            Increment = 1,
            Value = 0,
            Width = 120
        };

        CheckBox trimTransparentBorderCheckBox = new CheckBox
        {
            Content = "Trim transparent border",
            IsChecked = true
        };

        NumericUpDown trimPaddingBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 32,
            Increment = 1,
            Value = 0,
            Width = 120
        };

        CheckBox useMagentaTransparencyCheckBox = new CheckBox
        {
            Content = "Treat pure magenta as transparent",
            IsChecked = true
        };

        CheckBox anchorBottomCenterCheckBox = new CheckBox
        {
            Content = "Anchor frames by bottom center",
            IsChecked = true
        };

        TextBlock sheetInfoText = new TextBlock
        {
            Text = "Sheet size: " + imageWidth + " x " + imageHeight,
            FontWeight = FontWeight.SemiBold
        };

        TextBlock previewText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        TextBlock warningText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.OrangeRed,
            Margin = new Thickness(0, 4, 0, 0)
        };

        Button importButton = new Button
        {
            Content = "Import",
            Width = 90
        };

        Button cancelButton = new Button
        {
            Content = "Cancel",
            Width = 90
        };

        SpriteSheetImportDialogResult? result = null;

        Grid MakeRow(string label, Control control)
        {
            Grid grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("220,*"),
                ColumnSpacing = 12
            };

            TextBlock textBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(textBlock, 0);
            Grid.SetColumn(control, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(control);

            return grid;
        }

        void UpdatePreview()
        {
            int cellWidth = (int)(cellWidthBox.Value ?? 1);
            int cellHeight = (int)(cellHeightBox.Value ?? 1);
            int startX = (int)(startXBox.Value ?? 0);
            int startY = (int)(startYBox.Value ?? 0);
            int columns = (int)(columnsBox.Value ?? 1);
            int rows = (int)(rowsBox.Value ?? 1);
            int hSpacing = (int)(horizontalSpacingBox.Value ?? 0);
            int vSpacing = (int)(verticalSpacingBox.Value ?? 0);
            int directionCount = (int)(directionCountBox.Value ?? 5);
            bool directionsInRows = directionsInRowsCheckBox.IsChecked == true;

            int usedWidth = columns <= 0
                ? 0
                : (columns * cellWidth) + ((columns - 1) * hSpacing);

            int usedHeight = rows <= 0
                ? 0
                : (rows * cellHeight) + ((rows - 1) * vSpacing);

            int endX = startX + usedWidth;
            int endY = startY + usedHeight;

            bool fits =
                startX >= 0 &&
                startY >= 0 &&
                endX <= imageWidth &&
                endY <= imageHeight;

            int usableDirectionCount = directionsInRows
                ? Math.Min(directionCount, rows)
                : Math.Min(directionCount, columns);

            int framesPerDirection = directionsInRows ? columns : rows;
            int totalCells = rows * columns;

            previewText.Text =
                "Import preview:" + Environment.NewLine +
                "- Action: " + ((int)(actionIndexBox.Value ?? 0)) + Environment.NewLine +
                "- Direction count: " + usableDirectionCount + Environment.NewLine +
                "- Layout: " + (directionsInRows ? "rows = directions, columns = frames" : "columns = directions, rows = frames") + Environment.NewLine +
                "- Frames per direction: " + framesPerDirection + Environment.NewLine +
                "- Total cells in grid: " + totalCells + Environment.NewLine +
                "- Grid area: X " + startX + " to " + endX + ", Y " + startY + " to " + endY + Environment.NewLine +
                "- Trim: " + ((trimTransparentBorderCheckBox.IsChecked == true) ? "On" : "Off") +
                " | Padding: " + ((int)(trimPaddingBox.Value ?? 0)) + Environment.NewLine +
                "- Magenta transparency: " + ((useMagentaTransparencyCheckBox.IsChecked == true) ? "On" : "Off") + Environment.NewLine +
                "- Anchor: " + ((anchorBottomCenterCheckBox.IsChecked == true) ? "Bottom center" : "Image center");

            if (!fits)
            {
                warningText.Text =
                    "The selected grid extends outside the image bounds. Adjust start position, cell size, spacing, rows, or columns.";
                importButton.IsEnabled = false;
                return;
            }

            if (usableDirectionCount <= 0 || framesPerDirection <= 0)
            {
                warningText.Text = "Direction count and frame count must be greater than zero.";
                importButton.IsEnabled = false;
                return;
            }

            warningText.Text = string.Empty;
            importButton.IsEnabled = true;
        }

        void HookValueChanged(NumericUpDown box)
        {
            box.PropertyChanged += (_, args) =>
            {
                if (args.Property.Name == nameof(NumericUpDown.Value))
                {
                    UpdatePreview();
                }
            };
        }

        HookValueChanged(actionIndexBox);
        HookValueChanged(directionCountBox);
        HookValueChanged(cellWidthBox);
        HookValueChanged(cellHeightBox);
        HookValueChanged(startXBox);
        HookValueChanged(startYBox);
        HookValueChanged(columnsBox);
        HookValueChanged(rowsBox);
        HookValueChanged(horizontalSpacingBox);
        HookValueChanged(verticalSpacingBox);
        HookValueChanged(trimPaddingBox);

        directionsInRowsCheckBox.IsCheckedChanged += (_, _) => UpdatePreview();
        trimTransparentBorderCheckBox.IsCheckedChanged += (_, _) => UpdatePreview();
        useMagentaTransparencyCheckBox.IsCheckedChanged += (_, _) => UpdatePreview();
        anchorBottomCenterCheckBox.IsCheckedChanged += (_, _) => UpdatePreview();

        importButton.Click += (_, _) =>
        {
            result = new SpriteSheetImportDialogResult
            {
                Confirmed = true,
                ActionIndex = (int)(actionIndexBox.Value ?? 0),
                DirectionCount = (int)(directionCountBox.Value ?? 5),
                DirectionsInRows = directionsInRowsCheckBox.IsChecked == true,
                CellWidth = (int)(cellWidthBox.Value ?? 1),
                CellHeight = (int)(cellHeightBox.Value ?? 1),
                StartX = (int)(startXBox.Value ?? 0),
                StartY = (int)(startYBox.Value ?? 0),
                Columns = (int)(columnsBox.Value ?? 1),
                Rows = (int)(rowsBox.Value ?? 1),
                HorizontalSpacing = (int)(horizontalSpacingBox.Value ?? 0),
                VerticalSpacing = (int)(verticalSpacingBox.Value ?? 0),
                TrimTransparentBorder = trimTransparentBorderCheckBox.IsChecked == true,
                TrimPadding = (int)(trimPaddingBox.Value ?? 0),
                UseMagentaTransparency = useMagentaTransparencyCheckBox.IsChecked == true,
                AnchorBottomCenter = anchorBottomCenterCheckBox.IsChecked == true
            };

            dialog.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            result = null;
            dialog.Close();
        };

        StackPanel contentPanel = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                sheetInfoText,

                MakeRow("Action index", actionIndexBox),
                MakeRow("Direction count", directionCountBox),

                directionsInRowsCheckBox,

                MakeRow("Cell width", cellWidthBox),
                MakeRow("Cell height", cellHeightBox),

                MakeRow("Grid start X", startXBox),
                MakeRow("Grid start Y", startYBox),

                MakeRow("Columns", columnsBox),
                MakeRow("Rows", rowsBox),

                MakeRow("Horizontal spacing", horizontalSpacingBox),
                MakeRow("Vertical spacing", verticalSpacingBox),

                trimTransparentBorderCheckBox,
                MakeRow("Trim padding", trimPaddingBox),

                useMagentaTransparencyCheckBox,
                anchorBottomCenterCheckBox,

                previewText,
                warningText
            }
        };

        ScrollViewer scrollViewer = new ScrollViewer
        {
            Content = contentPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        StackPanel buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children =
            {
                cancelButton,
                importButton
            }
        };

        Grid rootGrid = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        Grid.SetRow(scrollViewer, 0);
        Grid.SetRow(buttonPanel, 1);

        rootGrid.Children.Add(scrollViewer);
        rootGrid.Children.Add(buttonPanel);

        dialog.Content = rootGrid;

        UpdatePreview();

        await dialog.ShowDialog(owner);
        return result;
    }

    private bool TryGetImportedSpriteSheetDirectionsForAction(
        int actionIndex,
        out Dictionary<int, List<VdFrameData>> directions)
    {
        if (hasImportedSpriteSheetSession &&
            importedSpriteSheetActions.TryGetValue(actionIndex, out Dictionary<int, List<VdFrameData>>? found) &&
            found != null)
        {
            directions = found;
            return true;
        }

        directions = new Dictionary<int, List<VdFrameData>>();
        return false;
    }

    private bool HasImportedSpriteSheetPreviewForCurrentSelection()
    {
        if (!hasImportedSpriteSheetSession)
        {
            return false;
        }

        int actionIndex = GetSelectedActionIndex();
        int directionIndex = GetSelectedDirectionIndex();

        return importedSpriteSheetActions.TryGetValue(actionIndex, out Dictionary<int, List<VdFrameData>>? directions) &&
               directions != null &&
               directions.TryGetValue(directionIndex, out List<VdFrameData>? frames) &&
               frames != null &&
               frames.Count > 0;
    }

    private void RebuildActionListForSelectedMulSlot()
    {
        if (SelectedMulSlot == null)
        {
            return;
        }

        suppressActionReload = true;

        ActionOptions.Clear();
        actionNameToIndex.Clear();

        string[] actionNames = GetActionNamesForMulSlot(SelectedMulSlot);

        for (int actionIndex = 0; actionIndex < SelectedMulSlot.AnimLength; actionIndex++)
        {
            string actionLabel;

            if (actionIndex >= 0 &&
                actionIndex < actionNames.Length &&
                !string.IsNullOrWhiteSpace(actionNames[actionIndex]))
            {
                actionLabel = actionNames[actionIndex] + " (" + actionIndex + ")";
            }
            else
            {
                actionLabel = "Action " + actionIndex;
            }

            ActionOptions.Add(actionLabel);
            actionNameToIndex[actionLabel] = actionIndex;
        }

        if (ActionOptions.Count > 0)
        {
            string? preferred = ActionOptions.FirstOrDefault(x =>
                actionNameToIndex.TryGetValue(x, out int idx) && idx == importedSpriteSheetLastActionIndex);

            SelectedAction = preferred ?? ActionOptions[0];
        }
        else
        {
            SelectedAction = null;
        }

        suppressActionReload = false;
    }

    private string[] GetActionNamesForMulSlot(MulSlotEntry slot)
    {
        if (slot.AnimLength == 13)
        {
            return MulAnimalActions;
        }

        if (slot.AnimLength == 22)
        {
            return MulMonsterActions;
        }

        if (slot.AnimLength == 35)
        {
            return MulHumanActions;
        }

        return Array.Empty<string>();
    }

    private bool QueueEditedMulFramesForResolvedBlock(
    ResolvedAnimationBlock resolvedBlock,
    List<VdFrameData> frames)
    {
        if (resolvedBlock == null || resolvedBlock.IsUop)
        {
            return false;
        }

        if (frames == null || frames.Count == 0)
        {
            return false;
        }

        byte[] blockData = mulEditedFrameSaveService.BuildEditedDirectionBlock(frames);
        if (blockData.Length == 0)
        {
            return false;
        }

        List<AnimationIdxEntry> workingIdxEntries;

        if (pendingMulImportSession.TryGetFileEdit(
                resolvedBlock.IdxPath,
                out PendingMulImportSession.PendingMulFileEdit? existingFileEdit))
        {
            workingIdxEntries = existingFileEdit.WorkingIdxEntries;
        }
        else
        {
            workingIdxEntries = ReadIdxEntriesForPendingEdit(resolvedBlock.IdxPath);
        }

        PendingMulImportSession.PendingMulFileEdit fileEdit =
            pendingMulImportSession.GetOrCreateFileEdit(
                resolvedBlock.MulPath,
                resolvedBlock.IdxPath,
                workingIdxEntries);

        fileEdit.PendingBlocksBySlotIndex[resolvedBlock.SlotIndex] =
            new PendingMulImportSession.PendingMulBlock
            {
                SlotIndex = resolvedBlock.SlotIndex,
                Extra = resolvedBlock.Extra,
                BlockData = blockData
            };

        return true;
    }
}