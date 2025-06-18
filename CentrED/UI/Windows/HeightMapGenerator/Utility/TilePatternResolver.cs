using System;

namespace CentrED.UI.Windows;

public static class TilePatternResolver
{
    private static readonly (int dx, int dy)[] NeighborOffsetsClockwise =
    {
        (-1, -1), (0, -1), (1, -1),
        (1, 0), (1, 1), (0, 1),
        (-1, 1), (-1, 0)
    };

    public static string GetPattern(Tile[,] snapshot, Tile tile, int x, int y)
    {
        Span<char> patternChars = stackalloc char[8];
        int i = 0;
        int width = snapshot.GetLength(0);
        int height = snapshot.GetLength(1);

        var center = tile;
        string centerAlias = HeightMapUtils.TypeAliases[center.Type];

        foreach (var (dx, dy) in NeighborOffsetsClockwise)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            {
                patternChars[i++] = 'B';
                continue;
            }

            var neighbor = new Tile(snapshot[nx, ny].Type, snapshot[nx, ny].Id, nx, ny, snapshot[nx, ny].GroupName);
            string neighborAlias = HeightMapUtils.TypeAliases[neighbor.Type];
            patternChars[i++] = neighborAlias == centerAlias ? 'A' : 'B';
        }

        return new string(patternChars);
    }
}
