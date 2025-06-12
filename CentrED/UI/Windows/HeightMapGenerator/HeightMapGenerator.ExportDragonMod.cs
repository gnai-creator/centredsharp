using System;
using System.IO;
using System.Text.Json;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void ExportDragonMod() => ExportDragonMod(dragonModPath);

    private void ExportDragonMod(string path)
    {
        try
        {
            LoadTransitions();
            var export = ConvertTransitions();

            dragonModEntries.Clear();
            foreach (var kv in export)
                dragonModEntries[kv.Key] = kv.Value;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            };
            File.WriteAllText(path, JsonSerializer.Serialize(export, options));
            dragonModPath = path;
            _statusText = $"Exported DragonMod to {Path.GetFileName(path)}";
            _statusColor = UIManager.Green;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to export DragonMod: {e.Message}");
            _statusText = $"Failed to export DragonMod: {e.Message}";
            _statusColor = UIManager.Red;
        }
    }
}
