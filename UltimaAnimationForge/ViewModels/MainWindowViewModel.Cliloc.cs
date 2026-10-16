using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UltimaAnimationForge.Models;
using UltimaAnimationForge.Services;

namespace UltimaAnimationForge.ViewModels;

public sealed class DictionaryFileOption
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;

    public override string ToString()
    {
        return FileName;
    }
}

public partial class MainWindowViewModel
{
    private readonly ClilocDataService clilocDataService = new();
    private string loadedClilocPath = string.Empty;

    public ObservableCollection<string> UoxDictionaryCategoryOptions { get; } = new();

    [ObservableProperty]
    private string selectedUoxDictionaryCategory = "All";

    [ObservableProperty]
    private bool showOnlyDuplicateClilocs;

    [ObservableProperty]
    private bool showOnlyEmptyClilocs;

    [ObservableProperty]
    private bool useUoxDictionaryMode;

    [ObservableProperty]
    private string uoxDictionaryFolderPath = string.Empty;

    public ObservableCollection<ClilocEntry> ClilocEntries { get; } = new();

    public ObservableCollection<int> FreeClilocNumbers { get; } = new();

    [ObservableProperty]
    private int? selectedFreeClilocNumber;

    [ObservableProperty]
    private ClilocEntry? selectedClilocEntry;

    [ObservableProperty]
    private string clilocSearchText = string.Empty;

    [ObservableProperty]
    private string clilocStatusText = "未加载 Cliloc。";

    public string ClilocEditorTitle =>
    UseUoxDictionaryMode ? "UOX3 字典编辑器" : "Cliloc 编辑器";

    public string EntryNumberLabel =>
        UseUoxDictionaryMode ? "字典编号" : "Cliloc 编号";

    public string FreeNumberTitle =>
    UseUoxDictionaryMode ? "空闲字典号" : "空闲 Cliloc 号";

    public string FindFreeNumbersButtonText =>
        UseUoxDictionaryMode ? "查找空闲字典号" : "查找空闲 Cliloc 号";

    public string UseFreeNumberButtonText =>
        UseUoxDictionaryMode ? "使用所选字典号" : "使用所选 Cliloc 号";

    [ObservableProperty]
    private string jumpToClilocText = string.Empty;

    public ObservableCollection<string> ClilocFileOptions { get; } = new();

    [ObservableProperty]
    private string? selectedClilocFile;

