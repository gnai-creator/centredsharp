using System;
using System.Collections.Generic;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private enum TerrainType
    {
        Water,
        Sand,
        Grass,
        Jungle,
        Rock,
        Snow
    }

    private struct Tile
    {
        public TerrainType Type;
        public ushort Id;

        public Tile(TerrainType type, ushort id)
        {
            Type = type;
            Id = id;
        }
    }

    private class TransitionTile
    {
        public int Id;
        public int MinZ;
        public int MaxZ;
    }

    private class TransitionConverter
    {
        private static readonly (int dx, int dy)[] NeighborOffsets = new (int, int)[]
        {
            (-1, -1), // NW
            (0, -1),  // N
            (1, -1),  // NE
            (1, 0),   // E
            (1, 1),   // SE
            (0, 1),   // S
            (-1, 1),  // SW
            (-1, 0)   // W
        };

        public void ApplyTransitions(Tile[,] map, Dictionary<string, Dictionary<string, TransitionTile>> transitionTiles)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            Tile[,] copy = (Tile[,])map.Clone();

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    var center = copy[x, y];
                    var counts = new Dictionary<TerrainType, int>();

                    foreach (var (dx, dy) in NeighborOffsets)
                    {
                        var t = copy[x + dx, y + dy];
                        if (t.Type == center.Type)
                            continue;
                        counts.TryGetValue(t.Type, out int c);
                        counts[t.Type] = c + 1;
                    }

                    if (counts.Count == 0)
                        continue;

                    var bType = TerrainType.Water;
                    int max = 0;
                    foreach (var kv in counts)
                    {
                        if (kv.Value > max)
                        {
                            max = kv.Value;
                            bType = kv.Key;
                        }
                    }

                    if (max == 0)
                        continue;

                    Span<char> patternChars = stackalloc char[8];
                    int i = 0;
                    foreach (var (dx, dy) in NeighborOffsets)
                    {
                        patternChars[i++] = copy[x + dx, y + dy].Type == center.Type ? 'A' : 'B';
                    }
                    string pattern = new(patternChars);

                    var key = $"{center.Type.ToString().ToLower()}-{bType.ToString().ToLower()}";
                    if (transitionTiles.TryGetValue(key, out var dict) && dict.TryGetValue(pattern, out var tileInfo))
                    {
                        if (tileInfo.Id != 0)
                            map[x, y] = new Tile(center.Type, (ushort)tileInfo.Id);
                    }
                }
            }
        }
    }

    private readonly TransitionConverter transitionConverter = new();
}
