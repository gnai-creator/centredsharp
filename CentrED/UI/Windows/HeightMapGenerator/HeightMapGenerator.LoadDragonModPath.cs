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
            {
                _statusText = $"DragonMod file not found: {Path.GetFileName(path)}";
                _statusColor = UIManager.Red;
                return;
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, TransitionTile>>>(File.ReadAllText(path), new JsonSerializerOptions
            {
                IncludeFields = true
            });

            if (data == null)
            {
                _statusText = "Invalid DragonMod file.";
                _statusColor = UIManager.Red;
                return;
            }

            dragonModEntries.Clear();
            foreach (var kv in data)
                dragonModEntries[kv.Key] = kv.Value;

            dragonModPath = path;
            _statusText = $"Loaded DragonMod from {Path.GetFileName(path)}";
            _statusColor = UIManager.Green;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load dragonmod transitions: {e.Message}");
            _statusText = $"Failed to load DragonMod: {e.Message}";
            _statusColor = UIManager.Red;
        }
    }
}
