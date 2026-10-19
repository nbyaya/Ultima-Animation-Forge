using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UltimaAnimationForge.Models;

namespace UltimaAnimationForge.Services;

public sealed class DragonScriptService
{
    private static readonly Dictionary<int, string> BuiltInGroupNames = new()
    {
        [0x00] = "草地",
        [0x01] = "森林",
        [0x02] = "水",
        [0x03] = "地下城洞穴",
        [0x04] = "黑色",
        [0x05] = "沙漠",
        [0x06] = "山脉",
        [0x07] = "雪地",
        [0x08] = "丛林",
        [0x09] = "土壤",
        [0x0A] = "鹅卵石",
        [0x0B] = "荒地",
        [0x0C] = "沼泽",
        [0x0D] = "地下城墙",
        [0x0E] = "地下城地板",
        [0x0F] = "金字塔地板",
        [0x10] = "熔岩",
        [0x11] = "冰川",
        [0x12] = "砍伐森林",
        [0x13] = "石洞",
        [0x14] = "大理石地板",
        [0x15] = "沟渠",
        [0x16] = "雪地草甸",
        [0x17] = "雪地森林",
        [0x18] = "沼泽",
        [0x19] = "草地凸起",
        [0x1A] = "洞穴入口",
        [0x1B] = "星空",
        [0x1C] = "深色木地板",
        [0x1D] = "隐形洞穴入口",
        [0x1E] = "沙漠森林"
    };

    public List<DragonTerrainColor> LoadTerrainColors(
        string maptransPath,
        string? groupsPath = null,
        string? palettePath = null)
    {
        Dictionary<int, int> groupByPalette = string.IsNullOrWhiteSpace(groupsPath)
            ? new Dictionary<int, int>()
            : LoadGroups(groupsPath);

        List<Color> paletteColors = LoadPaletteColors(palettePath);

        List<DragonTerrainColor> result = new();

        if (string.IsNullOrWhiteSpace(maptransPath) || !File.Exists(maptransPath))
        {
            return result;
        }

        foreach (string rawLine in File.ReadAllLines(maptransPath))
        {
            string line = StripComment(rawLine);

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                continue;
            }

            if (!TryParseHex(parts[0], out int paletteIndex))
            {
                continue;
            }

            int groupId;
            int zIndex;

            if (parts.Length >= 4 &&
                TryParseHex(parts[1], out int parsedGroup) &&
                TryParseInt(parts[2], out int parsedZ))
            {
                groupId = parsedGroup;
                zIndex = 2;
            }
            else if (TryParseInt(parts[1], out parsedZ))
            {
                groupId = groupByPalette.TryGetValue(paletteIndex, out int foundGroup)
                    ? foundGroup
                    : GuessGroupFromPalette(paletteIndex);

                zIndex = 1;
            }
            else
            {
                continue;
            }

            int z = int.Parse(parts[zIndex], CultureInfo.InvariantCulture);

            List<int> tileIds = new();

            for (int i = zIndex + 1; i < parts.Length; i++)
            {
                if (TryParseHex(parts[i], out int tileId))
                {
                    tileIds.Add(tileId);
                }
            }

            result.Add(new DragonTerrainColor
            {
                PaletteIndex = paletteIndex,
                GroupId = groupId,
                GroupName = GetGroupName(groupId),
                Z = z,
                TileIds = tileIds,
                Color = paletteIndex >= 0 && paletteIndex < paletteColors.Count
                    ? paletteColors[paletteIndex]
                    : BuildPreviewColor(groupId, z, paletteIndex)
            });
        }

