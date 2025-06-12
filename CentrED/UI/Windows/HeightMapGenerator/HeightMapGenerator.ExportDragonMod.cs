using System.IO;
using System.Linq;
using System.Text.Json;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void ExportDragonMod() => ExportDragonMod(dragonModPath);

    private void ExportDragonMod(string path)
    {
        LoadTransitions();
        var export = transitionConverter.ConvertTransitions(
            tileGroups.ToDictionary(kv => kv.Key, kv => kv.Value.Ids));

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
    }
}
