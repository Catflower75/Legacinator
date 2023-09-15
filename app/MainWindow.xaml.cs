﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;

using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;
#if !DEBUG
using Legacinator.Util.Web;
#endif

namespace Legacinator;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private static readonly string WinDir = Environment.GetEnvironmentVariable("WINDIR");

    private static readonly string InfDir = Path.Combine(WinDir, "INF");

    public MainWindow()
    {
        InitializeComponent();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("Legacinator.log")
            .CreateLogger();
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        Log.Logger.Information("Running with parent process: {Parent}", ParentProcessUtilities.GetParentProcess());

#if !DEBUG
        if (Updater.IsUpdateAvailable)
        {
            await this.ShowMessageAsync("Update available",
                "A newer version of the Legacinator is available, I'll now take you to the download site!");
            Process.Start(Constants.LegacinatorReleasesUri);
        }
#endif

        Log.Logger.Information("Starting complete scan");

        await Refresh();
    }

    /// <summary>
    ///     Runs all detection routines.
    /// </summary>
    private async Task Refresh()
    {
        Log.Logger.Information("Starting refresh of all component detection");

        ResultsPanel.Children.Clear();

        Log.Logger.Information("Refreshing phantom devices");

        Devcon.RefreshPhantom();

        Log.Logger.Information("Phantom device refreshing done");

        DetectHidGuardian();

        DetectScpComponents();

        DetectViGEmBus();

        DetectHidHide();

        DetectBthPS3();

        if (ResultsPanel.Children.Count == 0)
        {
            await this.ShowMessageAsync("All good",
                "Congratulations, seems like this system is free of any known problematic legacy drivers!");
        }

        Log.Logger.Information("Finished refresh of all component detection, found {Count} issues",
            ResultsPanel.Children.Count);
    }

    private void OpenGitHub(object sender, RoutedEventArgs e)
    {
        Process.Start(Constants.LegacinatorRepositoryUri);
    }

    private static CustomResultTile CreateNewTile(string title, Action onClicked, bool isCritical = false)
    {
        return new CustomResultTile(title, onClicked, isCritical
            ? PackIconForkAwesomeKind.Bomb
            : PackIconForkAwesomeKind.Info);
    }
}