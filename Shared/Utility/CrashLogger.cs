using System;
using System.IO;
using System.Threading.Tasks;

namespace CentrED.Utility;

public static class CrashLogger
{
    private const string CrashLogFile = "Crash.log";

    public static void Init()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogException(ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception);
    }

    private static void LogException(Exception e)
    {
        try
        {
            File.AppendAllText(CrashLogFile, $"[{DateTime.Now}] {e}{Environment.NewLine}");
        }
        catch
        {
            // ignored
        }
    }

    public static void Log(Exception e)
    {
        LogException(e);
    }
}