        result.Sort((a, b) => a.PaletteIndex.CompareTo(b.PaletteIndex));
        return result;
    }

    private static List<Color> LoadPaletteColors(string? palettePath)
    {
        List<Color> colors = new();

        if (string.IsNullOrWhiteSpace(palettePath) || !File.Exists(palettePath))
        {
            return colors;
        }

        string extension = Path.GetExtension(palettePath);

        if (string.Equals(extension, ".act", StringComparison.OrdinalIgnoreCase))
        {
            byte[] data = File.ReadAllBytes(palettePath);
            int count = Math.Min(256, data.Length / 3);

            for (int i = 0; i < count; i++)
            {
                int offset = i * 3;
                colors.Add(Color.FromRgb(data[offset], data[offset + 1], data[offset + 2]));
            }
        }
        else if (string.Equals(extension, ".pal", StringComparison.OrdinalIgnoreCase))
        {
            byte[] data = File.ReadAllBytes(palettePath);
            int start = 0;

            if (data.Length >= 24 &&
                data[0] == (byte)'R' &&
                data[1] == (byte)'I' &&
                data[2] == (byte)'F' &&
                data[3] == (byte)'F')
            {
                int dataIndex = FindBytes(data, new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });

                if (dataIndex >= 0)
                {
                    start = dataIndex + 8;
                }
            }

            for (int i = start; i + 3 < data.Length && colors.Count < 256; i += 4)
            {
                colors.Add(Color.FromRgb(data[i], data[i + 1], data[i + 2]));
            }
        }

        return colors;
    }

    private static int FindBytes(byte[] data, byte[] pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool match = true;

            for (int p = 0; p < pattern.Length; p++)
            {
                if (data[i + p] != pattern[p])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<int, int> LoadGroups(string groupsPath)
    {
        Dictionary<int, int> result = new();

        if (!File.Exists(groupsPath))
        {
            return result;
        }

        foreach (string rawLine in File.ReadAllLines(groupsPath))
        {
            string line = StripComment(rawLine);

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                continue;
            }

            if (TryParseHex(parts[0], out int paletteIndex) &&
                TryParseHex(parts[1], out int groupId))
            {
                result[paletteIndex] = groupId;
            }
        }

        return result;
    }

    private static string StripComment(string line)
    {
        int index = line.IndexOf("//", StringComparison.Ordinal);
        if (index >= 0)
        {
            line = line[..index];
        }

        return line.Trim();
    }

    private static bool TryParseHex(string value, out int result)
    {
        value = value.Trim();

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        return int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static int GuessGroupFromPalette(int paletteIndex)
    {
        if (paletteIndex <= 0x0E) return 0x00;
        if (paletteIndex >= 0x10 && paletteIndex <= 0x1E) return 0x01;
        if (paletteIndex >= 0x20 && paletteIndex <= 0x23) return 0x02;
        if (paletteIndex >= 0x24 && paletteIndex <= 0x2E) return 0x03;
        if (paletteIndex >= 0x30 && paletteIndex <= 0x3F) return 0x05;
        if (paletteIndex >= 0x40 && paletteIndex <= 0x4F) return 0x06;
        if (paletteIndex >= 0x50 && paletteIndex <= 0x5E) return 0x18;
        if (paletteIndex >= 0x60 && paletteIndex <= 0x6E) return 0x07;
        if (paletteIndex >= 0x70 && paletteIndex <= 0x7E) return 0x08;
        if (paletteIndex >= 0x80 && paletteIndex <= 0x8E) return 0x09;
        if (paletteIndex >= 0x90 && paletteIndex <= 0x9E) return 0x0A;
        if (paletteIndex >= 0xA0 && paletteIndex <= 0xA6) return 0x04;
        if (paletteIndex >= 0xB0 && paletteIndex <= 0xB6) return 0x0B;
        if (paletteIndex >= 0xB7 && paletteIndex <= 0xBF) return 0x19;

        return 0;
    }

    private static string GetGroupName(int groupId)
    {
        return BuiltInGroupNames.TryGetValue(groupId, out string? name)
            ? name
            : "Group " + groupId.ToString("X2");
    }

    private static Color BuildPreviewColor(int groupId, int z, int paletteIndex)
    {
        byte shade = (byte)Math.Clamp(80 + z * 3, 20, 240);

        return groupId switch
        {
            0x00 => Color.FromRgb((byte)(20 + shade / 5), shade, (byte)(20 + shade / 6)),
            0x01 => Color.FromRgb(0, shade, 45),
            0x02 => Color.FromRgb(0, (byte)(40 + shade / 4), shade),
            0x03 => Color.FromRgb(65, 65, 65),
            0x04 => Color.FromRgb(0, 0, 0),
            0x05 => Color.FromRgb(shade, (byte)(shade - 20), 80),
            0x06 => Color.FromRgb(shade, shade, shade),
            0x07 => Color.FromRgb(220, 220, 235),
            0x08 => Color.FromRgb(0, shade, 85),
            0x09 => Color.FromRgb(120, 82, 45),
            0x0A => Color.FromRgb(120, 120, 120),
            0x0B => Color.FromRgb(75, 65, 50),
            0x10 => Color.FromRgb(210, 55, 20),
            0x18 => Color.FromRgb(45, 75, 45),
            0x19 => Color.FromRgb(80, shade, 65),
            _ => Color.FromRgb(
                (byte)((paletteIndex * 53) % 255),
                (byte)((paletteIndex * 97) % 255),
                (byte)((paletteIndex * 151) % 255))
        };
    }
}