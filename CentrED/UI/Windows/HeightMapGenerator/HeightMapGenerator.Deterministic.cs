namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    internal static uint DeterministicHash(int x, int y)
    {
        unchecked
        {
            return (uint)((x * 73856093) ^ (y * 19349663));
        }
    }

    internal static int DeterministicIndex(int x, int y, int count)
    {
        if (count <= 0)
            return 0;
        return (int)(DeterministicHash(x, y) % (uint)count);
    }

    internal static double DeterministicDouble(int x, int y)
    {
        return DeterministicHash(x, y) / (double)uint.MaxValue;
    }
}
