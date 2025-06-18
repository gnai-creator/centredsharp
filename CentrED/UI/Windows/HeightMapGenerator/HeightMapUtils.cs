using System;
using System.Collections.Generic;
using System.IO;

namespace CentrED.UI.Windows;

internal static class HeightMapUtils
{
    public static readonly Dictionary<TerrainType, string> TypeAliases = new()
    {
        [TerrainType.Water] = "water",
        [TerrainType.Sand] = "sand",
        [TerrainType.Grass] = "grass",
        [TerrainType.Jungle] = "jungle",
        [TerrainType.Forest] = "forest",
        [TerrainType.Dirt] = "dirt",
        [TerrainType.Cobble] = "cobble",
        [TerrainType.Mountain] = "mountain",
        [TerrainType.Swamp] = "swamp",
        [TerrainType.Furrows] = "furrows",
        [TerrainType.Snow] = "snow",
        [TerrainType.None] = "none"
    };

    public static string CleanTransitionKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        key = key.Replace('\\', '/');
        if (key.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase))
            key = key.Substring("Scripts/".Length);
        if (key.StartsWith("map/", StringComparison.OrdinalIgnoreCase))
            key = key.Substring("map/".Length);
        if (key.StartsWith("map2/", StringComparison.OrdinalIgnoreCase))
            key = key.Substring("map2/".Length);
        if (key.StartsWith('/'))
            key = key.TrimStart('/');

        return Path.GetFileNameWithoutExtension(key);
    }
}
