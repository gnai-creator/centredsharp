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

            // Pre-process transitions so we don't need to parse the key for every tile
            var parsed = new Dictionary<TerrainType, List<(TerrainType bType, Dictionary<string, TransitionTile> dict)>>();
            foreach (var kv in transitionTiles)
            {
                if (!TryParseKey(kv.Key, out var aType, out var bType))
                    continue;
                if (!parsed.TryGetValue(aType, out var list))
                    list = parsed[aType] = new();
                list.Add((bType, kv.Value));
            }

            Tile[,] copy = (Tile[,])map.Clone();

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    var center = copy[x, y];
                    if (!parsed.TryGetValue(center.Type, out var transitions))
                        continue;

                    foreach (var (bType, dict) in transitions)
                    {
                        bool hasB = false;
                        Span<char> patternChars = stackalloc char[8];
                        for (int i = 0; i < NeighborOffsets.Length; i++)
                        {
                            var (dx, dy) = NeighborOffsets[i];
                            var t = copy[x + dx, y + dy];
                            if (t.Type == bType)
                                hasB = true;
                            patternChars[i] = t.Type == center.Type ? 'A' : 'B';
                        }

                        if (!hasB)
                            continue;

                        string pattern = new(patternChars);
                        if (pattern == "AAAAAAAA")
                            continue; // no transition when fully surrounded by A

                        if (dict.TryGetValue(pattern, out var tileInfo))
                        {
                            map[x, y] = new Tile(center.Type, (ushort)tileInfo.Id);
                            break; // apply only one transition per tile
                        }
                    }
                }
            }
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
