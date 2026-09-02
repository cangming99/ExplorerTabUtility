using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using ExplorerTabUtility.UI.Views;
using ExplorerTabUtility.Helpers;
using ExplorerTabUtility.Managers;

namespace ExplorerTabUtility;

// ReSharper disable once RedundantExtendsListEntry
public partial class App : Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Apply the persisted language before anything is shown, so the very first frame
            // (including the "already running" prompt) is localized right away.
            LocalizationManager.Initialize();

            _mutex = new Mutex(true, Constants.MutexId, out var createdNew);

            if (createdNew)
            {
                base.OnStartup(e);
                SetupTooltipBehavior();

                _ = new MainWindow();
                return;
            }

            CustomMessageBox.Show(
                LocalizationManager.GetString("App.AnotherInstanceRunning"),
                Constants.AppName,
                icon: MessageBoxImage.Information);
            Shutdown();
        }
        catch (Exception ex)
        {
            // Startup failures are otherwise silent; keep a crash log so runtime XAML/binding
            // errors (which the compiler cannot catch) are diagnosable without the event log.
            LogCrash(ex);
            throw;
        }
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Constants.AppName);
            Directory.CreateDirectory(directory);

            File.AppendAllText(
                Path.Combine(directory, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
        }
        catch
        {
            // Never mask the original exception.
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private static void SetupTooltipBehavior()
    {
        ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(3500));
        ToolTipService.InitialShowDelayProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(1700));
        ToolTipService.BetweenShowDelayProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(150));
        ToolTipService.ShowsToolTipOnKeyboardFocusProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(false));
    }
}