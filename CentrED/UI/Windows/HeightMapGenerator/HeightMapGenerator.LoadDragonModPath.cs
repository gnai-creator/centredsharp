using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void LoadDragonMod(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, TransitionTile>>>(File.ReadAllText(path), new JsonSerializerOptions
            {
                IncludeFields = true
            });
            if (data == null)
                return;
            dragonModEntries.Clear();
            foreach (var kv in data)
                dragonModEntries[kv.Key] = kv.Value;
            dragonModPath = path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load dragonmod transitions: {e.Message}");
        }
    }
}
