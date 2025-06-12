using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void LoadTransitions(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            var data = JsonSerializer.Deserialize<Dictionary<string, List<TransitionEntry>>>(File.ReadAllText(path), new JsonSerializerOptions
            {
                IncludeFields = true
            });
            if (data == null)
                return;
            transitions.Clear();
            _tileTypeMap.Clear();
            foreach (var kv in data)
            {
                transitions[kv.Key] = kv.Value;
                var parts = kv.Key.Split('-', 2);
                if (parts.Length != 2 ||
                    !Enum.TryParse<TerrainType>(parts[0], true, out var main) ||
                    !Enum.TryParse<TerrainType>(parts[1], true, out var other))
                    continue;

                foreach (var entry in kv.Value)
                {
                    for (int i = 0; i < entry.Tiles.Length; i++)
                    {
                        ushort id = entry.Tiles[i];
                        if (id == 0)
                            continue;
                        var type = i == 4 ? main : other;
                        _tileTypeMap[id] = type;
                    }
                }
            }
            if (!transitions.ContainsKey(selectedTransition))
                selectedTransition = transitions.Keys.FirstOrDefault() ?? string.Empty;
            selectedIndex = 0;
            transitionsPath = path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load transitions: {e.Message}");
        }
    }
}
