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
    private void LoadGroups(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                _statusText = $"Groups file not found: {Path.GetFileName(path)}";
                _statusColor = UIManager.Red;
                return;
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, Group>>(File.ReadAllText(path), new JsonSerializerOptions
            {
                IncludeFields = true
            });

            if (data == null)
            {
                _statusText = "Invalid groups file.";
                _statusColor = UIManager.Red;
                return;
            }

            tileGroups.Clear();
            foreach (var kv in data)
                tileGroups[kv.Key] = kv.Value;

            groupsPath = path;
            _statusText = $"Loaded groups from {Path.GetFileName(path)}";
            _statusColor = UIManager.Green;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load groups: {e.Message}");
            _statusText = $"Failed to load groups: {e.Message}";
            _statusColor = UIManager.Red;
        }
    }
}
