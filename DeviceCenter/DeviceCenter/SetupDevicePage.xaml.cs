using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SetupDevicePage.xaml
    /// </summary>
    public partial class SetupDevicePage : Page
    {
        /// <summary>
        /// Parser for the LKG file.   
        /// TBD this is needed only for internal build.
        /// </summary>
        LastKnownGood lkg = new LastKnownGood();

        private double flashStartTime = 0;

        public SetupDevicePage()
        {
            InitializeComponent();

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ReadLkgFile();
            RefreshDriveList();
        }

        private async void ReadLkgFile()
        {
            List<LKGPlatform> entries = new List<LKGPlatform>();

            ComboBoxDeviceType.IsEnabled = false;
            ComboBoxIotBuild.IsEnabled = false;

            await Task.Run((Action)(() =>
            {
                lkg.ReadFile();

                if (lkg.lkgAllPlatforms != null &&
                    lkg.lkgAllPlatforms.AllPlatforms != null &&
                    lkg.lkgAllPlatforms.AllPlatforms.Count > 0)
                {
                    foreach (var currentPlatform in lkg.lkgAllPlatforms.AllPlatforms)
                    {
                        switch (currentPlatform.Platform)
                        {
                            case "MBM":
                                entries.Add(currentPlatform);
                                break;
                            case "RPi2":
                                entries.Add(currentPlatform);
                                break;
                        }
                    }
                }
            }));

            foreach (var currentEntry in entries)
                ComboBoxDeviceType.Items.Add(currentEntry);

            if (ComboBoxDeviceType.Items.Count == 0)
            {
                ComboBoxDeviceType.Items.Add(Strings.Strings.DeviceListNoDevices);
            }
            else
            {
                ComboBoxDeviceType.IsEnabled = true;
                ComboBoxIotBuild.IsEnabled = true;
            }

            ComboBoxDeviceType.SelectedIndex = 0;

            buttonFlash.IsEnabled = UpdateStartState();
        }

        private async void RefreshDriveList()
        {
            RemoveableDevicesComboBox.IsEnabled = false;

            List<DriveInfo> drives = null;
            await Task.Run((Action)(() =>
            {
                drives = DriveInfo.GetRemovableDriveList();
            }));

            if (drives != null)
            {
                RemoveableDevicesComboBox.Items.Clear();

                if (drives.Count == 0)
                {
                    RemoveableDevicesComboBox.Items.Add(Strings.Strings.NewDeviceInsertSDCardMessage);
                    RemoveableDevicesComboBox.IsEnabled = false;
                }
                else
                {
                    foreach (var drive in drives)
                    {
                        RemoveableDevicesComboBox.Items.Add(drive);
                        RemoveableDevicesComboBox.IsEnabled = true;
                    }
                }

                RemoveableDevicesComboBox.SelectedIndex = 0;
            }

            buttonFlash.IsEnabled = UpdateStartState();
        }

        private Process dismProcess = null;

        /// <summary>
        /// Called when user clicks Continue to flash image to SD card. 
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">not used</param>
        private void FlashSDCard_Click(object sender, RoutedEventArgs e)
        {
            lock (dismLock)
            {
                if (!UpdateStartState())
                    return;
                
                DriveInfo driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;
                Debug.Assert(driveInfo != null);

                WindowWarning dlg = new WindowWarning()
                {
                    Header = Strings.Strings.ConnectAlertTitle,
                    Message = Strings.Strings.NewDeviceAlertMessage + "\n" + Strings.Strings.NewDeviceAlertMessage2
                };

                bool? confirmation = dlg.ShowDialog();
                if (confirmation.HasValue && confirmation.Value)
                {
                    // Flash it.
                    BuildInfo build = ComboBoxIotBuild.SelectedItem as BuildInfo;
                    Debug.Assert(build != null);

                    Process dismProcess = Dism.FlashFFUImageToDrive(build.Path, driveInfo);
                    dismProcess.EnableRaisingEvents = true;

                    dismProcess.Exited += DismProcess_Exited;

                    var deviceType = ComboBoxDeviceType.SelectedItem as LKGPlatform;

                    App.TelemetryClient.TrackEvent("FlashSDCard", new Dictionary<string, string>()
                    {
                        { "DeviceType", (deviceType != null) ? deviceType.ToString() : "" },
                        { "Build",  (build != null) ? build.Build.ToString() : ""}
                    });

                    flashStartTime = App.GlobalStopwatch.ElapsedMilliseconds;
                }
            }
        }

        private object dismLock = new object();

        private void DismProcess_Exited(object sender, EventArgs e)
        {
            lock(dismLock)
            {
                // Measure how long it took to flash the image
                App.TelemetryClient.TrackMetric("FlashSDCardTimeMs", App.GlobalStopwatch.ElapsedMilliseconds - flashStartTime);

                if (dismProcess != null)
                {
                    dismProcess.Dispose();
                    dismProcess = null;
                }
            }
        }

        private void buttonCancelDism_Click(object sender, RoutedEventArgs e)
        {
            lock (dismLock)
            {
                if (dismProcess != null)
                {
                    NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_BREAK_EVENT, (uint)dismProcess.Id);

                    App.TelemetryClient.TrackEvent("FlashSDCardCancel");
                }
            }
        }

        private void ComboBoxDeviceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxIotBuild.Items.Clear();

            LKGPlatform item = ComboBoxDeviceType.SelectedItem as LKGPlatform;
            if (item != null)
            {
                foreach (var cur in item.LkgBuilds)
                    ComboBoxIotBuild.Items.Add(cur);

                if (ComboBoxIotBuild.Items.Count > 0)
                    ComboBoxIotBuild.SelectedIndex = 0;
            }

            buttonFlash.IsEnabled = UpdateStartState();
        }

        private bool UpdateStartState()
        {
            if (!RemoveableDevicesComboBox.IsEnabled || !ComboBoxDeviceType.IsEnabled)
                return false;

            if (dismProcess != null)
                return false;

            // guards for invalid data
            if (ComboBoxIotBuild.SelectedIndex < 0)
                return false;

            // guards for invalid data
            BuildInfo build = ComboBoxIotBuild.SelectedItem as BuildInfo;
            if (build == null)
                return false;

            if (RemoveableDevicesComboBox.SelectedItem == null)
                return false;

            DriveInfo driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;

            if (driveInfo == null)
                return false;

            bool? isChecked = checkBoxEula.IsChecked;

            return isChecked.HasValue && isChecked.Value;
        }

        private void ComboBoxIotBuild_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonFlash.IsEnabled = UpdateStartState();
        }

        private void RemoveableDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonFlash.IsEnabled = UpdateStartState();
        }

        private void checkBoxEula_Checked(object sender, RoutedEventArgs e)
        {
            buttonFlash.IsEnabled = UpdateStartState();
        }
    }
}
