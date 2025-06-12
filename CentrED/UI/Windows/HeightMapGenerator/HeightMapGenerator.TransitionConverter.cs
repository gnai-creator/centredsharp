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
    }

    private readonly TransitionConverter transitionConverter = new();
}