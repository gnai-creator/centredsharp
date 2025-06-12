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
            foreach (var kv in data)
                transitions[kv.Key] = kv.Value;
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
