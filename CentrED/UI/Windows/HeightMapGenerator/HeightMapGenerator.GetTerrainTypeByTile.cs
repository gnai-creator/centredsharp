using System;
using System.Linq;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private TerrainType GetTerrainType(ushort tileId)
    {
        foreach (var kv in tileGroups)
        {
            if (kv.Value.Ids.Contains(tileId) &&
                Enum.TryParse<TerrainType>(kv.Key, true, out var type))
            {
                return type;
            }
        }
        return TerrainType.Water;
    }
}
