using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Numerics;
using ImGuiNET;
using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;
using CentrED.IO.Models;
using static CentrED.Application;
namespace CentrED.UI.Windows;

public class TransitionTilesWindow : Window
{
    private class TransitionTile
    {
        public int Id;
        public int MinZ;
        public int MaxZ;
    }

    private class TransitionEntry
    {
        public ushort[] Tiles = new ushort[9];
        public int MinZ;
        public int MaxZ;
    }

    private readonly Dictionary<string, List<TransitionEntry>> transitions = new()
    {
        ["water-sand"] = new(),
        ["sand-water"] = new(),
        ["sand-grass"] = new(),
        ["grass-sand"] = new(),
        ["grass-jungle"] = new(),
        ["jungle-grass"] = new(),
        ["jungle-rock"] = new(),
        ["rock-jungle"] = new(),
        ["rock-snow"] = new()
    };

    private string selectedTransition = "sand-grass";
    private int selectedIndex = 0;
    private const string SaveFile = "transition_tiles.json";

    public override string Name => "Transition Tiles";
    public override WindowState DefaultState => new() { IsOpen = false };

    protected override void InternalDraw()
    {
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }

        DrawTransitionSelector();
        ImGui.Separator();
        if (transitions.TryGetValue(selectedTransition, out var list))
        {
            if (selectedIndex >= list.Count)
                selectedIndex = Math.Max(0, list.Count - 1);
            if (list.Count == 0)
            {
                ImGui.Text("No entries");
            }
            else
            {
                DrawEntry(list[selectedIndex]);
            }
            if (ImGui.Button("Add Entry"))
            {
                list.Add(new TransitionEntry());
                selectedIndex = list.Count - 1;
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(list.Count == 0);
            if (ImGui.Button("Remove Entry"))
            {
                list.RemoveAt(selectedIndex);
                if (selectedIndex >= list.Count)
                    selectedIndex = Math.Max(0, list.Count - 1);
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Save"))
            {
                if (TinyFileDialogs.TrySaveFile("Save Entries", SaveFile, new[] { "*.json" }, "JSON Files", out var path))
                    SaveEntries(path);
            }
            ImGui.SameLine();
            if (ImGui.Button("Load"))
            {
                if (TinyFileDialogs.TryOpenFile("Load Entries", Environment.CurrentDirectory, new[] { "*.json" }, "JSON Files", false, out var path))
                    LoadEntries(path);
            }
            ImGui.SameLine();
            if (ImGui.Button("Export DragonMod"))
            {
                if (TinyFileDialogs.TrySaveFile("Export", SaveFile, new[] { "*.json" }, "JSON Files", out var path))
                    ExportDragonMod(path);
            }
        }
    }

    private void DrawTransitionSelector()
    {
        var keys = new List<string>(transitions.Keys);
        int current = keys.IndexOf(selectedTransition);
        if (ImGui.BeginCombo("Transition", selectedTransition))
        {
            foreach (var k in keys)
            {
                bool isSel = k == selectedTransition;
                if (ImGui.Selectable(k, isSel))
                {
                    selectedTransition = k;
                    selectedIndex = 0;
                }
            }
            ImGui.EndCombo();
        }
    }

    private static readonly Vector2 TileSize = new(44, 44);
    private static readonly int[] PatternMap = { 0, 1, 2, 5, 8, 7, 6, 3 };

    private void DrawEntry(TransitionEntry entry)
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int idx = row * 3 + col;
                DrawTileButton(ref entry.Tiles[idx]);
                if (col < 2)
                    ImGui.SameLine();
            }
        }
        ImGui.InputInt("MinZ", ref entry.MinZ);
        ImGui.InputInt("MaxZ", ref entry.MaxZ);
    }

    private void DrawTileButton(ref ushort id)
    {
        ImGui.PushID(Guid.NewGuid().ToString());
        if (id != 0)
        {
            var tex = CalculateButtonTexture(id);
            if (ImGui.ImageButton("tile", tex.texPtr, TileSize, tex.uv0, tex.uv1))
            {
                id = 0;
            }
            UIManager.Tooltip($"0x{id:X4}");
        }
        else
        {
            ImGui.Button("---", TileSize);
        }

        if (ImGui.BeginDragDropTarget())
        {
            var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Land_DragDrop_Target_Type);
            unsafe
            {
                if (payloadPtr.NativePtr != null)
                {
                    var dataPtr = (int*)payloadPtr.Data;
                    id = (ushort)dataPtr[0];
                }
            }
            ImGui.EndDragDropTarget();
        }
        ImGui.PopID();
    }

    private (nint texPtr, Vector2 uv0, Vector2 uv1) CalculateButtonTexture(ushort tileId)
    {
        var spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(TileDataLoader.Instance.LandData[tileId].TexID);
        if (spriteInfo.Texture == null)
        {
            spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(0x0001);
        }
        var tex = spriteInfo.Texture;
        var bounds = spriteInfo.UV;
        var texPtr = CEDGame.UIManager._uiRenderer.BindTexture(tex);
        var fWidth = (float)tex.Width;
        var fHeight = (float)tex.Height;
        var uv0 = new Vector2(bounds.X / fWidth, bounds.Y / fHeight);
        var uv1 = new Vector2((bounds.X + bounds.Width) / fWidth, (bounds.Y + bounds.Height) / fHeight);
        return (texPtr, uv0, uv1);
    }

    private void SaveEntries(string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        File.WriteAllText(path, JsonSerializer.Serialize(transitions, options));
    }

    private void LoadEntries(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            var data = JsonSerializer.Deserialize<Dictionary<string, List<TransitionEntry>>>(File.ReadAllText(path), new JsonSerializerOptions { IncludeFields = true });
            if (data != null)
            {
                transitions.Clear();
                foreach (var kv in data)
                    transitions[kv.Key] = kv.Value;
                if (!transitions.ContainsKey(selectedTransition))
                    selectedTransition = transitions.Keys.FirstOrDefault() ?? "";
                selectedIndex = 0;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load transitions: {e.Message}");
        }
    }

    private void ExportDragonMod(string path)
    {
        var export = new Dictionary<string, Dictionary<string, TransitionTile>>();
        foreach (var kv in transitions)
        {
            var dict = new Dictionary<string, TransitionTile>();
            foreach (var entry in kv.Value)
            {
                var pattern = ComputePattern(entry);
                dict[pattern] = new TransitionTile { Id = entry.Tiles[4], MinZ = entry.MinZ, MaxZ = entry.MaxZ };
            }
            export[kv.Key] = dict;
        }
        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        File.WriteAllText(path, JsonSerializer.Serialize(export, options));
    }

    private string ComputePattern(TransitionEntry entry)
    {
        Span<char> pattern = stackalloc char[8];
        ushort center = entry.Tiles[4];
        for (int i = 0; i < 8; i++)
        {
            ushort val = entry.Tiles[PatternMap[i]];
            pattern[i] = val == center ? 'A' : 'B';
        }
        return new string(pattern);
    }
}
