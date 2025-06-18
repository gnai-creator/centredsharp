using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    // Salva todos os dados convertidos em um único arquivo JSON
    public void SaveDragonModConvertedFiles(string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath) || DragonModConvertedFiles.Count == 0)
            return;
        File.WriteAllText(outputPath, JsonSerializer.Serialize(DragonModConvertedFiles, new JsonSerializerOptions { WriteIndented = true }));
    }



    // Salva todos os dados convertidos em arquivos JSON separados, replicando a estrutura Scripts/*
    public void SaveDragonModConvertedFilesToFolder(string outputFolder)
    {
        if (string.IsNullOrEmpty(outputFolder) || (DragonModConvertedFiles.Count == 0 && DragonModStaticFiles.Count == 0))
            return;

        var scriptsRoot = Path.Combine(outputFolder, "Scripts");

        foreach (var folder in DragonModConvertedFiles)
        {
            var folderPath = Path.Combine(scriptsRoot, folder.Key);
            Directory.CreateDirectory(folderPath);
            foreach (var file in folder.Value)
            {
                var jsonPath = Path.Combine(folderPath, Path.ChangeExtension(file.FileName, ".json"));
                var json = System.Text.Json.JsonSerializer.Serialize(file.Transitions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, json);
            }
        }

        foreach (var folder in DragonModStaticFiles)
        {
            var folderPath = Path.Combine(scriptsRoot, folder.Key);
            Directory.CreateDirectory(folderPath);
            foreach (var file in folder.Value)
            {
                var jsonPath = Path.Combine(folderPath, Path.ChangeExtension(file.FileName, ".json"));
                var json = System.Text.Json.JsonSerializer.Serialize(file.Statics, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, json);
            }
        }
    }
    // Salva os JSONs carregados (editados) em suas localizacoes relativas
    public void SaveLoadedDragonModJsonsToFolder(string outputFolder)
    {
        if (string.IsNullOrEmpty(outputFolder) || _dragonModJsonFiles.Count == 0)
            return;
        foreach (var kvp in _dragonModJsonFiles)
        {
            if (!_dragonModJsonPaths.TryGetValue(kvp.Key, out var relPath))
                continue;
            var path = Path.Combine(outputFolder, relPath);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(kvp.Value, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

}

