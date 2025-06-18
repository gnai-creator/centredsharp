using CentrED;
using ClassicUO.Assets;

namespace CentrED.UI.Windows;

public static class TileNameResolver
{
    public static string GetTileName(Tile[,] tileMap, int x, int y)
    {
        var tile = tileMap[x, y];
        var landData = TileDataLoader.Instance.LandData;
        if (tile.Id < landData.Length)
            return landData[tile.Id].Name ?? string.Empty;
        return string.Empty;
    }
}
