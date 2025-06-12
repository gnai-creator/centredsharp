using Xunit;
using CentrED.UI.Windows;

namespace CentrED.Tests;

public class HeightMapGeneratorPatternTests
{
    [Theory]
    [InlineData("AAAAAAAA", 4)]
    [InlineData("AAAAAAAB", 4)]
    [InlineData("AAAAAABA", 4)]
    [InlineData("AAAAAABB", 3)]
    [InlineData("AAAAABAA", 4)]
    [InlineData("AAAAABAB", 6)]
    [InlineData("AAAAABBA", 7)]
    [InlineData("AAAAABBB", 6)]
    [InlineData("AAAABAAA", 4)]
    [InlineData("AAAABAAB", 6)]
    [InlineData("AAAABABA", 4)]
    [InlineData("AAAABBAA", 7)]
    [InlineData("AAAABBBB", 7)]

    public void PatternMapsToExpectedIndex(string pattern, int expected)
    {
        int idx = HeightMapGenerator.GetTileIndexForPattern(pattern);
        Assert.Equal(expected, idx);
    }
}