    public ObservableCollection<ClilocEntry> FilteredClilocEntries
    {
        get
        {
            IEnumerable<ClilocEntry> query = ClilocEntries;

            if (!string.IsNullOrWhiteSpace(loadedDictionaryPath) &&
                !string.IsNullOrWhiteSpace(SelectedUoxDictionaryCategory) &&
                !string.Equals(SelectedUoxDictionaryCategory, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    string.Equals(x.Category, SelectedUoxDictionaryCategory, StringComparison.OrdinalIgnoreCase));
            }

            if (ShowOnlyDuplicateClilocs)
            {
                query = query.Where(x => x.IsDuplicate);
            }

            if (ShowOnlyEmptyClilocs)
            {
                query = query.Where(x => string.IsNullOrWhiteSpace(x.Text));
            }

            if (!string.IsNullOrWhiteSpace(ClilocSearchText))
            {
                string search = ClilocSearchText.Trim();

                query = query.Where(x =>
                    x.Number.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.NumberHex.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Text.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return new ObservableCollection<ClilocEntry>(
                query.OrderBy(x => x.Category).ThenBy(x => x.Number));
        }
    }

    partial void OnUseUoxDictionaryModeChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowClilocFileControls));
        OnPropertyChanged(nameof(ShowUoxDictionaryControls));
        OnPropertyChanged(nameof(ClilocEditorTitle));
        OnPropertyChanged(nameof(EntryNumberLabel));
        OnPropertyChanged(nameof(FreeNumberTitle));
        OnPropertyChanged(nameof(FindFreeNumbersButtonText));
        OnPropertyChanged(nameof(UseFreeNumberButtonText));
    }

    public bool ShowClilocFileControls => !UseUoxDictionaryMode;

    public bool ShowUoxDictionaryControls => UseUoxDictionaryMode;

    partial void OnSelectedUoxDictionaryCategoryChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    partial void OnShowOnlyDuplicateClilocsChanged(bool value)
    {
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    partial void OnShowOnlyEmptyClilocsChanged(bool value)
    {
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    private void RefreshUoxDictionaryCategories()
    {
        UoxDictionaryCategoryOptions.Clear();
        UoxDictionaryCategoryOptions.Add("All");

        foreach (string category in ClilocEntries
                     .Select(x => string.IsNullOrWhiteSpace(x.Category) ? "DICTIONARY" : x.Category)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x))
        {
            UoxDictionaryCategoryOptions.Add(category);
        }

        if (!UoxDictionaryCategoryOptions.Contains(SelectedUoxDictionaryCategory))
        {
            SelectedUoxDictionaryCategory = "All";
        }
    }

    private void RefreshClilocDuplicateFlags()
    {
        foreach (ClilocEntry entry in ClilocEntries)
        {
            entry.IsDuplicate = false;
        }

        foreach (IGrouping<int, ClilocEntry> group in ClilocEntries.GroupBy(x => x.Number))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            foreach (ClilocEntry entry in group)
            {
                entry.IsDuplicate = true;
            }
        }

        int duplicateCount = ClilocEntries.Count(x => x.IsDuplicate);
        int emptyCount = ClilocEntries.Count(x => string.IsNullOrWhiteSpace(x.Text));

        StatusText = "Duplicate IDs: " + duplicateCount + " | Empty text: " + emptyCount + ".";

        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    private readonly UoxDictionaryDataService uoxDictionaryDataService = new();
    private string loadedDictionaryPath = string.Empty;

    public ObservableCollection<DictionaryFileOption> DictionaryFileOptions { get; } = new();

    [ObservableProperty]
    private DictionaryFileOption? selectedDictionaryFile;

    private void FindFreeClilocNumbers()
    {
        FreeClilocNumbers.Clear();

        if (ClilocEntries.Count == 0)
        {
            StatusText = "请先加载 cliloc.enu。";
            return;
        }

        HashSet<int> used = ClilocEntries
            .Select(x => x.Number)
            .ToHashSet();

        int min = ClilocEntries.Min(x => x.Number);
        int max = ClilocEntries.Max(x => x.Number);

        for (int number = min; number <= max; number++)
        {
            if (!used.Contains(number))
            {
                FreeClilocNumbers.Add(number);
            }
        }

        SelectedFreeClilocNumber = FreeClilocNumbers.FirstOrDefault();

        StatusText = "找到 " + FreeClilocNumbers.Count + " 个空闲 Cliloc 编号。";
    }

    private void AddClilocEntryFromFreeNumber()
    {
        if (SelectedFreeClilocNumber == null || SelectedFreeClilocNumber <= 0)
        {
            StatusText = "请先选择一个空闲 Cliloc 编号。";
            return;
        }

        int number = SelectedFreeClilocNumber.Value;

        if (ClilocEntries.Any(x => x.Number == number))
        {
            StatusText = "该 Cliloc 编号已被使用。";
            return;
        }

        ClilocEntry entry = new()
        {
            Number = number,
            Flag = 0,
            Text = "New cliloc entry",
            Category = string.IsNullOrWhiteSpace(SelectedUoxDictionaryCategory) ||
                       SelectedUoxDictionaryCategory == "All"
                ? "DICTIONARY"
                : SelectedUoxDictionaryCategory,
            IsDirty = true
        };

        ClilocEntries.Add(entry);
        SelectedClilocEntry = entry;

        FreeClilocNumbers.Remove(number);

        StatusText = "已添加新 Cliloc 条目 " + number + "。";

        RefreshUoxDictionaryCategories();
        RefreshClilocDuplicateFlags();
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    partial void OnClilocSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    private void LoadCliloc()
    {
        string folder = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            StatusText = "请先打开 UO 文件夹。";
            return;
        }

        RefreshClilocFileOptions();

        string fileName = string.IsNullOrWhiteSpace(SelectedClilocFile) ? "cliloc.enu" : SelectedClilocFile;

        string path = Path.Combine(folder, fileName);

        if (!File.Exists(path))
        {
            StatusText = fileName + " 在选定的 UO 文件夹中找不到。";
            return;
        }

        ClilocEntries.Clear();

        foreach (ClilocEntry entry in clilocDataService.Load(path))
        {
            ClilocEntries.Add(entry);
        }

        loadedClilocPath = path;
        loadedDictionaryPath = string.Empty;

        SelectedClilocEntry = ClilocEntries.FirstOrDefault();

        StatusText = "已加载 " + ClilocEntries.Count + " 个 Cliloc 条目从 " + Path.GetFileName(path) + "。";
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    private void SaveCliloc()
    {
        if (!string.IsNullOrWhiteSpace(loadedDictionaryPath))
        {
            SaveDictionary();
            return;
        }

        if (string.IsNullOrWhiteSpace(loadedClilocPath))
        {
            StatusText = "保存前请先加载 cliloc.enu。";
            return;
        }

        try
        {
            clilocDataService.Save(loadedClilocPath, ClilocEntries);

            StatusText = "已将 " + ClilocEntries.Count + " 个 Cliloc 条目保存到 " + Path.GetFileName(loadedClilocPath) + "。";
            OnPropertyChanged(nameof(FilteredClilocEntries));
        }
        catch (Exception exception)
        {
            StatusText = "保存失败：" + exception.Message;
        }
    }

    private void AddClilocEntry()
    {
        int nextNumber = 1000000;

        if (ClilocEntries.Count > 0)
        {
            nextNumber = ClilocEntries.Max(x => x.Number) + 1;
        }

        ClilocEntry entry = new()
        {
            Number = nextNumber,
            Flag = 0,
            Text = "New cliloc entry",
            IsDirty = true
        };

        ClilocEntries.Add(entry);
        SelectedClilocEntry = entry;

        StatusText = "已添加新 Cliloc 条目 " + entry.Number + "。";
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    private void DeleteClilocEntry()
    {
        if (SelectedClilocEntry == null)
        {
            StatusText = "请选择要删除的 Cliloc 条目。";
            return;
        }

        int number = SelectedClilocEntry.Number;

        ClilocEntries.Remove(SelectedClilocEntry);
        SelectedClilocEntry = ClilocEntries.FirstOrDefault();

        StatusText = "已删除 Cliloc 条目 " + number + "。保存以写入更改。";
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    private void RefreshClilocFileOptions()
    {
        string? previousSelection = SelectedClilocFile;

        ClilocFileOptions.Clear();

        string folder = GetCurrentFolderPath();

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        foreach (string filePath in Directory.GetFiles(folder, "cliloc.*", SearchOption.TopDirectoryOnly)
                     .OrderBy(Path.GetFileName))
        {
            ClilocFileOptions.Add(Path.GetFileName(filePath));
        }

        if (!string.IsNullOrWhiteSpace(previousSelection) &&
            ClilocFileOptions.Contains(previousSelection))
        {
            SelectedClilocFile = previousSelection;
            return;
        }

        if (ClilocFileOptions.Count > 0)
        {
            SelectedClilocFile = ClilocFileOptions.FirstOrDefault(x =>
                string.Equals(x, "cliloc.enu", StringComparison.OrdinalIgnoreCase))
                ?? ClilocFileOptions.First();
        }
    }

    private void RefreshDictionaryFileOptions()
    {
        DictionaryFileOption? previousSelection = SelectedDictionaryFile;

        DictionaryFileOptions.Clear();

        string folder = string.IsNullOrWhiteSpace(UoxDictionaryFolderPath)
    ? GetCurrentFolderPath()
    : UoxDictionaryFolderPath;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        foreach (string filePath in Directory.GetFiles(folder, "dictionary.*", SearchOption.AllDirectories)
                     .OrderBy(Path.GetFileName))
        {
            DictionaryFileOptions.Add(new DictionaryFileOption
            {
                FileName = Path.GetFileName(filePath),
                FullPath = filePath
            });
        }

        if (previousSelection != null)
        {
            DictionaryFileOption? match = DictionaryFileOptions.FirstOrDefault(x =>
                string.Equals(x.FullPath, previousSelection.FullPath, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                SelectedDictionaryFile = match;
                return;
            }
        }

        SelectedDictionaryFile = DictionaryFileOptions.FirstOrDefault();
    }

    private void LoadDictionary()
    {
        RefreshDictionaryFileOptions();

        if (selectedDictionaryFile == null || !File.Exists(selectedDictionaryFile.FullPath))
        {
            StatusText = "未选择 UOX3 字典文件。";
            return;
        }

        ClilocEntries.Clear();

        foreach (ClilocEntry entry in uoxDictionaryDataService.Load(SelectedDictionaryFile.FullPath))
        {
            ClilocEntries.Add(entry);
        }

        loadedDictionaryPath = SelectedDictionaryFile.FullPath;
        loadedClilocPath = string.Empty;
        SelectedClilocEntry = ClilocEntries.FirstOrDefault();

        RefreshUoxDictionaryCategories();
        RefreshClilocDuplicateFlags();

        StatusText = "已从 " + SelectedDictionaryFile.FileName + " 加载了 " + ClilocEntries.Count + " 个字典条目。";
        OnPropertyChanged(nameof(FilteredClilocEntries));
    }

    private async Task SelectUoxDictionaryFolderAsync()
    {
        Window? mainWindow = GetMainWindow();

        if (mainWindow == null)
        {
            StatusText = "无法定位主窗口。";
            return;
        }

        IReadOnlyList<IStorageFolder> folders =
            await mainWindow.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "选择 UOX3 字典文件夹",
                    AllowMultiple = false
                });

        if (folders.Count == 0)
        {
            StatusText = "UOX3 字典文件夹选择已取消。";
            return;
        }

        string? path = folders[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            StatusText = "选定的 UOX3 字典文件夹无效。";
            return;
        }

        UoxDictionaryFolderPath = path;
        RefreshDictionaryFileOptions();

        StatusText = "选定的 UOX3 字典文件夹：" + path;
    }

    private void SaveDictionary()
    {
        if (string.IsNullOrWhiteSpace(loadedDictionaryPath))
        {
            StatusText = "保存前请先加载字典文件。";
            return;
        }

        try
        {
            uoxDictionaryDataService.Save(loadedDictionaryPath, ClilocEntries);

            StatusText = "已将 " + ClilocEntries.Count + " 个字典条目保存到 " + Path.GetFileName(loadedDictionaryPath) + "。";
            OnPropertyChanged(nameof(FilteredClilocEntries));
        }
        catch (Exception exception)
        {
            StatusText = "字典保存失败：" + exception.Message;
        }
    }

    private async Task ExportClilocAsync()
    {
        if (ClilocEntries.Count == 0)
        {
            StatusText = "导出前请先加载 Cliloc 或字典条目。";
            return;
        }

        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "无法定位主窗口。";
            return;
        }

        bool isDictionary = !string.IsNullOrWhiteSpace(loadedDictionaryPath);

        string defaultName = isDictionary
            ? Path.GetFileNameWithoutExtension(loadedDictionaryPath) + ".txt"
            : Path.GetFileNameWithoutExtension(loadedClilocPath) + ".csv";

        IStorageFolder? suggestedFolder = null;

        if (activeProfile == null)
        {
            activeProfile = GetActiveProfile();
        }

        string outputFolder = activeProfile?.OutputFolderPath ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(outputFolder) && Directory.Exists(outputFolder))
        {
            suggestedFolder = await mainWindow.StorageProvider.TryGetFolderFromPathAsync(outputFolder);
        }

        IStorageFile? file = await mainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = isDictionary ? "导出字典文本" : "导出 Cliloc CSV",
                SuggestedFileName = defaultName,
                SuggestedStartLocation = suggestedFolder
            });

        if (file == null)
        {
            StatusText = "导出已取消。";
            return;
        }

        string? path = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText = "导出路径无效。";
            return;
        }

        try
        {
            if (isDictionary)
            {
                ExportDictionaryText(path);
                StatusText = "已将字典文本导出到 " + Path.GetFileName(path) + "。";
            }
            else
            {
                ExportClilocCsv(path);
                StatusText = "已将 Cliloc CSV 导出到 " + Path.GetFileName(path) + "。";
            }
        }
        catch (Exception exception)
        {
            StatusText = "导出失败：" + exception.Message;
        }
    }

    private async Task ImportClilocAsync()
    {
        Window? mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            StatusText = "无法定位主窗口。";
            return;
        }

        bool isDictionary = !string.IsNullOrWhiteSpace(loadedDictionaryPath);

        IReadOnlyList<IStorageFile> files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = isDictionary ? "导入字典文本" : "导入 Cliloc CSV",
                AllowMultiple = false
            });

        if (files.Count == 0)
        {
            StatusText = "导入已取消。";
            return;
        }

        string? path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            StatusText = "导入文件无效。";
            return;
        }

        try
        {
            ClilocEntries.Clear();

            List<ClilocEntry> imported = isDictionary
                ? ImportDictionaryText(path)
                : ImportClilocCsv(path);

            foreach (ClilocEntry entry in imported)
            {
                ClilocEntries.Add(entry);
            }

            SelectedClilocEntry = ClilocEntries.FirstOrDefault();

            StatusText = "已从 " + Path.GetFileName(path) + " 导入了 " + ClilocEntries.Count + " 个条目。保存以写入更改。";
            OnPropertyChanged(nameof(FilteredClilocEntries));
        }
        catch (Exception exception)
        {
            StatusText = "导入失败：" + exception.Message;
        }
    }

    private void ExportClilocCsv(string path)
    {
        List<string> lines = new()
    {
        "编号,标志,文本"
    };

        foreach (ClilocEntry entry in ClilocEntries.OrderBy(x => x.Number))
        {
            lines.Add(
                CsvEscape(entry.Number.ToString()) + "," +
                CsvEscape(entry.Flag.ToString()) + "," +
                CsvEscape(entry.Text ?? string.Empty));
        }

        File.WriteAllLines(path, lines, Encoding.UTF8);
    }

    private List<ClilocEntry> ImportClilocCsv(string path)
    {
        List<ClilocEntry> result = new();
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        int startIndex = lines.Length > 0 && lines[0].StartsWith("Number,", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        for (int i = startIndex; i < lines.Length; i++)
        {
            List<string> columns = ParseCsvLine(lines[i]);

            if (columns.Count < 3)
            {
                continue;
            }

            if (!int.TryParse(columns[0], out int number))
            {
                continue;
            }

            if (!byte.TryParse(columns[1], out byte flag))
            {
                flag = 0;
            }

            result.Add(new ClilocEntry
            {
                Number = number,
                Flag = flag,
                Text = columns[2],
                IsDirty = true
            });
        }

        return result.OrderBy(x => x.Number).ToList();
    }

    private void ExportDictionaryText(string path)
    {
        List<string> lines = new()
    {
        "[DICTIONARY]",
        "{"
    };

        foreach (ClilocEntry entry in ClilocEntries.OrderBy(x => x.Number))
        {
            lines.Add(entry.Number + "=" + (entry.Text ?? string.Empty));
        }

        lines.Add("}");

        File.WriteAllLines(path, lines, Encoding.UTF8);
    }

    private List<ClilocEntry> ImportDictionaryText(string path)
    {
        return uoxDictionaryDataService.Load(path)
            .Select(x =>
            {
                x.IsDirty = true;
                return x;
            })
            .ToList();
    }

    private static string CsvEscape(string value)
    {
        value ??= string.Empty;

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> result = new();
        StringBuilder current = new();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private void JumpToCliloc()
    {
        string text = (JumpToClilocText ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText = "输入要跳转的编号。";
            return;
        }

        int number;

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out number))
            {
                StatusText = "无效的十六进制编号。";
                return;
            }
        }
        else if (!int.TryParse(text, out number))
        {
            StatusText = "无效的编号。";
            return;
        }

        ClilocEntry? entry = ClilocEntries.FirstOrDefault(x => x.Number == number);

        if (entry == null)
        {
            StatusText = "未找到编号 " + number + "。";
            return;
        }

        SelectedClilocEntry = entry;
        StatusText = "已选择编号 " + number + "。";
    }
}