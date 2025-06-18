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
    private static Group SelectGroup(ReadOnlySpan<Group> groups, int x, int y)
    {
        float total = 0f;
        foreach (var g in groups)
            total += g.Chance;
        var val = DeterministicDouble(x, y) * total;
        float acc = 0f;
        foreach (var g in groups)
        {
            acc += g.Chance;
            if (val <= acc)
                return g;
        }
        return groups[0];
    }

    private static (string Name, Group Group) SelectGroup(ReadOnlySpan<(string Name, Group Group)> groups, int x, int y)
    {
        float total = 0f;
        foreach (var g in groups)
            total += g.Group.Chance;
        var val = DeterministicDouble(x, y) * total;
        float acc = 0f;
        foreach (var g in groups)
        {
            acc += g.Group.Chance;
            if (val <= acc)
                return g;
        }
        return groups[0];
    }
}
