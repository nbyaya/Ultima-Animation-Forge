namespace UltimaAnimationForge.Models;

public enum CompressionFlag
{
    None = 0,
    Zlib = 1,
    Mythic = 3
}

public class UopAnimationIndexEntry
{
    public int Lookup { get; set; } = -1;
    public int Length { get; set; } = -1;
    public int DecompressedLength { get; set; } = -1;
    public CompressionFlag Flag { get; set; } = CompressionFlag.None;
    public int Extra { get; set; } = -1;
    public int Extra1 { get; set; } = -1;
    public int Extra2 { get; set; } = -1;

    public bool IsValid => Lookup >= 0 && Length > 0;
}

public sealed class UopBodySlotEntry
{
    public int BodyId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string BodyType { get; set; } = string.Empty;

    public int ActionCount { get; set; }

    public string DisplayText => FileName + " | Body " + BodyId;

    public string SecondaryText => BodyType + " | " + ActionCount + " actions";
}

public struct UopDataHeader(ulong offset, uint headerSize, uint compressedSize, uint decompressedSize, ulong hash, ushort flag)
{
    public ulong Offset { get; set; } = offset;

    public uint HeaderSize { get; set; } = headerSize;

    public uint CompressedSize { get; set; } = compressedSize;

    public uint DecompressedSize { get; set; } = decompressedSize;

    public ulong Hash { get; set; } = hash;

    public ushort Flag { get; set; } = flag;
}

public sealed class UopFileData
{
    public ulong Hash { get; set; }

    public byte[] Data { get; set; } = [];

    public byte[]? PrecompressedData { get; set; }

    public byte[]? HeaderBytes { get; set; }

    public uint HeaderSize { get; set; }

    public uint DecompressedSize { get; set; }

    public bool IsCompressed { get; set; } = true;

    public bool IsEmpty { get; set; }
}

public class UopTableEntry
{
    public ulong Offset { get; set; }
    public uint HeaderLength { get; set; }
    public uint CompressedLength { get; set; }
    public uint DecompressedLength { get; set; }
    public ulong Identifier { get; set; }
    public uint DataBlockHash { get; set; }
    public short Compression { get; set; }

    public bool IsValid => Identifier != 0 && DecompressedLength != 0 && Offset != 0;
}

public static class UopConstants
{
    public static class ActionNames
    {
        public static readonly string[] MonsterActions =
        {
            "行走",
            "静止",
            "死亡1",
            "死亡2",
            "攻击1",
            "攻击2",
            "攻击3",
            "弓箭攻击",
            "弩攻击",
            "投掷攻击",
            "受伤",
            "掠夺",
            "践踏",
            "施法2",
            "施法3",
            "右侧防御",
            "左侧防御",
            "静止",
            "坐立",
            "飞行",
            "起飞",
            "空中受伤"
        };

        public static readonly string[] AnimalActions =
        {
            "行走",
            "奔跑",
            "静止",
            "进食",
            "警觉",
            "攻击1",
            "攻击2",
            "受伤",
            "死亡1",
            "静止",
            "坐立",
            "躺下",
            "死亡2"
        };

        public static readonly string[] HumanActions =
        {
            "行走",
            "持杖行走",
            "奔跑",
            "持杖奔跑",
            "静止",
            "静止",
            "打哈欠伸展",
            "单手武器待机",
            "单手武器待机",
            "单手斜劈",
            "单手刺击",
            "单手敲击",
            "双手敲击",
            "双手斜劈",
            "双手刺击",
            "双手武器行走",
            "施法1",
            "施法2",
            "弓箭攻击",
            "弩攻击",
            "受伤",
            "向后死亡",
            "向前死亡",
            "骑马行走",
            "骑马奔跑",
            "骑马静止",
            "骑马单手敲击",
            "骑马弓箭攻击",
            "骑马弩攻击",
            "骑马双手斜劈",
            "盾牌防御",
            "拳击",
            "鞠躬",
            "敬礼（持武器）",
            "进食"
        };

        public static readonly string[] CharActions =
        {
            "行走",
            "行走（持武器）",
            "奔跑",
            "奔跑（持武器）",
            "静止",
            "静止（持武器）",
            "坐立",
            "待机 - 单手战斗",
            "待机 - 双手战斗",
            "斜劈（单手武器）",
            "刺击（单手武器）",
            "敲击（单手武器）",
            "敲击（双手武器）",
            "斜劈（双手武器）",
            "刺击（双手武器）",
            "战斗行走（双手武器）",
            "施法1",
            "施法2",
            "弓箭攻击",
            "弩攻击",
            "受伤",
            "向后死亡",
            "向前死亡",
            "骑马行走",
            "骑马奔跑",
            "骑马静止",
            "骑马敲击攻击",
            "骑马弓箭攻击",
            "骑马弩攻击",
            "骑马斜劈攻击",
            "盾牌防御",
            "拳击",
            "鞠躬",
            "敬礼（持武器）",
            "饮用",
            "战斗行走（单手武器）",
            "战斗行走（徒手）",
            "静止（盾牌）",
            "坐下",
            "受伤（双手武器）",
            "采矿",
            "待机 - 盾牌战斗",
            "饮用（坐下）",
            "",
            "",
            "",
            "",
            "静止（双手武器）骑马",
            "受伤骑马",
            "施法骑马",
            "受伤（盾牌）骑马",
            "饮用骑马",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "起飞",
            "降落",
            "前进飞行（慢）",
            "前进飞行（快）",
            "飞行静止",
            "飞行战斗待机",
            "飞行坐立",
            "飞行坐立2",
            "飞行受伤",
            "飞行向后死亡",
            "飞行向前死亡",
            "飞行攻击（单手武器）",
            "飞行攻击（双手武器）",
            "飞行攻击（回旋镖）",
            "飞行受伤（盾牌）",
            "飞行施法1",
            "飞行施法2",
            "飞行受伤",
            "飞行饮用"
        };
    }
}
