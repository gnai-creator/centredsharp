using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private static readonly Vector2 TileSize = new(44, 44);
    // Neighbor order is row-major starting from NW without the center:
    // [nw, n, ne, w, e, sw, s, se]
    private static readonly int[] PatternMap = { 0, 1, 2, 3, 5, 6, 7, 8 };

    private void DrawTransitionTiles()
    {
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
                if (TinyFileDialogs.TrySaveFile("Save Entries", transitionsPath, new[] { "*.json" }, "JSON Files", out var path))
                    SaveTransitions(path);
            }
            ImGui.SameLine();
            if (ImGui.Button("Load"))
            {
                if (TinyFileDialogs.TryOpenFile("Load Entries", Environment.CurrentDirectory, new[] { "*.json" }, "JSON Files", false, out var path))
                    LoadTransitions(path);
            }
            ImGui.SameLine();
            if (ImGui.Button("Export DragonMod"))
            {
                if (TinyFileDialogs.TrySaveFile("Export DragonMod", dragonModPath, new[] { "*.json" }, "JSON Files", out var path))
                    ExportDragonMod(path);
            }
            ImGui.SameLine();
            if (ImGui.Button("Load DragonMod"))
            {
                if (TinyFileDialogs.TryOpenFile("Load DragonMod", Environment.CurrentDirectory, new[] { "*.json" }, "JSON Files", false, out var path))
                    LoadDragonMod(path);
            }
        }
        DrawDragonModEntries();
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

    private void DrawEntry(TransitionEntry entry)
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int idx = row * 3 + col;
                DrawTileButton(ref entry.Tiles[idx], idx);
                if (col < 2)
                    ImGui.SameLine();
            }
        }
        ImGui.InputInt("MinZ", ref entry.MinZ);
        ImGui.InputInt("MaxZ", ref entry.MaxZ);
    }

    private void DrawTileButton(ref ushort id, int index)
    {
        ImGui.PushID(index);
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

    private void DrawDragonModEntries()
    {
        if (dragonModEntries.Count == 0)
            return;

        ImGui.Text($"DragonMod Entries ({Path.GetFileName(dragonModPath)})");
        if (ImGui.BeginChild("DragonModEntries", new Vector2(0, 200), ImGuiChildFlags.Borders))
        {
            foreach (var kv in dragonModEntries)
            {
                if (ImGui.TreeNode(kv.Key))
                {
                    foreach (var pat in kv.Value.OrderBy(p => p.Key))
                    {
                        var entry = pat.Value;
                        ImGui.PushID($"{kv.Key}_{pat.Key}");
                        ImGui.Text(pat.Key);
                        ImGui.SameLine(120);
                        int id = entry.Id;
                        ImGui.InputInt("Id", ref id);
                        entry.Id = id;
                        ImGui.SameLine();
                        ImGui.PushItemWidth(60);
                        ImGui.InputInt("MinZ", ref entry.MinZ);
                        ImGui.SameLine();
                        ImGui.InputInt("MaxZ", ref entry.MaxZ);
                        ImGui.PopItemWidth();
                        kv.Value[pat.Key] = entry;
                        ImGui.PopID();
                    }
                    ImGui.TreePop();
                }
            }
            ImGui.EndChild();
        }
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

    private Dictionary<string, Dictionary<string, TransitionTile>> ConvertTransitions()
    {
        var export = new Dictionary<string, Dictionary<string, TransitionTile>>();

        Dictionary<string, TransitionTile> createDict()
        {
            var d = new Dictionary<string, TransitionTile>();
            for (int i = 0; i < 256; i++)
                d[IntToPattern(i)] = new TransitionTile();
            return d;
        }

        foreach (var kv in transitions)
        {
            var parts = kv.Key.Split('-', 2);
            if (parts.Length != 2)
                continue;

            if (!export.TryGetValue(kv.Key, out var dict))
                dict = export[kv.Key] = createDict();

            foreach (var entry in kv.Value)
            {
                var pattern = ComputePattern(entry);
                int mappedIndex = GetTileIndexForPattern(pattern);
                dict[pattern] = new TransitionTile { Id = entry.Tiles[mappedIndex], MinZ = entry.MinZ, MaxZ = entry.MaxZ };
            }
        }

        return export;
    }

    private static string IntToPattern(int value)
    {
        Span<char> pattern = stackalloc char[8];
        for (int i = 0; i < 8; i++)
        {
            pattern[7 - i] = (value & (1 << i)) != 0 ? 'B' : 'A';
        }
        return new string(pattern);
    }

    private string ComputePattern(TransitionEntry entry)
    {
        Span<char> pattern = stackalloc char[8];
        ushort center = entry.Tiles[4];
        var centerType = GetTerrainType(center);
        for (int i = 0; i < 8; i++)
        {
            ushort val = entry.Tiles[PatternMap[i]];
            pattern[i] = GetTerrainType(val) == centerType ? 'A' : 'B';
        }
        return new string(pattern);
    }


    internal static int GetTileIndexForPattern(string pattern)
    {
        // Pattern indices follow [nw,n,ne,w,e,sw,s,se]
        bool n = pattern[1] == 'B';
        bool e = pattern[4] == 'B';
        bool s = pattern[6] == 'B';
        bool w = pattern[3] == 'B';
        bool nw = pattern[0] == 'B';
        bool ne = pattern[2] == 'B';
        bool se = pattern[7] == 'B';
        bool sw = pattern[5] == 'B';

        if (n && w && nw) return 0;
        if (n && e && ne) return 2;
        if (s && w && sw) return 6;
        if (s && e && se) return 8;
        if (n) return 1;
        if (e) return 5;
        if (s) return 7;
        if (w) return 3;
        return 4;
    }

}
