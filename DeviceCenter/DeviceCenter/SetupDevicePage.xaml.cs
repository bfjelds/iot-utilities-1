using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Management;
using System.Windows.Controls;
using System.IO;
using System.ComponentModel;
using System.Windows.Threading;

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
        readonly LastKnownGood _lkg = new LastKnownGood();

        private double _flashStartTime = 0;
        private LKGPlatform _cachedDeviceType;
        private DriveInfo _cachedDriveInfo;
        private BuildInfo _cachedBuildInfo;

        private EventArrivedEventHandler _usbhandler = null;
        private Frame navigationFrame;

        public SetupDevicePage(Frame navigationFrame)
        {
            InitializeComponent();
            this.navigationFrame = navigationFrame;

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ReadLkgFile();
            await RefreshDriveList();
            this._usbhandler = new EventArrivedEventHandler(UsbAddedorRemoved);
            DriveInfo.AddUSBDetectionHandler(_usbhandler);
        }

        private async void ReadLkgFile()
        {
            var entries = new List<LKGPlatform>();

            ComboBoxDeviceType.IsEnabled = false;
            ComboBoxIotBuild.IsEnabled = false;

            await Task.Run((Action)(() =>
            {
                _lkg.ReadFile();

                if (_lkg.LkgAllPlatforms != null &&
                    _lkg.LkgAllPlatforms.AllPlatforms != null &&
                    _lkg.LkgAllPlatforms.AllPlatforms.Count > 0)
                {
                    foreach (var currentPlatform in _lkg.LkgAllPlatforms.AllPlatforms)
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
                // TBD - this list should be hardcoded to MBM and RPi2.  
                ComboBoxDeviceType.Items.Add("LKG not found \\\\webnas\\AthensDrop");
            }
            else
            {
                ComboBoxDeviceType.IsEnabled = true;
                ComboBoxIotBuild.IsEnabled = true;
            }

            ComboBoxDeviceType.SelectedIndex = 0;

            buttonFlash.IsEnabled = UpdateStartState();
        }      


        private async Task RefreshDriveList()
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

        public async void UsbAddedorRemoved(object sender, EventArgs e)
        {            
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () => 
            {
                await RefreshDriveList();
            }));
        }

        private Process _dismProcess = null;

        /// <summary>
        /// Called when user clicks Continue to flash image to SD card. 
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">not used</param>
        private void FlashSDCard_Click(object sender, RoutedEventArgs e)
        {
            lock (_dismLock)
            {
                if (!UpdateStartState())
                    return;
                
                var driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;
                Debug.Assert(driveInfo != null);

                var dlg = new WindowWarning()
                {
                    Header = Strings.Strings.ConnectAlertTitle,
                    Message = Strings.Strings.NewDeviceAlertMessage + "\n" + Strings.Strings.NewDeviceAlertMessage2
                };

                var confirmation = dlg.ShowDialog();

                if (confirmation.HasValue && confirmation.Value)
                {
                    // Flash it.
                    var build = ComboBoxIotBuild.SelectedItem as BuildInfo;
                    Debug.Assert(build != null);

                    try
                    {
                        _dismProcess = Dism.FlashFfuImageToDrive(build.Path, driveInfo);
                        _dismProcess.EnableRaisingEvents = true;
                        _dismProcess.Exited += DismProcess_Exited;
                    }
                    catch (Exception ex)
                    {   
                        Debug.WriteLine(ex.ToString());

                        var exception = ex as FileNotFoundException;

                        if (exception != null)
                        {
                            // the app name as caption
                            var errorCaption = Strings.Strings.AppNameDisplay;   

                            // show the filename, use standard windows error
                            var errorMsg = new Win32Exception(2).Message + ": " + exception.FileName;

                            MessageBox.Show(errorMsg, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                            return;
                        }
                    }

                    var deviceType = ComboBoxDeviceType.SelectedItem as LKGPlatform;

                    App.TelemetryClient.TrackEvent("FlashSDCard", new Dictionary<string, string>()
                    {
                        { "DeviceType", (deviceType != null) ? deviceType.ToString() : "" },
                        { "Build",  (build != null) ? build.Build.ToString() : ""}
                    });

                    // For flash speed metric telemetry
                    _cachedBuildInfo = build;
                    _cachedDeviceType = deviceType;
                    _cachedDriveInfo = driveInfo;
                    _flashStartTime = App.GlobalStopwatch.ElapsedMilliseconds;
                }
            }
        }

        private readonly object _dismLock = new object();

        private void DismProcess_Exited(object sender, EventArgs e)
        {
            lock(_dismLock)
            {
                // Measure how long it took to flash the image
                App.TelemetryClient.TrackMetric("FlashSDCardTimeMs", App.GlobalStopwatch.ElapsedMilliseconds - _flashStartTime, new Dictionary<string, string>()
                {
                    { "DeviceType", (_cachedDeviceType != null) ? _cachedDeviceType.ToString() : "" },
                    { "Build",  (_cachedBuildInfo != null) ? _cachedBuildInfo.Build.ToString() : ""},
                    { "DriveSize", (_cachedDriveInfo != null) ? _cachedDriveInfo.SizeString : ""},
                    { "DriveModel", (_cachedDriveInfo != null) ? _cachedDriveInfo.Model : "" }
                });

                if (_dismProcess != null)
                {
                    _dismProcess.Dispose();
                    _dismProcess = null;
                }
            }

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                this.navigationFrame.Navigate(new PageDiskImageComplete(this.navigationFrame));
            }));
        }

        private void buttonCancelDism_Click(object sender, RoutedEventArgs e)
        {
            lock (_dismLock)
            {
                if (_dismProcess != null)
                {
                    NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_BREAK_EVENT, (uint)_dismProcess.Id);

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

            if (_dismProcess != null)
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DriveInfo.RemoveUSBDetectionHandler();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {

        }
    }
}
