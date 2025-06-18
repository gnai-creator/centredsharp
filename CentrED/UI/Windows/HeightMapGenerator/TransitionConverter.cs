using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Assets;
using CentrED.IO.Models;
using CentrED.Map;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using CentrED.Utility;
namespace CentrED.UI.Windows;

internal class TransitionTile
{
    public List<uint> Tiles { get; set; } = new();

    public uint GetId(int x, int intY)
    {
        if (Tiles == null || Tiles.Count == 0)
            return 0;
        int idx = HeightMapGenerator.DeterministicIndex(x, intY, Tiles.Count);
        return Tiles[idx];
    }
}

internal sealed class TransitionConverter
{
    public static readonly (int dx, int dy)[] NeighborOffsetsClockwise =
    {
        (-1, -1), (0, -1), (1, -1),
        (1, 0), (1, 1), (0, 1),
        (-1, 1), (-1, 0)
    };

    public void ApplyAllTransitionPasses(Tile[,] map, Dictionary<string, Dictionary<string, TransitionTile>> transitionTiles)
    {
        int maxPasses = 4;

        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool changed = false;
            var snapshot = (Tile[,])map.Clone();

            foreach (var key in HeightMapGenerator.transitionsOrderList)
            {
                int localChangeCount = ApplyTransitionsForKey(map, snapshot, transitionTiles, key);
                if (localChangeCount > 0)
                    changed = true;
            }


            if (changed)
                break;
        }
    }



    private int ApplyTransitionsForKey(Tile[,] map, Tile[,] snapshot, Dictionary<string, Dictionary<string, TransitionTile>> transitionTiles, string key)
    {
        if (!transitionTiles.ContainsKey(key))
            return 0;

        var keyParts = key.Split('2');
        if (keyParts.Length != 2)
            return 0;

        string expectedSource = keyParts[0];
        string expectedTarget = keyParts[1];

        if (!Enum.TryParse(expectedSource, true, out TerrainType sourceType))
            return 0;


        int width = map.GetLength(0);
        int height = map.GetLength(1);
        int changeCount = 0;

        if (!transitionTiles.ContainsKey(key))
            return 0;


        Parallel.For(1, height - 1, y =>
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (map[x, y].Type != sourceType)
                    continue;

                try
                {
                    string centerKey = TileNameResolver.GetTileName(snapshot, x, y).ToLowerInvariant().Replace(" ", "").Replace("_", "");

                    if (!expectedSource.Equals(centerKey, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Verificar se pelo menos um vizinho é do tipo target
                    bool hasNeighborOfTarget = false;
                    foreach (var (dx, dy) in NeighborOffsetsClockwise)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                            continue;

                        string neighborKey = TileNameResolver.GetTileName(snapshot, nx, ny).ToLowerInvariant().Replace(" ", "").Replace("_", "");

                        if (neighborKey.Equals(expectedTarget, StringComparison.OrdinalIgnoreCase))
                        {
                            hasNeighborOfTarget = true;
                            break;
                        }
                    }

                    if (!hasNeighborOfTarget)
                        continue;

                    var pattern = TilePatternResolver.GetPattern(snapshot, new Tile(snapshot[x, y].Type, snapshot[x, y].Id, x, y, snapshot[x, y].GroupName), x, y);

                    if (!transitionTiles[key].TryGetValue(pattern, out var tile))
                        continue;

                    if (tile.Tiles.Count == 0)
                        continue;

                    uint id = tile.GetId(x, y);
                    if (id == 0 || id >= TileDataLoader.Instance.LandData.Length)
                        continue;

                    if (!HeightMapGenerator.TileIdToType.TryGetValue((ushort)id, out var newType))
                        continue;

                    map[x, y] = new Tile(newType, (ushort)id, x, y, snapshot[x, y].GroupName);
                    Interlocked.Increment(ref changeCount);
                }
                catch { }
            }
        });

        return changeCount;
    }

    private static readonly Dictionary<TerrainType, Rgba32> TypeColors = new()
    {
        [TerrainType.Water] = new Rgba32(0, 0, 255),
        [TerrainType.Sand] = new Rgba32(244, 164, 96),
        [TerrainType.Grass] = new Rgba32(0, 128, 0),
        [TerrainType.Jungle] = new Rgba32(0, 100, 0),
        [TerrainType.Forest] = new Rgba32(34, 139, 34),
        [TerrainType.Dirt] = new Rgba32(139, 69, 19),
        [TerrainType.Cobble] = new Rgba32(128, 128, 128),
        [TerrainType.Mountain] = new Rgba32(169, 169, 169),
        [TerrainType.Swamp] = new Rgba32(47, 79, 79),
        [TerrainType.Furrows] = new Rgba32(210, 180, 140),
        [TerrainType.Snow] = new Rgba32(255, 255, 255),
        [TerrainType.None] = new Rgba32(0, 0, 0)
    };

    public void SaveDebugImage(Tile[,] map, string filePath)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        using Image<Rgba32> img = new(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var type = map[x, y].Type;
                img[x, y] = TypeColors.TryGetValue(type, out var c) ? c : new Rgba32(0, 0, 0);
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        img.Save(filePath);
    }
}
