using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void DrawEnviromentTab()
    {
        ImGui.Text("Enviroments");
        if (ImGui.BeginChild("EnviromentList", new System.Numerics.Vector2(0, 120), ImGuiChildFlags.Borders))
        {
            foreach (var kv in enviromentStatics.ToArray())
            {
                ImGui.PushID($"env_{kv.Key}");
                bool isSel = selectedEnviroment == kv.Key;
                if (ImGui.Selectable(kv.Key, isSel))
                    selectedEnviroment = kv.Key;
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        enviromentStatics.Remove(kv.Key);
                        if (selectedEnviroment == kv.Key) selectedEnviroment = string.Empty;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
                ImGui.PopID();
            }
            ImGui.EndChild();
        }
        ImGui.InputText("##newenv", ref newEnviromentName, 32);
        ImGui.SameLine();
        if (ImGui.Button("Add##env"))
        {
            if (!string.IsNullOrWhiteSpace(newEnviromentName) && !enviromentStatics.ContainsKey(newEnviromentName))
            {
                enviromentStatics[newEnviromentName] = new EnviromentData();
                selectedEnviroment = newEnviromentName;
                newEnviromentName = string.Empty;
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Load Enviroments"))
        {
            if (TinyFileDialogs.TrySelectFolder("Load Enviroments", System.Environment.CurrentDirectory, out var folder))
            {
                LoadEnviromentsFromFolder(folder);
                _statusText = $"Enviroments carregados de {folder}";
                _statusColor = UIManager.Green;
            }
        }
        if (!string.IsNullOrEmpty(selectedEnviroment) && enviromentStatics.TryGetValue(selectedEnviroment, out var data))
        {
            if (ImGui.BeginChild($"{selectedEnviroment}_rows", new System.Numerics.Vector2(0, 140), ImGuiChildFlags.Borders))
            {
                if (ImGui.BeginTable("EnvRows", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Chance");
                    ImGui.TableSetupColumn("Tile 1");
                    ImGui.TableSetupColumn("Tile 2");
                    ImGui.TableSetupColumn("Tile 3");
                    ImGui.TableSetupColumn("Tile 4");
                    ImGui.TableSetupColumn(" ");
                    ImGui.TableHeadersRow();

                    for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
                    {
                        var row = data.Rows[rowIndex];
                        ImGui.TableNextRow();
                        ImGui.PushID(rowIndex);

                        ImGui.TableSetColumnIndex(0);
                        float chance = row.Chance;
                        if (ImGui.DragFloat("##chance", ref chance, 0.1f, 0f, 100f, "%.1f%%"))
                            row.Chance = chance;

                        for (int i = 0; i < 4; i++)
                        {
                            ImGui.TableSetColumnIndex(i + 1);
                            EnsureTileList(row, i);
                            ushort tileId = row.Tiles[i];

                            ImGui.InvisibleButton($"##drop{i}", new System.Numerics.Vector2(44, 44));
                            var rectMin = ImGui.GetItemRectMin();
                            var rectMax = ImGui.GetItemRectMax();

                            if (ImGui.BeginDragDropTarget())
                            {
                                unsafe
                                {
                                    var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Static_DragDrop_Target_Type);
                                    if (payloadPtr.NativePtr != null)
                                    {
                                        int id = *(int*)payloadPtr.Data;
                                        EnsureTileList(row, i);
                                        row.Tiles[i] = (ushort)id;
                                    }
                                }
                                ImGui.EndDragDropTarget();
                            }

                            var drawList = ImGui.GetWindowDrawList();
                            var info = StaticInfo(tileId);
                            if (info.Texture != null)
                            {
                                var texPtr = CEDGame.UIManager._uiRenderer.BindTexture(info.Texture);
                                float fWidth = info.Texture.Width;
                                float fHeight = info.Texture.Height;
                                var uv0 = new System.Numerics.Vector2(info.Bounds.X / fWidth, info.Bounds.Y / fHeight);
                                var uv1 = new System.Numerics.Vector2((info.Bounds.X + info.Bounds.Width) / fWidth, (info.Bounds.Y + info.Bounds.Height) / fHeight);
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

                        ImGui.TableSetColumnIndex(5);
                        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(1, 0, 0, 0.2f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(1, 0, 0, 1));
                        if (ImGui.SmallButton("x"))
                        {
                            data.Rows.RemoveAt(rowIndex);
                            ImGui.PopStyleColor(2);
                            ImGui.PopID();
                            rowIndex--;
                            continue;
                        }
                        ImGui.PopStyleColor(2);
                        ImGui.PopID();
                    }

                    ImGui.EndTable();
                }
                if (ImGui.Button($"Add Row##{selectedEnviroment}"))
                {
                    data.Rows.Add(new EnviromentRow { Chance = 100f, Tiles = new List<ushort> { 0, 0, 0, 0 } });
                }
                ImGui.EndChild();
            }

            if (ImGui.Button("Load Enviroment"))
            {
                if (TinyFileDialogs.TrySelectFolder("Load Enviroment", System.Environment.CurrentDirectory, out var folder))
                {
                    LoadEnviromentsFromFolder(folder);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Save Enviroments"))
            {
                if (TinyFileDialogs.TrySelectFolder("Save Enviroments", System.Environment.CurrentDirectory, out var folder))
                {
                    SaveEnviromentsToFolder(folder);
                }
            }
        }
    }

    private void SaveEnviromentsToFolder(string baseFolder)
    {
        if (string.IsNullOrEmpty(baseFolder) || enviromentStatics.Count == 0)
            return;
        var envFolder = Path.Combine(baseFolder, "enviroment");
        Directory.CreateDirectory(envFolder);
        foreach (var kv in enviromentStatics)
        {
            var path = Path.Combine(envFolder, $"{kv.Key}.json");
            var json = JsonSerializer.Serialize(kv.Value, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    private void LoadEnviromentsFromFolder(string baseFolder)
    {
        enviromentStatics.Clear();
        if (string.IsNullOrEmpty(baseFolder))
            return;
        var envFolder = Path.Combine(baseFolder, "enviroment");
        if (!Directory.Exists(envFolder))
            return;

        var jsonFiles = Directory.GetFiles(envFolder, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var jsonPath in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var data = JsonSerializer.Deserialize<EnviromentData>(json);
                if (data != null)
                {
                    var key = Path.GetFileNameWithoutExtension(jsonPath);
                    enviromentStatics[key] = data;
                }
            }
            catch { }
        }
    }

    private void EnsureTileList(EnviromentRow row, int index)
    {
        if (row.Tiles == null)
            row.Tiles = new List<ushort>(new ushort[index + 1]);
        while (row.Tiles.Count <= index)
            row.Tiles.Add(0);
    }


}
