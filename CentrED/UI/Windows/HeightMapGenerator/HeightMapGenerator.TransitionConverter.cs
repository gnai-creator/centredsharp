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
        // Neighbor order must match PatternMap in DrawTransitionTiles:
        // [nw, n, ne, w, e, sw, s, se]
        private static readonly (int dx, int dy)[] NeighborOffsets = new (int, int)[]
        {
            (-1, -1), // NW
            (0, -1),  // N
            (1, -1),  // NE
            (-1, 0),  // W
            (1, 0),   // E
            (-1, 1),  // SW
            (0, 1),   // S
            (1, 1)    // SE
        };

        public Dictionary<string, Dictionary<string, TransitionTile>> ConvertTransitions(Dictionary<string, List<ushort>> groups)
        {
            var result = new Dictionary<string, Dictionary<string, TransitionTile>>();

            foreach (var group in groups)
            {
                var key = group.Key;
                if (!TryParseKey(key, out var a, out var b))
                    continue;

                var tiles = group.Value;
                if (tiles.Count != 9)
                    continue;

                var patterns = GenerateAll8NeighborPatterns();
                var mapping = new Dictionary<string, TransitionTile>();

                int i = 0;
                foreach (var pattern in patterns)
                {
                    if (pattern == "AAAAAAAA")
                        continue;

                    mapping[pattern] = new TransitionTile
                    {
                        Id = tiles[i % tiles.Count],
                        MinZ = 0,
                        MaxZ = 0
                    };
                    i++;
                }

                result[key] = mapping;
            }

            return result;
        }

        private static List<string> GenerateAll8NeighborPatterns()
        {
            var result = new List<string>();
            var chars = new char[8];
            GenerateRecursive(result, chars, 0);
            return result;
        }

        private static void GenerateRecursive(List<string> result, char[] chars, int index)
        {
            if (index == 8)
            {
                result.Add(new string(chars));
                return;
            }

            chars[index] = 'A';
            GenerateRecursive(result, chars, index + 1);
            chars[index] = 'B';
            GenerateRecursive(result, chars, index + 1);
        }

        private static bool TryParseKey(string key, out TerrainType a, out TerrainType b)
        {
            a = b = default;
            var parts = key.Split('-', 2);
            if (parts.Length != 2)
                return false;
            return Enum.TryParse(parts[0], true, out a) && Enum.TryParse(parts[1], true, out b);
        }

        public void ApplyTransitions(Tile[,] map, Dictionary<string, Dictionary<string, TransitionTile>> dict)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            var source = (Tile[,])map.Clone();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var center = source[x, y];
                    var centerType = center.Type;

                    Span<char> patternChars = stackalloc char[8];
                    for (int i = 0; i < NeighborOffsets.Length; i++)
                    {
                        var (dx, dy) = NeighborOffsets[i];
                        int nx = x + dx;
                        int ny = y + dy;
                        TerrainType nType = centerType;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            nType = source[nx, ny].Type;
                        patternChars[i] = nType == centerType ? 'A' : 'B';
                    }

                    string pattern = new(patternChars);

                    for (int i = 0; i < NeighborOffsets.Length; i++)
                    {
                        var (dx, dy) = NeighborOffsets[i];
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;

                        var neighborType = source[nx, ny].Type;
                        if (neighborType == centerType)
                            continue;

                        string key = $"{centerType.ToString().ToLower()}-{neighborType.ToString().ToLower()}";
                        if (!dict.TryGetValue(key, out var mappings))
                            continue;
                        if (mappings.TryGetValue(pattern, out var trans) && trans.Id != 0)
                        {
                            map[x, y].Id = (ushort)trans.Id;
                            break;
                        }
                    }
                }
            }
        }
    }

    private readonly TransitionConverter transitionConverter = new();
}