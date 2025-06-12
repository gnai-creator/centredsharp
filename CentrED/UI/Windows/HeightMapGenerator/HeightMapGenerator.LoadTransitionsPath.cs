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
            {
                _statusText = $"Transitions file not found: {Path.GetFileName(path)}";
                _statusColor = UIManager.Red;
                return;
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, List<TransitionEntry>>>(File.ReadAllText(path), new JsonSerializerOptions
            {
                IncludeFields = true
            });

            if (data == null)
            {
                _statusText = "Invalid transitions file.";
                _statusColor = UIManager.Red;
                return;
            }
            transitions.Clear();
            _tileTypeMap.Clear();
            foreach (var kv in data)
            {
                var list = kv.Value ?? new List<TransitionEntry>();
                foreach (var e in list)
                {
                    if (e.Tiles == null)
                        e.Tiles = new ushort[9];
                    else if (e.Tiles.Length < 9)
                    {
                        var tiles = new ushort[9];
                        Array.Copy(e.Tiles, tiles, e.Tiles.Length);
                        e.Tiles = tiles;
                    }
                }
                transitions[kv.Key] = list;
                var parts = kv.Key.Split('-', 2);
                if (parts.Length != 2 ||
                    !Enum.TryParse<TerrainType>(parts[0], true, out var main) ||
                    !Enum.TryParse<TerrainType>(parts[1], true, out var other))
                    continue;

                foreach (var entry in list)
                {
                    // Tiles already normalized above

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
            _statusText = $"Loaded transitions from {Path.GetFileName(path)}";
            _statusColor = UIManager.Green;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load transitions: {e.Message}");
            _statusText = $"Failed to load transitions: {e.Message}";
            _statusColor = UIManager.Red;
        }
    }
}
