using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.ApplicationInsights;
using System.Diagnostics;
using Microsoft.Win32;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Microsoft.ApplicationInsights.TelemetryClient TelemetryClient;
        public static Stopwatch GlobalStopwatch;
        private static string machineId = "";

        public App()
        {
            // Create Stopwatch to monitor uptime
            GlobalStopwatch = new Stopwatch();

            // Create AppInsights telemetry client to track app usage
            TelemetryClient = new Microsoft.ApplicationInsights.TelemetryClient();
            TelemetryClient.Context.User.Id = getMachineId();
            TelemetryClient.Context.Session.Id = Guid.NewGuid().ToString();

            // Handle uncaught exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private string getMachineId()
        {
            string id = null;
            try
            {
                // Try querying 64-bit registry for key
                RegistryKey localRegKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                id = (string)localRegKey.OpenSubKey(@"SOFTWARE\Microsoft\SQMClient").GetValue("MachineId");

                // If can't find key in 64-bit registry, query 32-bit registry
                if(id == null)
                {
                    localRegKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
                    id = (string)localRegKey.OpenSubKey(@"SOFTWARE\Microsoft\SQMClient").GetValue("MachineId");
                }
            }
            catch (Exception) { }

            return id.Replace("{", "").Replace("}", "");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            TelemetryClient.TrackEvent("AppStart", new Dictionary<string, string>()
            {
                { "MachineId", machineId }
            });
            GlobalStopwatch.Start();
            DriveInfo.InitializeWatcher();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TelemetryClient.TrackEvent("AppExit", new Dictionary<string, string>()
            {
                { "MachineId", machineId }
            });
            TelemetryClient.TrackMetric("UpTimeMinutes", GlobalStopwatch.Elapsed.TotalMinutes);
            GlobalStopwatch.Stop();

            DriveInfo.DisposeWatcher();

            // Disconnect from softAP and enable DHCP
            Helper.SoftApHelper.Instance.Disconnect();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                // Disconnect from softAP and enable DHCP on exception
                Helper.SoftApHelper.Instance.Disconnect();

                // Track exception
                TelemetryClient.TrackException(exception);
            }

            // Disconnect from softAP and enable DHCP on exception
            Helper.SoftApHelper.Instance.Disconnect();
        }
    }
}
