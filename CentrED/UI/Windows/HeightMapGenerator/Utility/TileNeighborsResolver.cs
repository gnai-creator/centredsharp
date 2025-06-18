using System;

namespace CentrED.UI.Windows;

public static class TileNeighborsResolver
{
    private static readonly (int dx, int dy)[] NeighborOffsetsClockwise =
    {
        (-1, -1), (0, -1), (1, -1),
        (1, 0), (1, 1), (0, 1),
        (-1, 1), (-1, 0)
    };

    public static Tile[] GetNeighbors(Tile[,] snapshot, Tile tile, string pattern, int x, int y)
    {
        if (pattern.Length != 8)
            throw new ArgumentException("Pattern must have length 8", nameof(pattern));

        var actual = TilePatternResolver.GetPattern(snapshot, tile, x, y);
        if (!actual.Equals(pattern, StringComparison.Ordinal))
            throw new ArgumentException("Provided pattern does not match the tile at the given coordinates", nameof(pattern));

        Tile[] neighbors = new Tile[8];
        int width = snapshot.GetLength(0); // Assuming tile is 2x2
        int height = snapshot.GetLength(1); // Assuming tile is 2x2
        int i = 0;

        foreach (var (dx, dy) in NeighborOffsetsClockwise)
        {
            int nx = x + dx;
            int ny = y + dy;
            neighbors[i++] = (nx >= 0 && ny >= 0 && nx < width && ny < height) ? snapshot[nx, ny] : default;
        }

        return neighbors;
    }
}
