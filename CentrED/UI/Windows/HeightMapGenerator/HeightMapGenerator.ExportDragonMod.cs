using System.IO;
using System.Text.Json;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void ExportDragonMod() => ExportDragonMod(dragonModPath);

    private void ExportDragonMod(string path)
    {
        var export = ConvertTransitions();
        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        File.WriteAllText(path, JsonSerializer.Serialize(export, options));
        dragonModPath = path;
    }
}
