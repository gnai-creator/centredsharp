using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private string newGroupName = string.Empty;

    // Fields required for UI state
    private int _dragTransitionIndex = -1;
    private string TransitionOrderDragType = "TransitionOrderDragType";
    private string newTransitionName = string.Empty;
    private Task? generationTask;

    protected override void InternalDraw()
    {
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        if (ImGui.BeginTabBar("HeightMapTabs"))
        {
            try
            {
                if (ImGui.BeginTabItem("Editor"))
                {

                    ImGui.Text("Tile Groups");
                    DrawGroups(tileGroups, ref selectedGroup, ref newGroupName);
                    if (ImGui.Button("Save Groups"))
                    {
                        if (TinyFileDialogs.TrySaveFile("Save Groups", groupsPath, new[] { "*.json" }, "JSON Files", out var path))
                        {
                            SaveGroups(path);
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Load Groups"))
                    {
                        if (TinyFileDialogs.TryOpenFile("Load Groups", Environment.CurrentDirectory, new[] { "*.json" }, "JSON Files", false, out var path))
                        {
                            LoadGroups(path);
                            _statusText = $"Grupos carregados de {path}";
                        }
                    }
                    ImGui.Separator();
                    ImGui.Text("Transitions Order");
                    var itemHeight = ImGui.GetTextLineHeightWithSpacing();
                    bool childVisible = ImGui.BeginChild("TransitionOrderChild", new System.Numerics.Vector2(0, itemHeight * transitionsOrderList.Length + 4), ImGuiChildFlags.Borders);
                    try
                    {
                        if (childVisible)
                        {
                            for (int i = 0; i < transitionsOrderList.Length; i++)
                            {
                                ImGui.PushID(i);
                                var key = transitionsOrderList[i];
                                ImGui.Selectable(key, false);
                                if (ImGui.BeginDragDropSource())
                                {
                                    _dragTransitionIndex = i;
                                    unsafe
                                    {
                                        int index = i;
                                        ImGui.SetDragDropPayload(TransitionOrderDragType, (IntPtr)(&index), sizeof(int));
                                    }
                                    ImGui.Text(key);
                                    ImGui.EndDragDropSource();
                                }
                                if (ImGui.BeginDragDropTarget())
                                {
                                    var payloadPtr = ImGui.AcceptDragDropPayload(TransitionOrderDragType);
                                    unsafe
                                    {
                                        if (payloadPtr.NativePtr != null)
                                        {
                                            int* dataPtr = (int*)payloadPtr.Data;
                                            int from = dataPtr[0];
                                            int to = i;
                                            if (from != to)
                                            {
                                                var list = transitionsOrderList.ToList();
                                                var moved = list[from];
                                                list.RemoveAt(from);
                                                if (from < to) to--;
                                                list.Insert(to, moved);
                                                transitionsOrderList = list.ToArray();
                                            }
                                            _dragTransitionIndex = -1;
                                        }
                                    }
                                    ImGui.EndDragDropTarget();
                                }
                                ImGui.PopID();
                            }
                        }
                    }
                    finally
                    {
                        ImGui.EndChild();
                    }
                    ImGui.InputText("##newtransition", ref newTransitionName, 32);
                    ImGui.SameLine();
                    if (ImGui.Button("Add##transition"))
                    {
                        if (!string.IsNullOrWhiteSpace(newTransitionName))
                        {
                            var list = transitionsOrderList.ToList();
                            list.Add(newTransitionName);
                            transitionsOrderList = list.ToArray();
                            newTransitionName = string.Empty;
                        }
                    }
                    if (ImGui.Button("Reset Order"))
                    {
                        transitionsOrderList = new[]
                        {
                            "grass2sand",
                            "grass2forest",
                            "forest2dirt",
                            "dirt2mountain"
                        };
                    }
                    ImGui.Separator();
                    if (ImGui.Button("Load Heightmap"))
                    {
                        if (TinyFileDialogs.TryOpenFile("Select Heightmap", Environment.CurrentDirectory, new[] { "*.png" }, "PNG Files", false, out var path))
                        {
                            LoadHeightmap(path);
                        }
                    }
                    if (!string.IsNullOrEmpty(heightMapPath))
                    {
                        ImGui.Text($"Loaded: {Path.GetFileName(heightMapPath)}");
                    }
                    ImGui.Text("Area");
                    int prevX1 = x1;
                    int prevX2 = x2;
                    int prevY1 = y1;
                    int prevY2 = y2;
                    ImGui.InputInt("X1", ref x1);
                    ImGui.InputInt("X2", ref x2);
                    ImGui.InputInt("Y1", ref y1);
                    ImGui.InputInt("Y2", ref y2);
                    if (x1 == prevX1 || x2 == prevX2 || y1 == prevY1 || y2 == prevY2)
                    {
                        // UpdateHeightData();
                    }
                    ImGui.Text("Quadrant");
                    for (int qy = 0; qy < 3; qy++)
                    {
                        for (int qx = 0; qx < 3; qx++)
                        {
                            int idx = qy * 3 + qx;
                            bool check = selectedQuadrant == idx;
                            if (ImGui.Checkbox($"##q_{qy}_{qx}", ref check))
                            {
                                if (check)
                                {
                                    selectedQuadrant = idx;
                                    //UpdateHeightData();
                                }
                            }
                            if (qx < 2) ImGui.SameLine();
                        }
                    }
                    bool fullMap = selectedQuadrant == -1;
                    if (ImGui.Checkbox("Full Map", ref fullMap))
                    {
                        if (fullMap)
                        {
                            selectedQuadrant = -1;
                        }
                        else
                        {
                            selectedQuadrant = -2; // Use -2 to indicate "no quadrant selected"
                        }
                        //UpdateHeightData();
                    }
                    if (selectedQuadrant == -2)
                    {
                        ImGui.TextColored(UIManager.Red, "No quadrant selected!");
                    }

                    ImGui.Separator();

                    ImGui.Separator();
                    ImGui.Text("Generate Heightmap");
                    if (ImGui.Button("Generate"))
                    {
                        // Generate(false);
                        Generate(true);
                    }
                    ImGui.SameLine();
                    ImGui.TextColored(UIManager.Red, "<== This operation cannot be undone!");

                    ImGui.Separator();

                    if (!string.IsNullOrEmpty(_statusText))
                    {
                        ImGui.TextColored(_statusColor, _statusText);
                    }

                    ImGui.Separator();
                    if (ImGui.Button("Load DragonMod JSON'S"))
                    {
                        if (TinyFileDialogs.TrySelectFolder("Escolha a pasta DragonMod", Environment.CurrentDirectory, out var baseFolder))
                        {
                            LoadDragonModJsonsFromFolder(baseFolder);
                            // Cria um grupo para cada tipo A (parte antes do "2")
                            foreach (var key in transitionTiles.Keys)
                            {
                                var idx = key.IndexOf("2", StringComparison.Ordinal);
                                var typeA = idx > 0 ? key.Substring(0, idx) : key;
                                if (!tileGroups.ContainsKey(typeA))
                                    tileGroups.Add(typeA, new Group(typeA));
                            }
                            _statusText = $"JSONs carregados de {baseFolder}";
                            _statusColor = UIManager.Green;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Save JSON'S"))
                    {
                        if (TinyFileDialogs.TrySelectFolder("Salvar todos os JSON DragonMod em...", Environment.CurrentDirectory, out var saveFolder))
                        {
                            if (_dragonModJsonFiles.Count == 0 && _dragonModStaticsJsonFiles.Count == 0)
                            {
                                _statusText = "Nenhum JSON carregado para salvar.";
                                _statusColor = UIManager.Red;
                            }
                            else
                            {
                                SaveDragonModConvertedFilesToFolder(saveFolder);
                                _statusText = $"Dados salvos em {saveFolder}";
                                _statusColor = UIManager.Green;
                            }
                        }
                    }
                    DrawDragonModJsonList();

                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("DragonMod"))
                {
                    if (ImGui.Button("Load DragonMod Files"))
                    {
                        if (TinyFileDialogs.TrySelectFolder("Choose DragonMod Root Folder", Environment.CurrentDirectory, out var folderPath))
                        {
                            selectedDragonModPath = folderPath;
                            LoadDragonModFiles(selectedDragonModPath);
                            _statusColor = UIManager.Green;
                        }
                    }
                    ImGui.Text($"Selected folder: {selectedDragonModPath}");
                    ImGui.Separator();
                    if (ImGui.Button("Load DragonMod JSON'S"))
                    {
                        if (TinyFileDialogs.TrySelectFolder("Escolha a pasta DragonMod", Environment.CurrentDirectory, out var baseFolder))
                        {
                            LoadDragonModJsonsFromFolder(baseFolder);
                            _statusText = $"JSONs carregados de {baseFolder}";
                            _statusColor = UIManager.Green;
                        }
                    }
                    // Não exibe a lista para evitar conflitos entre mapas e statics
                    ImGui.Separator();
                    if (ImGui.Button("Convert Files to Json"))
                    {
                        if (!string.IsNullOrEmpty(selectedDragonModPath))
                        {
                            ConvertDragonModTxtToJson(selectedDragonModPath);
                            _statusText = $"Files converted from folder {selectedDragonModPath}";
                            _statusColor = UIManager.Green;
                        }
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Statics"))
                {
                    if (ImGui.Button("Load Statics JSON'S"))
                    {
                        if (TinyFileDialogs.TrySelectFolder("Escolha a pasta DragonMod", Environment.CurrentDirectory, out var baseFolder))
                        {
                            LoadDragonModStaticsJsonsFromFolder(baseFolder);
                            _statusText = $"Statics carregados de {baseFolder}";
                            _statusColor = UIManager.Green;
                        }
                    }
                    DrawDragonModStaticsJsonList();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Enviroment"))
                {
                    DrawEnviromentTab();
                    ImGui.EndTabItem();
                }
            }
            finally
            {
                ImGui.EndTabBar();
            }
        }
    }


}
