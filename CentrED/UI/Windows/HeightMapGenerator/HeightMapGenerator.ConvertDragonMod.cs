using System.Globalization;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{

    private void ConvertDragonModTxtToJson(string scriptsRoot)
    {
        if (string.IsNullOrEmpty(scriptsRoot) || !Directory.Exists(scriptsRoot))
            return;

        var scriptsDir = Path.Combine(scriptsRoot, "Scripts");
        if (!Directory.Exists(scriptsDir))
            return;

        var txtFiles = Directory.GetFiles(scriptsDir, "*.txt", SearchOption.AllDirectories);

        var mapOutputDir = Path.Combine(scriptsDir, "map");
        var staticsOutputDir = Path.Combine(scriptsDir, "statics");
        Directory.CreateDirectory(mapOutputDir);
        Directory.CreateDirectory(staticsOutputDir);

        int convertedCount = 0;
        foreach (var txtPath in txtFiles)
        {
            try
            {
                var byPattern = new Dictionary<string, List<uint>>();
                foreach (var line in File.ReadLines(txtPath))
                {
                    var l = line.Trim();
                    if (string.IsNullOrEmpty(l) || l.StartsWith("//"))
                        continue;
                    var parts = l.Split((char[])Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var pattern = parts[0];
                        var tileIds = ParseHexCodes(parts.Skip(1));
                        byPattern[pattern] = tileIds;
                    }
                }

                var transitions = new List<object>();
                foreach (var pattern in AllPatterns)
                {
                    byPattern.TryGetValue(pattern, out var list);
                    list ??= new List<uint>();
                    transitions.Add(new { pattern, tiles = list });
                }

                var fileName = Path.GetFileNameWithoutExtension(txtPath) + ".json";
                bool isStatics = txtPath.Contains($"{Path.DirectorySeparatorChar}statics{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
                var jsonPath = Path.Combine(isStatics ? staticsOutputDir : mapOutputDir, fileName);
                var json = System.Text.Json.JsonSerializer.Serialize(transitions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, json);
                convertedCount++;
            }
            catch (Exception ex)
            {
                _statusText = $"Erro ao converter {txtPath}: {ex.Message}";
                _statusColor = UIManager.Red;
            }
        }

        _statusText = $"Conversão concluída para {convertedCount} arquivos.";
        _statusColor = UIManager.Green;

        // Recarrega os arquivos convertidos para exibição
        LoadConvertedDragonModJsons(scriptsRoot);
    }
}