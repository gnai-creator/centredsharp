using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using CentrED.Utility;
using CentrED.UI.Windows;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private CancellationTokenSource? cancellationSource;
    private bool refreshMapPending = false;

    private void Generate(bool applyTransitions = false)
    {

        // UpdateHeightData();
        EnsureTileMap(applyTransitions);


        _statusText = string.Empty;
        cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        generationTask = Task.Run(async () =>
        {
            var total = MapSizeX * MapSizeY;
            if (total > MAX_TILES)
                return;

            try
            {
                await GenerateLines(tileGroups
                    .Where(kv => kv.Value.Ids.Count > 0)
                    .ToDictionary(kv => kv.Key, kv => kv.Value), token);
            }
            finally
            {
                CEDClient.Flush();
            }

            if (token.IsCancellationRequested)
            {
                _statusText = "Generation cancelled.";
                _statusColor = UIManager.Red;
            }
            else
            {
                refreshMapPending = true;
            }
        }, token);
    }

}
