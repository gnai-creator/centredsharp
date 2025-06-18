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
        if (generationTask != null && !generationTask.IsCompleted)
            return;

        if (tileGroups.Count == 0 || heightData == null)
        {
            _statusText = "Height data or tile groups not loaded.";
            _statusColor = UIManager.Red;
            return;
        }

        _statusText = string.Empty;
        cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        generationTask = Task.Run(async () =>
        {
            // Heavy initialization runs in the background to prevent the main
            // thread from blocking and disconnecting the client.
            EnsureTileMap(applyTransitions);

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
                Application.ClientPacketQueue.Enqueue(new ServerFlushPacket());
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
