using Xunit;
using CentrED.UI.Windows;

namespace CentrED.Tests;

public class HeightMapGeneratorPatternTests
{
    [Theory]
    [InlineData("AAAAAAAA", 4)]
    [InlineData("AAAAAAAB", 4)]
    [InlineData("AAAABBBB", 8)]
    [InlineData("ABAAAAAA", 1)]
    [InlineData("AAAABAAA", 5)]
    [InlineData("AAAAAABA", 7)]
    [InlineData("AAABAAAA", 3)]
    [InlineData("BBABAAAA", 0)]
    [InlineData("ABBABAAA", 2)]
    [InlineData("AAABABBA", 6)]
    [InlineData("AAAABABB", 8)]
    public void PatternMapsToExpectedIndex(string pattern, int expected)
    {
        int idx = HeightMapGenerator.GetTileIndexForPattern(pattern);
        Assert.Equal(expected, idx);
    }
}
