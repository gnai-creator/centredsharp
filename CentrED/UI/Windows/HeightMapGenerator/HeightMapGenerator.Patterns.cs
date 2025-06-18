using System;
using System.Collections.Generic;
using System.Linq;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private static string CreatePattern(int value)
    {
        Span<char> chars = stackalloc char[8];
        for (int bit = 0; bit < 8; bit++)
        {
            chars[7 - bit] = ((value >> bit) & 1) != 0 ? 'B' : 'A';
        }
        return new string(chars);
    }

    private static readonly string[] AllPatterns = Enumerable.Range(0, 256)
        .Select(CreatePattern)
        .ToArray();

    private static void EnsureAllPatterns(List<DragonModTransition> list)
    {
        if (list.Count >= 256)
            return;
        var existing = new HashSet<string>(list.Select(t => t.pattern));
        foreach (var pattern in AllPatterns)
        {
            if (!existing.Contains(pattern))
                list.Add(new DragonModTransition { pattern = pattern, tiles = new List<uint>() });
        }
    }
}
