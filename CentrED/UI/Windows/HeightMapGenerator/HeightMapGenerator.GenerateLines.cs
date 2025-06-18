using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static CentrED.Application;
using CentrED.Client;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private async Task GenerateLines(Dictionary<string, Group> groups, CancellationToken ct)
    {

        if (MapSizeX <= 0 || MapSizeY <= 0)
        {
            _statusText = "Invalid map size.";
            _statusColor = UIManager.Red;
            return;
        }

        var groupsByHeight = BuildGroupsByHeightWithNames(groups);
        var defaultCandidates = groups.Select(kv => (kv.Key, kv.Value)).ToArray();

        // Send rows in chunks of 8 tiles to avoid gaps between lines
        // when applying the large scale operations. Sending a single row
        // at a time caused the server to apply the changes only to every
        // eighth row, leaving black gaps between generated lines.
        for (int y = y1; y <= y2 && !ct.IsCancellationRequested;)
        {
            var remaining = y2 - y + 1;
            var height = Math.Min(8, remaining);
            await GenerateArea(x1, y, MapSizeX, height, groupsByHeight, defaultCandidates, ct);
            y += height;
        }

        // Garante que todos os dados sejam enviados
        Application.ClientPacketQueue.Enqueue(new ServerFlushPacket());
    }
}
