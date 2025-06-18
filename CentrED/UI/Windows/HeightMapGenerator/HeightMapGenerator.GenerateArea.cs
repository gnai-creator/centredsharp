using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CentrED.Utility;
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

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{

    private async Task GenerateArea(
        int startX,
        int startY,
        int width,
        int height,
        Dictionary<sbyte, (string Name, Group Group)[]> groupsByHeight,
        (string Name, Group Group)[] defaultCandidates,
        CancellationToken ct)
    {


        int endX = startX + width - 1;
        int endY = startY + height - 1;

        var altitudes = new sbyte[width * height];
        var ids = new ushort[width * height];
        var groupNames = new string[width, height];
        int idx = 0;

        int startBlockX = startX / 8;
        int endBlockX = endX / 8;
        int startBlockY = startY / 8;
        int endBlockY = endY / 8;

        var envAreas = enviromentStatics.Keys.ToDictionary(k => k, _ => new List<AreaInfo>());

        for (int bx = startBlockX; bx <= endBlockX && !ct.IsCancellationRequested; bx++)
        {
            for (int by = startBlockY; by <= endBlockY && !ct.IsCancellationRequested; by++)
            {
                for (int ty = 0; ty < 8 && !ct.IsCancellationRequested; ty++)
                {
                    int y = by * 8 + ty;
                    if (y < startY || y > endY)
                        continue;

                    for (int tx = 0; tx < 8 && !ct.IsCancellationRequested; tx++)
                    {
                        int x = bx * 8 + tx;
                        if (x < startX || x > endX)
                            continue;

                        if (heightData == null)
                            continue;

                        var z = heightData[x - x1, y - y1];
                        altitudes[idx] = z;

                        ushort id;
                        string gname;
                        if (tileMap != null)
                        {
                            var tile = tileMap[x - x1, y - y1];
                            id = tile.Id;
                            gname = tile.GroupName;
                        }
                        else
                        {
                            if (!groupsByHeight.TryGetValue(z, out var candidates) || candidates.Length == 0)
                                candidates = defaultCandidates;
                            if (candidates.Length > 0)
                            {
                                var grpInfo = SelectGroup(candidates, x, y);
                                var grp = grpInfo.Group;
                                gname = grpInfo.Name;
                                id = grp.Ids[DeterministicIndex(x, y, grp.Ids.Count)];
                            }
                            else
                            {
                                id = 0;
                                gname = string.Empty;
                            }
                        }

                        ids[idx] = id;
                        groupNames[x - startX, y - startY] = gname;
                        idx++;
                    }
                }
            }
        }

        // Agrupa tiles por bloco de 8x8 para reduzir o numero de areas
        for (int by = 0; by < height; by += 8)
        {
            for (int bx = 0; bx < width; bx += 8)
            {
                int localStartX = bx;
                int localStartY = by;
                int localEndX = Math.Min(bx + 7, width - 1);
                int localEndY = Math.Min(by + 7, height - 1);
                var firstName = groupNames[localStartX, localStartY];
                bool uniform = true;
                for (int ty = localStartY; ty <= localEndY && uniform; ty++)
                {
                    for (int tx = localStartX; tx <= localEndX; tx++)
                    {
                        if (groupNames[tx, ty] != firstName)
                        {
                            uniform = false;
                            break;
                        }
                    }
                }
                if (uniform && !string.IsNullOrEmpty(firstName) && envAreas.TryGetValue(firstName, out var ulist))
                {
                    ulist.Add(new AreaInfo(
                        (ushort)(startX + localStartX),
                        (ushort)(startY + localStartY),
                        (ushort)(startX + localEndX),
                        (ushort)(startY + localEndY)));
                }
                else
                {
                    for (int ty = localStartY; ty <= localEndY; ty++)
                    {
                        for (int tx = localStartX; tx <= localEndX; tx++)
                        {
                            var gname = groupNames[tx, ty];
                            if (string.IsNullOrEmpty(gname))
                                continue;
                            if (envAreas.TryGetValue(gname, out var list))
                            {
                                list.Add(new AreaInfo(
                                    (ushort)(startX + tx),
                                    (ushort)(startY + ty),
                                    (ushort)(startX + tx),
                                    (ushort)(startY + ty)));
                            }
                        }
                    }
                }
            }
        }


        var area = new AreaInfo((ushort)startX, (ushort)startY, (ushort)endX, (ushort)endY);

        // Calcula altitude mínima e máxima da área
        var minAltitude = altitudes.Min();
        var maxAltitude = altitudes.Max();

        // Envia o mapa de altitude
        Application.ClientPacketQueue.Enqueue(new LargeScaleOperationPacket([area], new LSOSetAltitude(minAltitude, maxAltitude)));

        // Envia o mapa de land tiles
        Application.ClientPacketQueue.Enqueue(new LargeScaleOperationPacket([area], new LSODrawLand(ids)));

        // Envia statics em lotes pequenos
        foreach (var env in enviromentStatics)
        {
            if (!envAreas.TryGetValue(env.Key, out var areas) || areas.Count == 0)
                continue;

            foreach (var row in env.Value.Rows)
            {
                if (row.Tiles.Count == 0)
                    continue;

                var chance = (byte)Math.Clamp((int)Math.Round(row.Chance / 100f * 255f), 0, 255);
                if (chance == 0)
                    continue;

                var envIds = row.Tiles
                    .Where(id => id > 0)
                    .Select(id => (ushort)(id + 0x4000))
                    .ToArray();

                if (envIds.Length == 0)
                    continue;

                // Lotes pequenos - 25 áreas por vez
                for (int i = 0; i < areas.Count; i += 25)
                {
                    if (!CEDClient.Running) // Verifica se ainda está conectado
                        return;

                    var chunk = areas.Skip(i).Take(25).ToArray();
                    Application.ClientPacketQueue.Enqueue(new LargeScaleOperationPacket(chunk, new LSOAddStatics(envIds, chance, LSO.StaticsPlacement.Top, 0)));
                }
            }
        }
    }
}