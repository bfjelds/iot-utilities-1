﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;
using Microsoft.Win32;
using System.Deployment.Application;
using System.Globalization;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Microsoft.ApplicationInsights.TelemetryClient TelemetryClient;
        public static Stopwatch GlobalStopwatch;

        public App()
        {
            // Create Stopwatch to monitor uptime
            GlobalStopwatch = new Stopwatch();

            // Create AppInsights telemetry client to track app usage
            TelemetryClient = new Microsoft.ApplicationInsights.TelemetryClient();
            TelemetryClient.Context.User.Id = getMachineId();
            TelemetryClient.Context.Session.Id = Guid.NewGuid().ToString();

            // App is designed for Win 10.
            const int MIN_OS = 10;
            if (Environment.OSVersion.Version.Major < MIN_OS)
            {
                MessageBox.Show(
                    Strings.Strings.ErrorWindowsIncompatible,
                    LocalStrings.AppNameDisplay,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);

                Shutdown();
            }

            // Handle uncaught exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private string getMachineId()
        {
            string id = null;
            try
            {
                // Try querying 64-bit registry for key
                var localRegKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);

                if (localRegKey != null)
                {
                    id = (string)localRegKey.OpenSubKey(@"SOFTWARE\Microsoft\SQMClient").GetValue("MachineId");

                    // If can't find key in 64-bit registry, query 32-bit registry
                    if (id == null)
                    {
                        localRegKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);

                        if (localRegKey != null)
                        {
                            id = (string)localRegKey.OpenSubKey(@"SOFTWARE\Microsoft\SQMClient").GetValue("MachineId");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            if (id != null)
            {
                return id.Replace("{", "").Replace("}", "");
            }

            return null;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            TelemetryClient.TrackEvent("AppStart", new Dictionary<string, string>()
            {
                { "MachineId", getMachineId() },
                { "AppVersion", (ApplicationDeployment.IsNetworkDeployed) ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString() : "Private Build" },
                { "Language", CultureInfo.CurrentCulture.Name }
            });
            GlobalStopwatch.Start();
            DriveInfo.InitializeWatcher();
            if (e.Args != null && e.Args.Length > 0)
            {
                string ffuFilePath = "";
                try
                {
                    ffuFilePath = e.Args[0];
                    Uri uri = new Uri(ffuFilePath);
                    ffuFilePath = uri.LocalPath;
                    Application.Current.Properties["FFUFilePath"] = ffuFilePath;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to get file path from double click \n" + ex.InnerException);
                }

            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TelemetryClient.TrackEvent("AppExit", new Dictionary<string, string>()
            {
                { "MachineId", getMachineId() }
            });
            TelemetryClient.TrackMetric("UpTimeMinutes", GlobalStopwatch.Elapsed.TotalMinutes);
            GlobalStopwatch.Stop();

            DriveInfo.DisposeWatcher();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                // Track exception
                TelemetryClient.TrackException(exception);
            }
        }
    }
}
