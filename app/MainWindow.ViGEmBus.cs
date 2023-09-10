﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MahApps.Metro.Controls.Dialogs;

using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    private void DetectViGEmBus()
    {
        //
        // Look for old ViGEmBus (v1.14.x) virtual device
        // 
        if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGemBusVersion1_14HardwareId))
        {
            ResultTile tile = new ResultTile { Title = "Deprecated ViGEmBus (pre-Gen1) Driver found" };

            tile.Clicked += ViGEmBusPreGen1OnClicked;

            ResultsPanel.Children.Add(tile);
        }

        //
        // Scan for ViGEmBus Gen1 and check version
        // 
        if (Devcon.FindByInterfaceGuid(Constants.ViGEmBusGen1InterfaceGuid, out _, out string instanceId, 0, false))
        {
            try
            {
                PnPDevice bus = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string hardwareId = bus.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds).ToList().First();
                Version driverVersion = new Version(bus.GetProperty<string>(DevicePropertyKey.Device_DriverVersion));

                if (hardwareId.Equals(Constants.ViGemBusVersion1_16HardwareId, StringComparison.OrdinalIgnoreCase)
                    && driverVersion < Constants.ViGEmBusVersionLatest)
                {
                    ResultTile tile = new ResultTile
                    {
                        Title = $"Outdated ViGEmBus (Gen1) Driver found (v{driverVersion})"
                    };

                    tile.Clicked += ViGEmBusGen1OutdatedOnClicked;

                    ResultsPanel.Children.Add(tile);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during ViGEmBus Gen1 detection");
            }
        }

        //
        // Look for HP Fork of ViGEmBus (v1.14.x) virtual device
        // 
        if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGEmBusHPForkHardwareId))
        {
            ResultTile tile = new ResultTile { Title = "HP Fork of ViGEmBus Driver found" };

            tile.Clicked += HPForkViGEmBusOnClicked;

            ResultsPanel.Children.Add(tile);
        }
    }

    private static void HPForkViGEmBusOnClicked()
    {
        Process.Start(Constants.HPForkSolutionUri);
    }

    private async void ViGEmBusGen1OutdatedOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGemBusVersion1_16HardwareId,
                out IEnumerable<string> instances);

            foreach (string instanceId in instances)
            {
                Log.Information("Processing instance {Instance}", instanceId);

                PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

                try
                {
                    controller.SetMessage("Deleting device driver");

                    Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete driver");
                }

                controller.SetMessage("Removing device");

                try
                {
                    Devcon.Remove(Constants.SystemDeviceClassGuid, instanceId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to remove device");
                }

                try
                {
                    controller.SetMessage("Deleting device driver store copies");

                    foreach (string path in DriverStore.ExistingDrivers.Where(d =>
                                 d.Contains(Constants.ViGEmBusInfName)))
                    {
                        DriverStore.RemoveDriver(path);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete driver store copy");
                }
            }
        });

        await Refresh();

        await controller.CloseAsync();
    }

    private async void ViGEmBusPreGen1OnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGemBusVersion1_14HardwareId,
                out IEnumerable<string> instances);

            foreach (string instanceId in instances)
            {
                Log.Information("Processing instance {Instance}", instanceId);

                PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

                try
                {
                    controller.SetMessage("Deleting device driver");

                    Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete driver");
                }

                controller.SetMessage("Removing device");

                try
                {
                    Devcon.Remove(Constants.SystemDeviceClassGuid, instanceId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to remove device");
                }

                try
                {
                    controller.SetMessage("Deleting device driver store copies");

                    foreach (string path in DriverStore.ExistingDrivers.Where(d =>
                                 d.Contains(Constants.ViGEmBusInfName)))
                    {
                        DriverStore.RemoveDriver(path);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete driver store copy");
                }
            }
        });

        await Refresh();

        await controller.CloseAsync();
    }
}