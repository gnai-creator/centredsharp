using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private string _statusText = string.Empty;
    private System.Numerics.Vector4 _statusColor = UIManager.Green;

    private static List<uint> ParseHexCodes(IEnumerable<string> codes)
    {
        List<uint> list = new();
        foreach (var code in codes)
        {
            if (uint.TryParse(code, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var val))
            {
                list.Add(val);
                if (list.Count == 4)
                    break;
            }
            else
            {
                // stop parsing at first non-hex token (usually altitude info)
                break;
            }
        }
        return list;
    }


    // Dicionário: pasta -> lista de arquivos convertidos
    public Dictionary<string, List<DragonModFileData>> DragonModConvertedFiles { get; private set; } = new();
    public Dictionary<string, List<DragonModFileData>> DragonModStaticFiles { get; private set; } = new();

    // Carrega todos os arquivos txt (exceto history.txt e pasta Statics) e converte para a estrutura
    // Carrega todos os arquivos txt (exceto history.txt e pasta Statics) e converte para a estrutura
    public void LoadDragonModFiles(string scriptsRoot)
    {
        DragonModConvertedFiles.Clear();
        DragonModStaticFiles.Clear();
        if (string.IsNullOrEmpty(scriptsRoot) || !Directory.Exists(scriptsRoot))
            return;
        try
        {
            var scriptsDir = Path.Combine(scriptsRoot, "Scripts");
            if (!Directory.Exists(scriptsDir))
                return;
            var folders = Directory.GetDirectories(scriptsDir)
                .Where(f => !Path.GetFileName(f).Equals("statics", StringComparison.OrdinalIgnoreCase));
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                var files = Directory.GetFiles(folder, "*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f => Path.GetFileName(f).ToLowerInvariant() != "history.txt");
                var fileDataList = new List<DragonModFileData>();
                foreach (var txtPath in files)
                {
                    var transitions = new List<DragonModTransition>();
                    try
                    {
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
                                transitions.Add(new DragonModTransition { pattern = pattern, tiles = tileIds });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusText = $"Erro ao ler arquivo {txtPath}: {ex.Message}";
                        _statusColor = UIManager.Red;
                    }
                    fileDataList.Add(new DragonModFileData
                    {
                        Folder = folderName,
                        FileName = Path.GetFileName(txtPath),
                        Transitions = transitions
                    });
                }
                if (fileDataList.Count > 0)
                    DragonModConvertedFiles[folderName] = fileDataList;
            }

            var staticsDir = Path.Combine(scriptsDir, "statics");
            if (Directory.Exists(staticsDir))
            {
                var files = Directory.GetFiles(staticsDir, "*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f => Path.GetFileName(f).ToLowerInvariant() != "history.txt");
                var fileDataList = new List<DragonModFileData>();
                foreach (var txtPath in files)
                {
                    var statics = new List<DragonModTransition>();
                    try
                    {
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
                                statics.Add(new DragonModTransition { pattern = pattern, tiles = tileIds });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusText = $"Erro ao ler arquivo {txtPath}: {ex.Message}";
                        _statusColor = UIManager.Red;
                    }
                    fileDataList.Add(new DragonModFileData
                    {
                        Folder = "statics",
                        FileName = Path.GetFileName(txtPath),
                        Statics = statics
                    });
                }
                if (fileDataList.Count > 0)
                    DragonModStaticFiles["statics"] = fileDataList;
            }
        }
        catch (Exception ex)
        {
            _statusText = $"Erro ao ler pastas DragonMod: {ex.Message}";
            _statusColor = UIManager.Red;
        }
    }

}
