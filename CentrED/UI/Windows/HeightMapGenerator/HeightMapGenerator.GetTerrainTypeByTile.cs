using System;
using System.Linq;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private TerrainType GetTerrainType(ushort tileId)
    {
        if (_tileTypeMap.TryGetValue(tileId, out var type))
            return type;

        foreach (var kv in tileGroups)
        {
            if (kv.Value.Ids.Contains(tileId) &&
                Enum.TryParse<TerrainType>(kv.Key, true, out var parsed))
            {
                return parsed;
            }
        }
        return TerrainType.Water;
    }
}
