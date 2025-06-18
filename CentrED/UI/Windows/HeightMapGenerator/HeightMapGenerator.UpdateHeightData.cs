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
    private void UpdateHeightData()
    {
        if (heightMapTextureData == null)
            return;

        int quadWidth = heightMapWidth / 3;
        int quadHeight = heightMapHeight / 3;
        bool fullMap = selectedQuadrant < 0;
        int qx = fullMap ? 0 : selectedQuadrant % 3;
        int qy = fullMap ? 0 : selectedQuadrant / 3;

        heightData = new sbyte[MapSizeX, MapSizeY];

        // ---------------------------
        // 1. Criar paleta de índice
        // ---------------------------
        int[] palette = new int[256];
        {
            HashSet<int> uniques = new();
            for (int iy = 0; iy < heightMapHeight; iy++)
            {
                for (int ix = 0; ix < heightMapWidth; ix++)
                {
                    var col = heightMapTextureData[iy * heightMapWidth + ix];
                    int b = (int)MathF.Round((col.R + col.G + col.B) / 3f);
                    uniques.Add(Math.Clamp(b, 0, 255));
                }
            }
            var sorted = uniques.OrderBy(v => v).ToArray();
            if (sorted.Length == NUM_CHANNELS)
            {
                int prev = 0;
                for (int i = 0; i < sorted.Length; i++)
                {
                    int next = i < sorted.Length - 1 ? (sorted[i] + sorted[i + 1]) / 2 : 256;
                    for (int b = prev; b < next; b++)
                        palette[b] = i;
                    prev = next;
                }
            }
            else
            {
                for (int b = 0; b < 256; b++)
                    palette[b] = Math.Clamp((int)(b / (256f / NUM_CHANNELS)), 0, NUM_CHANNELS - 1);
            }
        }

        // ---------------------------
        // 2. Mapeamento de ranges dinâmico (usando tileGroups)
        // ---------------------------
        var paletteRanges = new (int Min, int Max)[NUM_CHANNELS];
        {
            foreach (var group in tileGroups.Values)
            {
                int idx = group.ChannelIndex;
                if (idx >= 0 && idx < NUM_CHANNELS)
                    paletteRanges[idx] = (group.MinHeight, group.MaxHeight);
            }
        }

        int[,] idxMap = new int[MapSizeX, MapSizeY];
        Parallel.For(0, MapSizeY, y =>
        {
            int sy = fullMap
                ? (MapSizeY == 1 ? 0 : y * (heightMapHeight - 1) / (MapSizeY - 1))
                : qy * quadHeight +
                  (MapSizeY == 1 ? 0 : y * (quadHeight - 1) / (MapSizeY - 1));

            for (int x = 0; x < MapSizeX; x++)
            {
                int sx = fullMap
                    ? (MapSizeX == 1 ? 0 : x * (heightMapWidth - 1) / (MapSizeX - 1))
                    : qx * quadWidth +
                      (MapSizeX == 1 ? 0 : x * (quadWidth - 1) / (MapSizeX - 1));

                var c = heightMapTextureData[sy * heightMapWidth + sx];
                int brightness = (int)MathF.Round((c.R + c.G + c.B) / 3f);
                idxMap[x, y] = palette[Math.Clamp(brightness, 0, 255)];
            }
        });

        // ---------------------------
        // 3. Gerar altura base sem suavização
        // ---------------------------
        Parallel.For(0, MapSizeY, y =>
        {
            for (int x = 0; x < MapSizeX; x++)
            {
                int idx = idxMap[x, y];
                var range = paletteRanges[idx];
                int z;

                bool isEdge = false;
                for (int dy = -1; dy <= 1 && !isEdge; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= MapSizeY) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        if (nx < 0 || nx >= MapSizeX) continue;
                        if (idxMap[nx, ny] != idx)
                        {
                            isEdge = true;
                            break;
                        }
                    }
                }

                if (idx == 0)
                {
                    z = -127;
                }
                else
                {
                    float n = noise.Fractal((x1 + x) * NOISE_SCALE, (y1 + y) * NOISE_SCALE, NOISE_ROUGHNESS);
                    float t = (n + 1f) * 0.5f;
                    z = (int)MathF.Round(range.Min + t * (range.Max - range.Min));
                    if (isEdge)
                    {
                        float edgePerturb = noise.Noise((x1 + x) * 0.3f, (y1 + y) * 0.3f);
                        z += (int)(edgePerturb * 3);
                    }
                }

                heightData[x, y] = (sbyte)Math.Clamp(z, -127, 127);
            }
        });

        // ---------------------------
        // 4. Suavização entre biomas
        // ---------------------------
        for (int src = 0; src < NUM_CHANNELS - 2; src++)
        {
            ComputeDistanceMap(idxMap, src);

            Parallel.For(0, MapSizeY, y =>
            {
                for (int x = 0; x < MapSizeX; x++)
                {

                    var smooth = SMOOTH_RADIUS;
                    // Se o índice não for o esperado, pula para a próxima iteração
                    if (src == 5)
                        smooth = 0; // Para o último canal, reduz o raio de suavização
                    else
                        smooth = SMOOTH_RADIUS;
                    if (idxMap[x, y] != src + 1) continue;
                    int dist = distMapCache![x, y];

                    if (dist > smooth) continue;



                    int z;
                    if (dist <= 1)
                        z = paletteRanges[src + 1].Min;

                    else
                    {
                        float lerpT = (dist - 1) / (float)(smooth - 1);
                        z = (int)MathF.Round(MathHelper.Lerp(paletteRanges[src].Max, heightData![x, y], lerpT));
                    }
                    heightData![x, y] = (sbyte)Math.Clamp(z, -127, 127);
                }
            });
        }

        for (int src = NUM_CHANNELS - 2; src > 0; src--)
        {
            ComputeDistanceMap(idxMap, src);

            Parallel.For(0, MapSizeY, y =>
            {
                for (int x = 0; x < MapSizeX; x++)
                {

                    var smooth = SMOOTH_RADIUS;

                    if (idxMap[x, y] != src - 1) continue;
                    int dist = distMapCache![x, y];
                    if (dist > smooth) continue;

                    int z;
                    if (dist <= 1)
                        z = paletteRanges[src - 1].Max;
                    else
                    {
                        float lerpT = (dist - 1) / (float)(smooth - 1);
                        z = (int)MathF.Round(MathHelper.Lerp(paletteRanges[src].Min, heightData![x, y], lerpT));
                    }
                    heightData![x, y] = (sbyte)Math.Clamp(z, -127, 127);
                }
            });
        }

        SmoothTransitions(idxMap);


        // ---------------------------
        // 5. Aplicar altura mínima para células sem bioma  
        //    e substituir água com altura maior que o máximo da água    
        // ---------------------------    
        for (int y = 0; y < MapSizeY; y++)
        {
            for (int x = 0; x < MapSizeX; x++)
            {


                if (idxMap[x, y] == 0)
                    heightData[x, y] = WATER_MAX_HEIGHT;

            }
        }

        // EnsureTileMap();

    }


    /// <summary>
    /// Apply a simple averaging filter on cells located at terrain boundaries
    /// to smooth transitions in all directions.
    /// </summary>
    private void SmoothTransitions(int[,] idxMap)
    {
        if (heightData == null)
            return;

        var original = (sbyte[,])heightData.Clone();
        Parallel.For(0, MapSizeY, y =>
        {
            for (int x = 0; x < MapSizeX; x++)
            {

                bool isBoundary = false;
                for (int dy = -1; dy <= 1 && !isBoundary; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= MapSizeY) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        if (nx < 0 || nx >= MapSizeX) continue;
                        if (idxMap[nx, ny] != idxMap[x, y])
                        {
                            isBoundary = true;
                            break;
                        }
                    }
                }

                if (!isBoundary)
                    continue;

                int sum = 0;
                int count = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= MapSizeY) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        if (nx < 0 || nx >= MapSizeX) continue;
                        sum += original[nx, ny];
                        count++;
                    }
                }

                int avg = (int)MathF.Round(sum / (float)count);
                heightData[x, y] = (sbyte)Math.Clamp(avg, -127, 127);
            }
        });
    }

    private void ComputeDistanceMap(int[,] idxMap, int src)
    {
        if (distMapCache == null || distMapCache.GetLength(0) != MapSizeX || distMapCache.GetLength(1) != MapSizeY)
            distMapCache = new int[MapSizeX, MapSizeY];

        bfsFrontier.Clear();
        bfsNextFrontier.Clear();

        for (int y = 0; y < MapSizeY; y++)
        {
            for (int x = 0; x < MapSizeX; x++)
            {
                if (idxMap[x, y] == src)
                {
                    distMapCache[x, y] = 0;
                    bfsFrontier.Add((x, y));
                }
                else
                {
                    distMapCache[x, y] = int.MaxValue;
                }
            }
        }

        while (bfsFrontier.Count > 0)
        {
            bfsNextFrontier.Clear();
            Parallel.For(0, bfsFrontier.Count, i =>
            {
                var (cx, cy) = bfsFrontier[i];
                int nd = distMapCache[cx, cy] + 1;
                if (nd > SMOOTH_RADIUS)
                    return;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = cy + dy;
                    if (ny < 0 || ny >= MapSizeY) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        if (nx < 0 || nx >= MapSizeX) continue;
                        if (nd < distMapCache[nx, ny])
                        {
                            distMapCache[nx, ny] = nd;
                            lock (bfsLock)
                                bfsNextFrontier.Add((nx, ny));
                        }
                    }
                }
            });
            var tmp = bfsFrontier;
            bfsFrontier = bfsNextFrontier;
            bfsNextFrontier = tmp;
        }
    }

}
