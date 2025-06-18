using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ImGuiNET;
using CentrED.Client;
using static CentrED.Application;
using CentrED.IO.Models;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{


    // Dicionário: nome do arquivo json -> lista de transições (pattern, tiles)
    private Dictionary<string, List<DragonModTransition>> _dragonModJsonFiles = new();
    private Dictionary<string, string> _dragonModJsonPaths = new();

    private Dictionary<string, List<DragonModTransition>> _dragonModStaticsJsonFiles = new();
    private Dictionary<string, string> _dragonModStaticsJsonPaths = new();


    // Use the same constant as TilesWindow for land index boundary
    private const int MAX_LAND_INDEX = 0x4000; // ArtLoader.MAX_LAND_DATA_INDEX_COUNT or 16384
    // Helper methods for tile info (use reflection to access private methods if needed)
    private TileInfo LandInfo(int index)
    {
        var tilesWindow = CEDGame.UIManager.GetWindow<TilesWindow>();
        var method = typeof(TilesWindow).GetMethod("LandInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method == null)
            return TileInfo.INVALID;

        var result = method.Invoke(tilesWindow, new object[] { index });
        if (result == null)
            return TileInfo.INVALID;

        var t = result.GetType();
        return new TileInfo(
            (int)(t.GetProperty("RealIndex")?.GetValue(result) ?? 0),
            (Microsoft.Xna.Framework.Graphics.Texture2D?)t.GetProperty("Texture")?.GetValue(result) ?? null!,
            (Microsoft.Xna.Framework.Rectangle)(t.GetProperty("Bounds")?.GetValue(result) ?? default(Microsoft.Xna.Framework.Rectangle)),
            (string)(t.GetProperty("Name")?.GetValue(result) ?? string.Empty),
            (string)(t.GetProperty("Flags")?.GetValue(result) ?? string.Empty),
            (uint)(t.GetProperty("Height")?.GetValue(result) ?? 0u));
    }

    private TileInfo StaticInfo(int index)
    {
        var tilesWindow = CEDGame.UIManager.GetWindow<TilesWindow>();
        var method = typeof(TilesWindow).GetMethod("StaticInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method == null)
            return TileInfo.INVALID;

        var result = method.Invoke(tilesWindow, new object[] { index });
        if (result == null)
            return TileInfo.INVALID;

        var t = result.GetType();
        return new TileInfo(
            (int)(t.GetProperty("RealIndex")?.GetValue(result) ?? 0),
            (Microsoft.Xna.Framework.Graphics.Texture2D?)t.GetProperty("Texture")?.GetValue(result) ?? null!,
            (Microsoft.Xna.Framework.Rectangle)(t.GetProperty("Bounds")?.GetValue(result) ?? default(Microsoft.Xna.Framework.Rectangle)),
            (string)(t.GetProperty("Name")?.GetValue(result) ?? string.Empty),
            (string)(t.GetProperty("Flags")?.GetValue(result) ?? string.Empty),
            (uint)(t.GetProperty("Height")?.GetValue(result) ?? 0u));
    }

    // Chame este método após converter os arquivos para atualizar a lista
    private void LoadConvertedDragonModJsons(string baseFolder)
    {
        _dragonModJsonFiles.Clear();
        _dragonModJsonPaths.Clear();
        _dragonModStaticsJsonFiles.Clear();
        _dragonModStaticsJsonPaths.Clear();

        var scriptsDir = Path.Combine(baseFolder, "Scripts");
        var mapDir = Path.Combine(scriptsDir, "map");
        var staticsDir = Path.Combine(scriptsDir, "statics");

        if (Directory.Exists(mapDir))
        {
            var jsonFiles = Directory.GetFiles(mapDir, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var jsonPath in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    var transitions = JsonSerializer.Deserialize<List<DragonModTransition>>(json);
                    if (transitions != null)
                    {
                        EnsureAllPatterns(transitions);
                        var key = HeightMapUtils.CleanTransitionKey(Path.Combine("map", Path.GetFileName(jsonPath)));
                        _dragonModJsonFiles[key] = transitions;
                        _dragonModJsonPaths[key] = Path.Combine("map", Path.GetFileName(jsonPath));
                    }
                }
                catch { /* Ignora arquivos json inválidos */ }
            }
        }

        if (Directory.Exists(staticsDir))
        {
            var jsonFiles = Directory.GetFiles(staticsDir, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var jsonPath in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    var transitions = JsonSerializer.Deserialize<List<DragonModTransition>>(json);
                    if (transitions != null)
                    {
                        EnsureAllPatterns(transitions);
                        var key = HeightMapUtils.CleanTransitionKey(Path.Combine("statics", Path.GetFileName(jsonPath)));
                        _dragonModStaticsJsonFiles[key] = transitions;
                        _dragonModStaticsJsonPaths[key] = Path.Combine("statics", Path.GetFileName(jsonPath));
                    }
                }
                catch { /* Ignora arquivos json inválidos */ }
            }
        }
    }

    // Carrega todos os arquivos .json de uma pasta Scripts/*
    public void LoadDragonModJsonsFromFolder(string baseFolder)
    {
        _dragonModJsonFiles.Clear();
        _dragonModJsonPaths.Clear();
        if (string.IsNullOrEmpty(baseFolder)) return;
        var scriptsDir = Path.Combine(baseFolder, "map");
        if (!Directory.Exists(scriptsDir)) return;
        var jsonFiles = Directory.GetFiles(scriptsDir, "*.json", SearchOption.AllDirectories);
        foreach (var jsonPath in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var transitions = JsonSerializer.Deserialize<List<DragonModTransition>>(json);
                if (transitions != null)
                {
                    // Mostra o caminho relativo Scripts/...
                    var relPath = Path.GetRelativePath(baseFolder, jsonPath);
                    var key = HeightMapUtils.CleanTransitionKey(relPath);
                    EnsureAllPatterns(transitions);
                    _dragonModJsonFiles[key] = transitions;
                    _dragonModJsonPaths[key] = relPath;
                }
            }
            catch { /* Ignora arquivos json inválidos */ }
        }

        // if (_dragonModJsonFiles.Count > 0)
        // {
        //     var cleanedKeys = _dragonModJsonFiles.Keys
        //         .Select(k => HeightMapUtils.CleanTransitionKey(k).ToLowerInvariant().Replace("_", "").Replace(" ", ""))
        //         .ToArray();

        //     if (transitionsOrderList.Length == 0)
        //         transitionsOrderList = cleanedKeys;
        //     else
        //         transitionsOrderList = transitionsOrderList
        //             .Concat(cleanedKeys.Where(k => !transitionsOrderList.Contains(k)))
        //             .ToArray();
        // }
    }

    public void LoadDragonModStaticsJsonsFromFolder(string baseFolder)
    {
        _dragonModStaticsJsonFiles.Clear();
        _dragonModStaticsJsonPaths.Clear();
        if (string.IsNullOrEmpty(baseFolder)) return;
        var staticsDir = Path.Combine(baseFolder, "Statics");
        if (!Directory.Exists(staticsDir)) return;
        var jsonFiles = Directory.GetFiles(staticsDir, "*.json", SearchOption.AllDirectories);
        foreach (var jsonPath in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var transitions = JsonSerializer.Deserialize<List<DragonModTransition>>(json);
                if (transitions != null)
                {
                    var relPath = Path.GetRelativePath(baseFolder, jsonPath);
                    var key = HeightMapUtils.CleanTransitionKey(relPath);
                    EnsureAllPatterns(transitions);
                    _dragonModStaticsJsonFiles[key] = transitions;
                    _dragonModStaticsJsonPaths[key] = relPath;
                }
            }
            catch { }
        }
    }



    // Método para desenhar a lista na aba DragonMod
    // ...existing code...
    private void DrawDragonModJsonList()
    {
        ImGui.Text("Arquivos JSON convertidos:");
        if (_dragonModJsonFiles.Count == 0)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "Nenhum arquivo convertido encontrado.");
        }
        else
        {
            int fileIndex = 0;
            foreach (var kvp in _dragonModJsonFiles)
            {
                string headerLabel = kvp.Key ?? "<null>";
                if (ImGui.CollapsingHeader(headerLabel))
                {
                    if (ImGui.BeginTable($"##DragonModTable{fileIndex}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupColumn("Pattern");
                        ImGui.TableSetupColumn("Tile 1");
                        ImGui.TableSetupColumn("Tile 2");
                        ImGui.TableSetupColumn("Tile 3");
                        ImGui.TableSetupColumn("Tile 4");
                        ImGui.TableHeadersRow();

                        int transitionIdx = 0;
                        foreach (var transition in kvp.Value)
                        {
                            ImGui.TableNextRow();
                            ImGui.PushID(transitionIdx);

                            ImGui.TableSetColumnIndex(0);
                            ImGui.TextUnformatted(transition.pattern ?? "<null>");

                            for (int i = 0; i < 4; i++)
                            {
                                ImGui.TableSetColumnIndex(i + 1);

                                // Garante que a lista existe e tem espaço
                                EnsureTileList(transition, i);
                                var tiles = transition.tiles!;
                                uint tileId = tiles[i];

                                TileInfo tileInfo = TileInfo.INVALID;
                                try
                                {
                                    if (tileId < MAX_LAND_INDEX)
                                        tileInfo = LandInfo((int)tileId);
                                    else
                                        tileInfo = StaticInfo((int)(tileId - MAX_LAND_INDEX));
                                }
                                catch { tileInfo = TileInfo.INVALID; }

                                ImGui.InvisibleButton($"##drop{i}", new System.Numerics.Vector2(44, 44));
                                var rectMin = ImGui.GetItemRectMin();
                                var rectMax = ImGui.GetItemRectMax();

                                if (ImGui.BeginDragDropTarget())
                                {
                                    unsafe
                                    {
                                        var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Land_DragDrop_Target_Type);
                                        if (payloadPtr.NativePtr != null)
                                        {
                                            int id = *(int*)payloadPtr.Data;
                                            EnsureTileList(transition, i);
                                            transition.tiles![i] = (uint)id;
                                        }

                                        payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Static_DragDrop_Target_Type);
                                        if (payloadPtr.NativePtr != null)
                                        {
                                            int id = *(int*)payloadPtr.Data + MAX_LAND_INDEX;
                                            EnsureTileList(transition, i);
                                            transition.tiles![i] = (uint)id;
                                        }
                                    }
                                    ImGui.EndDragDropTarget();
                                }

                                var drawList = ImGui.GetWindowDrawList();
                                if (tileInfo.Texture != null)
                                {
                                    var texPtr = CEDGame.UIManager._uiRenderer.BindTexture(tileInfo.Texture);
                                    var fWidth = (float)tileInfo.Texture.Width;
                                    var fHeight = (float)tileInfo.Texture.Height;
                                    var uv0 = new System.Numerics.Vector2(tileInfo.Bounds.X / fWidth, tileInfo.Bounds.Y / fHeight);
                                    var uv1 = new System.Numerics.Vector2((tileInfo.Bounds.X + tileInfo.Bounds.Width) / fWidth, (tileInfo.Bounds.Y + tileInfo.Bounds.Height) / fHeight);
                                    drawList.AddImage(texPtr, rectMin, rectMin + new System.Numerics.Vector2(44, 44), uv0, uv1);
                                }
                                else
                                {
                                    drawList.AddRect(rectMin, rectMin + new System.Numerics.Vector2(44, 44), ImGui.GetColorU32(UIManager.Pink));
                                }

                                var text = $"0x{tileId:X4}";
                                var textSize = ImGui.CalcTextSize(text);
                                drawList.AddText(new System.Numerics.Vector2(rectMin.X, rectMax.Y - textSize.Y), ImGui.GetColorU32(ImGuiCol.Text), text);
                            }
                            ImGui.PopID();
                            transitionIdx++;
                        }

                        ImGui.EndTable();
                    }
                }
                fileIndex++;
            }
        }
    }
    // ...existing code...

    // Helper to ensure transition.tiles is not null and has at least i+1 elements
    private void EnsureTileList(DragonModTransition transition, int i)
    {
        if (transition.tiles == null)
            transition.tiles = new List<uint>(new uint[i + 1]);
        while (transition.tiles.Count <= i)
            transition.tiles.Add(0);
    }

    private void DrawDragonModStaticsJsonList()
    {
        ImGui.Text("Arquivos JSON de Statics:");
        if (_dragonModStaticsJsonFiles.Count == 0)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "Nenhum arquivo encontrado.");
        }
        else
        {
            int fileIndex = 0;
            foreach (var kvp in _dragonModStaticsJsonFiles)
            {
                string headerLabel = kvp.Key ?? "<null>";
                if (ImGui.CollapsingHeader(headerLabel))
                {
                    if (ImGui.BeginTable($"##DragonModStaticsTable{fileIndex}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupColumn("Pattern");
                        ImGui.TableSetupColumn("Tile 1");
                        ImGui.TableSetupColumn("Tile 2");
                        ImGui.TableSetupColumn("Tile 3");
                        ImGui.TableSetupColumn("Tile 4");
                        ImGui.TableHeadersRow();

                        int transitionIdx = 0;
                        foreach (var transition in kvp.Value)
                        {
                            ImGui.TableNextRow();
                            ImGui.PushID(transitionIdx);
                            ImGui.TableSetColumnIndex(0);
                            ImGui.TextUnformatted(transition.pattern ?? "<null>");
                            for (int i = 0; i < 4; i++)
                            {
                                ImGui.TableSetColumnIndex(i + 1);
                                EnsureTileList(transition, i);
                                var tiles = transition.tiles!;
                                uint tileId = tiles[i];
                                TileInfo tileInfo = TileInfo.INVALID;
                                try
                                {
                                    if (tileId >= MAX_LAND_INDEX)
                                    {
                                        tileInfo = StaticInfo((int)(tileId - MAX_LAND_INDEX));
                                    }
                                    else
                                    {
                                        tileInfo = StaticInfo((int)tileId);
                                        if (tileInfo == TileInfo.INVALID)
                                            tileInfo = LandInfo((int)tileId);
                                    }
                                }
                                catch { tileInfo = TileInfo.INVALID; }

                                ImGui.InvisibleButton($"##dropstatic{i}", new System.Numerics.Vector2(44, 44));
                                var rectMin = ImGui.GetItemRectMin();
                                var rectMax = ImGui.GetItemRectMax();
                                var drawList = ImGui.GetWindowDrawList();
                                if (tileInfo.Texture != null)
                                {
                                    var texPtr = CEDGame.UIManager._uiRenderer.BindTexture(tileInfo.Texture);
                                    float fWidth = tileInfo.Texture.Width;
                                    float fHeight = tileInfo.Texture.Height;
                                    var uv0 = new System.Numerics.Vector2(tileInfo.Bounds.X / fWidth, tileInfo.Bounds.Y / fHeight);
                                    var uv1 = new System.Numerics.Vector2((tileInfo.Bounds.X + tileInfo.Bounds.Width) / fWidth, (tileInfo.Bounds.Y + tileInfo.Bounds.Height) / fHeight);
                                    drawList.AddImage(texPtr, rectMin, rectMin + new System.Numerics.Vector2(44, 44), uv0, uv1);
                                }
                                else
                                {
                                    drawList.AddRect(rectMin, rectMin + new System.Numerics.Vector2(44, 44), ImGui.GetColorU32(UIManager.Pink));
                                }

                                var text = $"0x{tileId:X4}";
                                var textSize = ImGui.CalcTextSize(text);
                                drawList.AddText(new System.Numerics.Vector2(rectMin.X, rectMax.Y - textSize.Y), ImGui.GetColorU32(ImGuiCol.Text), text);
                            }
                            ImGui.PopID();
                            transitionIdx++;
                        }

                        ImGui.EndTable();
                    }
                }
                fileIndex++;
            }
        }
    }
}


