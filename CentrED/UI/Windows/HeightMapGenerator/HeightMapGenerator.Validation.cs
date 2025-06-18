namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    /// <summary>
    /// Ensures that height data and tile groups are loaded before running
    /// generation routines.
    /// </summary>
    /// <returns><c>true</c> if both resources are loaded; otherwise <c>false</c>.</returns>
    private bool ValidateLoadedData()
    {
        if (tileGroups.Count == 0 || heightData == null)
        {
            _statusText = "Height data or tile groups not loaded.";
            _statusColor = UIManager.Red;
            return false;
        }
        return true;
    }
}
