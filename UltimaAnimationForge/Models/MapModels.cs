using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace UltimaAnimationForge.Models;

public sealed class UoMapGotoLocation : IEquatable<UoMapGotoLocation>
{
    public string Name { get; set; } = string.Empty;
    public int MapId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public string DisplayText => $"{Name} ({X}, {Y})";

    public bool Equals(UoMapGotoLocation? other) =>
        other != null && Name == other.Name && MapId == other.MapId && X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => Equals(obj as UoMapGotoLocation);

    public override int GetHashCode() => HashCode.Combine(Name, MapId, X, Y);
}

public sealed class UoMapOption
{
    public string Name { get; set; } = string.Empty;
    public int MapId { get; set; }
    public int FileIndex { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public string DisplayText => $"{Name} ({Width}x{Height})";
}

public sealed class UoMapRenderResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public WriteableBitmap? Bitmap { get; set; }
}

public sealed class UoMapTileDetails
{
    public int X { get; set; }
    public int Y { get; set; }

    public ushort LandTileId { get; set; }
    public sbyte LandZ { get; set; }

    public string LandTileHex => "0x" + LandTileId.ToString("X4");

    public List<UoMapStaticDetails> Statics { get; set; } = new();

    public string DisplayText =>
        $"X: {X}\nY: {Y}\nLand: {LandTileId} [{LandTileHex}]\nZ: {LandZ}\nStatics: {Statics.Count}";
}

public sealed class UoMapStaticDetails
{
    public ushort Graphic { get; set; }
    public byte LocalX { get; set; }
    public byte LocalY { get; set; }
    public sbyte Z { get; set; }
    public short Hue { get; set; }

    public string GraphicHex => "0x" + Graphic.ToString("X4");
    public string HueHex => "0x" + Hue.ToString("X4");

    public string DisplayText =>
        $"{Graphic} [{GraphicHex}] Z:{Z} Hue:{HueHex}";
}

public sealed partial class UoMapMarker : ObservableObject
{
    [ObservableProperty]
    private string label = "Marker";

    [ObservableProperty]
    private int x;

    [ObservableProperty]
    private int y;

    [ObservableProperty]
    private int mapId;

    public string DisplayText => $"{Label} ({X}, {Y})";
}

public enum UoMapAltitudeMode
{
    Normal,
    NormalWithAltitude,
    AltitudeMap
}

public enum UoMapAltitudePreset
{
    Sharp,
    Normal,
    Soft
}

/// <summary>
/// 高度模式包装类 - 在 ComboBox 显示中文文本，保存枚举值用于业务逻辑（类型 2+17 汉化模式）
/// </summary>
public sealed class UoMapAltitudeModeItem : IEquatable<UoMapAltitudeModeItem>
{
    public UoMapAltitudeMode Mode { get; }

    public UoMapAltitudeModeItem(UoMapAltitudeMode mode)
    {
        Mode = mode;
    }

    // 兼容旧代码，接受可选的显示文本参数（但不使用）
    public UoMapAltitudeModeItem(UoMapAltitudeMode mode, string? displayText) : this(mode)
    {
    }

    public override string ToString() => Mode switch
    {
        UoMapAltitudeMode.Normal => "正常",
        UoMapAltitudeMode.NormalWithAltitude => "高度显示",
        UoMapAltitudeMode.AltitudeMap => "高度地图",
        _ => Mode.ToString()
    };

    public bool Equals(UoMapAltitudeModeItem? other) => other != null && Mode == other.Mode;
    public override bool Equals(object? obj) => Equals(obj as UoMapAltitudeModeItem);
    public override int GetHashCode() => Mode.GetHashCode();
}

/// <summary>
/// 高度预设包装类 - 在 ComboBox 显示中文文本，保存枚举值用于业务逻辑（类型 2+17 汉化模式）
/// </summary>
public sealed class UoMapAltitudePresetItem : IEquatable<UoMapAltitudePresetItem>
{
    public UoMapAltitudePreset Preset { get; }

    public UoMapAltitudePresetItem(UoMapAltitudePreset preset)
    {
        Preset = preset;
    }

    // 兼容旧代码，接受可选的显示文本参数（但不使用）
    public UoMapAltitudePresetItem(UoMapAltitudePreset preset, string? displayText) : this(preset)
    {
    }

    public override string ToString() => Preset switch
    {
        UoMapAltitudePreset.Sharp => "锐利",
        UoMapAltitudePreset.Normal => "正常",
        UoMapAltitudePreset.Soft => "柔和",
        _ => Preset.ToString()
    };

    public bool Equals(UoMapAltitudePresetItem? other) => other != null && Preset == other.Preset;
    public override bool Equals(object? obj) => Equals(obj as UoMapAltitudePresetItem);
    public override int GetHashCode() => Preset.GetHashCode();
}