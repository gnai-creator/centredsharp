using Xunit;
using CentrED.UI.Windows;

namespace CentrED.Tests;

public class HeightMapGeneratorPatternTests
{
    [Theory]
    [InlineData("AAAAAAAA", 4)]
    [InlineData("AAAAAAAB", 3)]
    [InlineData("AAAABBBB", 6)]
    public void PatternMapsToExpectedIndex(string pattern, int expected)
    {
        int idx = HeightMapGenerator.GetTileIndexForPattern(pattern);
        Assert.Equal(expected, idx);
    }
}
