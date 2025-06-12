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
    private void SaveTransitions(string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        foreach (var kv in transitions)
        {
            var list = kv.Value;
            if (list.Count == 0)
                list.Add(new TransitionEntry { Tiles = new ushort[9] });
            foreach (var entry in list)
            {
                if (entry.Tiles == null)
                    entry.Tiles = new ushort[9];
                else if (entry.Tiles.Length < 9)
                {
                    var tiles = new ushort[9];
                    Array.Copy(entry.Tiles, tiles, entry.Tiles.Length);
                    entry.Tiles = tiles;
                }
            }
        }
        File.WriteAllText(path, JsonSerializer.Serialize(transitions, options));
        transitionsPath = path;
    }
}
