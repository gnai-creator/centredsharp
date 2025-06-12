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
    private void LoadHeightmap(string path)
    {
        if (generationTask != null && !generationTask.IsCompleted)
            return;
        try
        {
            using var fs = File.OpenRead(path);
            var tex = Texture2D.FromStream(CEDGame.GraphicsDevice, fs);
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);

            heightMapTextureData = data;
            heightMapWidth = tex.Width;
            heightMapHeight = tex.Height;

            UpdateHeightData();
            heightMapPath = path;
            _statusText = $"Loaded heightmap {Path.GetFileName(path)}";
            _statusColor = UIManager.Green;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load heightmap: {e.Message}");
            heightData = null;
            heightMapPath = string.Empty;
            _statusText = $"Failed to load heightmap: {e.Message}";
            _statusColor = UIManager.Red;
        }
    }
}
