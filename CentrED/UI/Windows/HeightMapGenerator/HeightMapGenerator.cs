using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator : Window
{
    public override string Name => "HeightMap Generator";

    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    private int x1 = 0;
    private int y1 = 0;
    private int x2 = 4095;
    private int y2 = 4095;
    private int MapSizeX => x2 - x1 + 1;
    private int MapSizeY => y2 - y1 + 1;
    private const int BLOCK_SIZE = 256;
    private const int MAX_TILES = 16 * 1024 * 1024;
    private static string GroupsFile = "heightmap_groups.json";
    private static string TransitionsFile = "heightmap_transitions.json";

    private static string DragonModPath = "unknow";
    private const int NUM_CHANNELS = 6;
    private const float NOISE_SCALE = 1.25f;
    // Roughness applied to the fractal noise used when generating heights.
    // Slightly higher value makes the terrain more rugged.
    private const float NOISE_ROUGHNESS = 1.5f;
    // Increase the radius used when blending transitions between different
    // terrain types. A much larger value results in softer changes and less
    // noticeable hills.
    private const int SMOOTH_RADIUS = 12;
    private const int WATER_MAX_HEIGHT = -25;

    private string groupsPath = GroupsFile;
    private string transitionsPath = TransitionsFile;

    private string selectedDragonModPath = DragonModPath;

    private string heightMapPath = string.Empty;
    private sbyte[,]? heightData;
    private Dictionary<sbyte, (string Name, Group Group)[]> groupsByHeight;
    private Color[]? heightMapTextureData;
    private int heightMapWidth;
    private int heightMapHeight;
    // -1 significa que o mapa completo deve ser utilizado
    private int selectedQuadrant = -1;

    private readonly Perlin noise = new(Environment.TickCount);

    // Estruturas reutilizadas para os cálculos de suavização
    private int[,]? distMapCache;
    private List<(int X, int Y)> bfsFrontier = new();
    private List<(int X, int Y)> bfsNextFrontier = new();
    private readonly object bfsLock = new();

    private readonly Dictionary<string, Group> tileGroups = new();

    private readonly Dictionary<string, EnviromentData> enviromentStatics = new();

    private string selectedEnviroment = string.Empty;
    private string newEnviromentName = string.Empty;

    private string selectedGroup = string.Empty;
    private string selectedTransition = string.Empty;
    private Dictionary<string, Dictionary<string, TransitionTile>> transitionTiles = new();
    internal static readonly TransitionConverter TransitionConverter = new TransitionConverter();
    private TransitionConverter transitionConverter;

    public static string[] transitionsOrderList = new[]
                    {
                        "grass2sand",
                        "grass2forest",
                        "forest2dirt",
                        "dirt2mountain",
                    };
    internal static readonly Dictionary<ushort, TerrainType> TileIdToType = new();
    internal static string? GetAliasById(ushort id)
    {
        if (TileIdToType.TryGetValue(id, out var type))
            return HeightMapUtils.TypeAliases[type];
        return null;
    }

    private static void InitializeTileIdToType()
    {
        var landData = TileDataLoader.Instance.LandData;
        if (landData == null)
            return;

        for (ushort id = 0; id < landData.Length; id++)
        {
            string name = landData[id].Name?.ToLowerInvariant() ?? "";

            if (name.Contains("water") || name.Contains("sea") || name.Contains("ocean"))
                TileIdToType[id] = TerrainType.Water;
            else if (name.Contains("sand"))
                TileIdToType[id] = TerrainType.Sand;
            else if (name.Contains("grass"))
                TileIdToType[id] = TerrainType.Grass;
            else if (name.Contains("forest") || name.Contains("tree"))
                TileIdToType[id] = TerrainType.Forest;
            else if (name.Contains("dirt") || name.Contains("mud"))
                TileIdToType[id] = TerrainType.Dirt;
            else if (name.Contains("mountain") || name.Contains("rock") || name.Contains("cliff"))
                TileIdToType[id] = TerrainType.Mountain;
            else if (name.Contains("jungle"))
                TileIdToType[id] = TerrainType.Jungle;
            else if (name.Contains("swamp"))
                TileIdToType[id] = TerrainType.Swamp;
            else if (name.Contains("cobble") || name.Contains("stone"))
                TileIdToType[id] = TerrainType.Cobble;
            else if (name.Contains("furrows") || name.Contains("farm"))
                TileIdToType[id] = TerrainType.Furrows;
            else if (name.Contains("snow") || name.Contains("ice"))
                TileIdToType[id] = TerrainType.Snow;
            else
                TileIdToType[id] = TerrainType.None;
        }
    }


    public HeightMapGenerator()
    {
        tileMap = new Tile[0, 0]; // Initialize with an empty array to satisfy non-nullable requirement.
        transitionConverter = new TransitionConverter();

    }
}
