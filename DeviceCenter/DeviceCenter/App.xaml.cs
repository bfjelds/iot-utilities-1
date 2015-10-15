using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.ApplicationInsights;
using System.Diagnostics;

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

            // Handle uncaught exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            TelemetryClient.TrackEvent("AppStart");
            GlobalStopwatch.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TelemetryClient.TrackEvent("AppExit");
            TelemetryClient.TrackMetric("UpTimeMinutes", GlobalStopwatch.Elapsed.TotalMinutes);
            GlobalStopwatch.Stop();
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
