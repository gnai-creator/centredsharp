using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.UI.Windows;

public enum TerrainType
{
    Water,
    Sand,
    Grass,
    Jungle,
    Forest,
    Dirt,
    Cobble,
    Mountain,
    Swamp,
    Furrows,
    Snow,
    None
}

public struct Tile
{
    public TerrainType Type;
    public ushort Id;
    public int X;
    public int Y;
    public string GroupName;

    public Tile(TerrainType type, ushort id, int x = 0, int y = 0, string groupName = "")
    {
        Type = type;
        Id = id;
        X = x;
        Y = y;
        GroupName = groupName;
    }
}

internal record struct TileInfo(int RealIndex, Texture2D Texture, Rectangle Bounds, string Name, string Flags, uint Height)
{
    public static TileInfo INVALID = new(-1, default!, default, string.Empty, string.Empty, 0);
}

internal class Group
{
    public Group(string name)
    {
        Name = name;
    }
    public string Name { get; set; } = string.Empty;
    public int ChannelIndex = -1;
    public float Chance = 100f;
    public sbyte MinHeight = -128;
    public sbyte MaxHeight = 127;
    public List<ushort> Ids = new();
}

public class DragonModFileData
{
    public string Folder { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public List<DragonModTransition> Transitions { get; set; } = new();
    public List<DragonModTransition> Statics { get; set; } = new();
}

public class DragonModTransition
{
    public string pattern { get; set; } = string.Empty;
    public List<uint> tiles { get; set; } = new();
}

public class EnviromentRow
{
    public float Chance { get; set; } = 100f;
    public List<ushort> Tiles { get; set; } = new();
}

public class EnviromentData
{
    public List<EnviromentRow> Rows { get; set; } = new();
}
