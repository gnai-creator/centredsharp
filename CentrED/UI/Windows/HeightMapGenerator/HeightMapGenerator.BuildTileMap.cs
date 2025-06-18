using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using CentrED.Utility;
using CentrED.UI.Windows;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private Tile[,] tileMap;

    /// <summary>
    /// Ensures <see cref="tileMap"/> is initialized. This allows different
    /// generation routines to share the same underlying map without having to
    /// explicitly call <c>BuildTileMap</c> everywhere.
    /// </summary>

    private void EnsureTileMap(bool applyTransitions = false)
    {
        if (tileGroups.Count == 0 || heightData == null)
        {
            _statusText = "Height data or tile groups not loaded.";
            _statusColor = UIManager.Red;
            return;
        }

        if (tileMap != null && tileMap.Length < MapSizeX * MapSizeY)
            tileMap = new Tile[MapSizeX, MapSizeY];

        if (tileMap == null || tileMap.GetLength(0) != MapSizeX || tileMap.GetLength(1) != MapSizeY)
        {
            tileMap = new Tile[MapSizeX, MapSizeY];
            UpdateHeightData();
            _statusText = "Tile map initialized.";
            _statusColor = UIManager.Green;

        }
        if (heightData == null)
            UpdateHeightData();
        InitializeTileIdToType();
        BuildTileMap(applyTransitions);

    }
    private void BuildTileMap(bool applyTransitions = false)
    {


        if (heightData == null)
        {
            _statusText = "Height data not loaded.";
            _statusColor = UIManager.Red;
            return;
        }

        var groupsDict = tileGroups
            .Where(kv => kv.Value.Ids.Count > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        if (groupsDict.Count == 0)
        {
            _statusText = "No valid tile groups found.";
            _statusColor = UIManager.Red;
            return;
        }

        var groupsByHeight = BuildGroupsByHeightWithNames(groupsDict);
        var defaultCandidates = groupsDict.Select(kv => (kv.Key, kv.Value)).ToArray();

        Parallel.For(0, MapSizeY, y =>
        {
            for (int x = 0; x < MapSizeX; x++)
            {
                var z = heightData[x, y];
                if (!groupsByHeight.TryGetValue(z, out var candidates) || candidates.Length == 0)
                    candidates = defaultCandidates;

                var grpInfo = SelectGroup(candidates, x, y);
                var grp = grpInfo.Group;
                var grpName = grpInfo.Name;

                ushort id = 0;
                var validIds = grp.Ids.FindAll(t => t != 0 && t != 1 && t != 2);
                if (validIds.Count > 0)
                    id = validIds[DeterministicIndex(x, y, validIds.Count)];

                tileMap[x, y] = new Tile(GetTerrainType(z), id, x, y, grpName);
            }
        });

        transitionTiles = BuildTransitionTilesFromDragonMod();
        if (applyTransitions)
        {
            transitionConverter.ApplyAllTransitionPasses(tileMap, transitionTiles);
            var path = Path.Combine(AppContext.BaseDirectory, "transition_debug.png");
            transitionConverter.SaveDebugImage(tileMap, path);
        }


    }


    private Dictionary<string, Dictionary<string, TransitionTile>> BuildTransitionTilesFromDragonMod()
    {
        transitionTiles.Clear();

        foreach (var kv in _dragonModJsonFiles)
        {
            string key = HeightMapUtils.CleanTransitionKey(kv.Key).ToLowerInvariant().Replace("_", "").Replace(" ", "");

            // Load only groups defined in the order list
            if (!transitionsOrderList.Contains(key))
            {
                continue;
            }

            var dict = new Dictionary<string, TransitionTile>();
            foreach (var tr in kv.Value)
            {
                var cleanedTiles = tr.tiles.FindAll(t => t != 0);
                if (cleanedTiles.Count > 0)
                    dict[tr.pattern] = new TransitionTile { Tiles = cleanedTiles };
            }

            if (dict.Count > 0)
            {
                Console.WriteLine($"[TransitionLoad] Loaded key={key} with {dict.Count} patterns");
                transitionTiles[key] = dict;
            }
        }
        return transitionTiles;
    }


    private TerrainType GetTerrainType(sbyte z)
    {
        foreach (var kvp in tileGroups)
        {
            string groupName = kvp.Key.ToLowerInvariant();
            var group = kvp.Value;

            if (z >= group.MinHeight && z <= group.MaxHeight)
            {
                foreach (var typeAlias in HeightMapUtils.TypeAliases)
                {
                    if (typeAlias.Value.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                        return typeAlias.Key;
                }
            }
        }
        return TerrainType.None;
    }

    private static Dictionary<sbyte, Group[]> BuildGroupsByHeight(List<Group> groups)
    {
        var byHeight = new Dictionary<sbyte, List<Group>>(256);
        for (int h = -128; h <= 127; h++)
            byHeight[(sbyte)h] = new List<Group>();

        foreach (var g in groups)
        {
            for (int h = g.MinHeight; h <= g.MaxHeight; h++)
            {
                if (byHeight.TryGetValue((sbyte)h, out var list))
                    list.Add(g);
            }
        }

        var result = new Dictionary<sbyte, Group[]>(256);
        foreach (var kv in byHeight)
            result[kv.Key] = kv.Value.ToArray();
        return result;
    }

    private static Dictionary<sbyte, (string Name, Group Group)[]> BuildGroupsByHeightWithNames(Dictionary<string, Group> groups)
    {
        var byHeight = new Dictionary<sbyte, List<(string, Group)>>(256);
        for (int h = -128; h <= 127; h++)
            byHeight[(sbyte)h] = new List<(string, Group)>();

        foreach (var kv in groups)
        {
            var g = kv.Value;
            for (int h = g.MinHeight; h <= g.MaxHeight; h++)
            {
                if (byHeight.TryGetValue((sbyte)h, out var list))
                    list.Add((kv.Key, g));
            }
        }

        var result = new Dictionary<sbyte, (string, Group)[]>(256);
        foreach (var kv in byHeight)
            result[kv.Key] = kv.Value.ToArray();
        return result;
    }
}
