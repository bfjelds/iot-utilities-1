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
using System.Net;
using System.Management.Automation;
using System.Collections.ObjectModel;

namespace DeviceCenter
{
    public enum FlashingStates
    {
        Downloading,
        Extracting,
        Flashing,
        Completed
    }

    /// <summary>
    /// Interaction logic for SetupDevicePage.xaml
    /// </summary>
    public partial class SetupDevicePage : Page
    {
        /// <summary>
        /// Parser for the LKG file.   
        /// TBD this is needed only for internal build.
        /// </summary>

#region Constants
        private readonly Uri _rpi2DownloadLink = new Uri("http://go.microsoft.com/fwlink/?LinkId=619755");
        private readonly Uri _mbmDownloadLink = new Uri("http://go.microsoft.com/fwlink/?LinkId=619756");        
        private readonly string _rpi2MsiName = "Windows_10_IoT_Core_RPi2.msi";
        private readonly string _mbmMsiName = "Windows_10_IoT_Core_Mbm.msi";
        private readonly string _mbmFfuSubPath = @"Microsoft IoT\FFU\MinnowBoardMax\Flash.ffu";
        private readonly string _rpi2FfuSubPath = @"Microsoft IoT\FFU\RaspberryPi2\Flash.ffu";
        readonly LastKnownGood _lkg = new LastKnownGood();
#endregion

        private double _flashStartTime = 0;
        private LkgPlatform _cachedDeviceType;
        private DriveInfo _cachedDriveInfo;
        private BuildInfo _cachedBuildInfo;
        private EventArrivedEventHandler _usbhandler = null;
        private readonly Frame navigationFrame;
        private string _isoFilePath;
        private FlashingStates _currentFlashingState;
        private bool _internalBuild = false;
        readonly WebClient _webClient = new WebClient();

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

            PanelManualImage.Visibility = Visibility.Collapsed;
            PanelAutomaticImage.Visibility = Visibility.Visible;
        }

        private void SetUpDefaults()
        {
            // If unable to read files
            _internalBuild = false;
            ComboBoxIotBuild.Visibility = Visibility.Hidden;

            // TODO: populate here for ISO on Internet.  TFS#5299920.

            ComboBoxDeviceType.Items.Add("LKG not found \\\\webnas\\AthensDrop");
            ComboBoxDeviceType.SelectedIndex = 0;
            ComboBoxDeviceType.IsEnabled = false;

            buttonFlash.IsEnabled = UpdateStartState();
        }

        private async void ReadLkgFile()
        {
            try
            {
                if (!await _lkg.ReadFileAsync())
                {
                    SetUpDefaults();
                    return;
                }
            }
            catch (Exception)
            {
                SetUpDefaults();
                return;
            }

            _internalBuild = true;
            var entries = new List<LkgPlatform>();
            ComboBoxDeviceType.IsEnabled = false;
            ComboBoxIotBuild.IsEnabled = false;

            await Task.Run((Action)(() =>
            {
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
                            case "QCOM":
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
        private async void FlashSDCard_Click(object sender, RoutedEventArgs e)
        {
            // For internal build use the BuildInfo
            buttonFlash.IsEnabled = false;
            if (_internalBuild)
            {
                var build = ComboBoxIotBuild.SelectedItem as BuildInfo;
                if (build != null)
                {
                    FlashFFU(build);
                    return;
                }
            }

            // For public builds, download the FFU from Download Center

            // show the Download progress 
            PanelFlashing.Visibility = Visibility.Visible;

            // 1. Download the image from the URL 
            Uri downloadUri;
            string msiName;
            string ffuSubPath; 
            if (ComboBoxDeviceType.SelectedIndex == 1)
            {
                downloadUri = _rpi2DownloadLink;
                msiName = _rpi2MsiName;
                ffuSubPath = _rpi2FfuSubPath;
            }
            else
            {
                downloadUri = _mbmDownloadLink;
                msiName = _mbmMsiName;
                ffuSubPath = _mbmFfuSubPath;
            }

            _isoFilePath = Path.Combine(Path.GetTempPath(), "windows_10_iot_core.iso");
            FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashingDownload;
            var selectedType = ComboBoxDeviceType.SelectedValuePath;
            _webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
            _webClient.DownloadFileCompleted += (s, eventargs) =>
            {
                if (eventargs.Cancelled)
                {
                    ResetProgressUi();
                    return;
                }
            };

            _currentFlashingState = FlashingStates.Downloading;
            try
            {
                await _webClient.DownloadFileTaskAsync(downloadUri, _isoFilePath);
            }
            catch (WebException exception)
            {
                if (exception.Status != WebExceptionStatus.RequestCanceled)
                {
                    var errorCaption = Strings.Strings.AppNameDisplay;
                    MessageBox.Show(exception.Message, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    ResetProgressUi();
                }
                else
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                var errorCaption = Strings.Strings.AppNameDisplay;
                MessageBox.Show(exception.Message, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashingExtractMSI;
            _currentFlashingState = FlashingStates.Extracting;
            FlashingProgress.Value = 0;
            string ffuPath = string.Empty;
            await Task.Run((Action)(() =>
            {
                ffuPath = Extract(msiName, ffuSubPath);
            }));

            if (String.IsNullOrEmpty(ffuPath))
            {
                MessageBox.Show(Strings.Strings.NewDeviceFlashingExtractMSIFailed,
                                Strings.Strings.NewDeviceFlashingExtractMSIFailedCaption,
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return; 
            }
            FlashingProgress.Value = 99;
            FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashing;
            _currentFlashingState = FlashingStates.Flashing;

            FlashFFU(ffuPath);
            
            _currentFlashingState = FlashingStates.Completed;
        }

        public string  Extract(string msiName, string ffuPath)
        {
            using (PowerShell powerShellInstance = PowerShell.Create())
            {
                // use "AddScript" to add the contents of a script file to the end of the execution pipeline.
                // use "AddCommand" to add individual commands/cmdlets to the end of the execution pipeline.
                powerShellInstance.AddScript("$mountResult = Mount-DiskImage " + _isoFilePath + " -PassThru | Get-Volume; $mountResult.DriveLetter; ");

                // invoke the script
                Collection<PSObject> psOutput = powerShellInstance.Invoke();

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    FlashingProgress.Value = 33;
                }));

                // loop through each output object item
                string driveLetter = "";
                foreach (PSObject outputItem in psOutput)
                {
                    if (null != outputItem)
                    {
                        driveLetter = outputItem.BaseObject.ToString();
                        break;
                    }
                }

                string msiPath;
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    msiPath = driveLetter + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + msiName;
                }
                else
                {
                    DisMountVhd();
                    return string.Empty;
                }

                string extractionPath = Path.Combine(Path.GetTempPath() + "IoTCoreMSIContent");

                // Delete everything in the extractionPath
                try
                {
                    var extractionPathInfo = new DirectoryInfo(extractionPath);
                    foreach (var file in extractionPathInfo.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (var dir in extractionPathInfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    //Its ok if the directory is not present
                }

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    FlashingProgress.Value = 66;
                }));

                var msiProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = "msiexec",
                        Arguments =
                            $@"/a {msiPath} /qn TARGETDIR={extractionPath} REINSTALLMODE=amus"
                    }
                };
                
                // msiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                msiProcess.Start();
                msiProcess.WaitForExit();
                DisMountVhd();

                return msiProcess.ExitCode != 0 ? string.Empty : Path.Combine(extractionPath, ffuPath);
            }
        }
        void DisMountVhd()
        {
            using (var powerShellInstance = PowerShell.Create())
            {
                // Dismount the ISO
                powerShellInstance.AddScript("$mountResult = Dismount-DiskImage " + _isoFilePath);
                var psOutput = powerShellInstance.Invoke();
            }
        }

        void FlashFFU(BuildInfo bldInfo)
        {
            FlashFFU(bldInfo.Path);
            var deviceType = ComboBoxDeviceType.SelectedItem as LkgPlatform;

            App.TelemetryClient.TrackEvent("FlashSDCard", new Dictionary<string, string>()
            {
                { "DeviceType", (deviceType != null) ? deviceType.ToString() : "" },
                { "Build",  (bldInfo != null) ? bldInfo.Build.ToString() : ""}
            });

            // For flash speed metric telemetry
            _cachedBuildInfo = bldInfo;
            lock (_dismLock)
            {
                _cachedDeviceType = deviceType;
            }
            _cachedDriveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo; ;
            _flashStartTime = App.GlobalStopwatch.ElapsedMilliseconds;
        }

        void FlashFFU(string ffuPath)
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
                    Message = Strings.Strings.NewDeviceAlertMessage + "\n" + Strings.Strings.NewDeviceAlertMessage2,
                    Owner = Window.GetWindow(this)
                };

                var confirmation = dlg.ShowDialog();
                if (confirmation.HasValue && confirmation.Value == false)
                {
                    buttonFlash.IsEnabled = true;
                    ResetProgressUi();
                }

                if (confirmation.HasValue && confirmation.Value)
                {
                    try
                    {
                        _dismProcess = Dism.FlashFfuImageToDrive(ffuPath, driveInfo);
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
                }
            }
        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                FlashingProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
            }));
          
        }

        private readonly object _dismLock = new object();

        private void ResetProgressUi()
        {
            _currentFlashingState = FlashingStates.Completed;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                buttonFlash.IsEnabled = true;
                FlashingProgress.Value = 0;
                PanelFlashing.Visibility = Visibility.Hidden;
            }));
        }

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

                ResetProgressUi();
            }

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                this.navigationFrame.Navigate(new PageDiskImageComplete(this.navigationFrame));
            }));
        }

        private void buttonCancelDism_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentFlashingState)
            {
                case FlashingStates.Downloading:
                    _webClient.CancelAsync();
                    break;
                case FlashingStates.Flashing:
                    lock (_dismLock)
                    {
                        if (_dismProcess != null)
                        {
                            NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_BREAK_EVENT, (uint)_dismProcess.Id);
                            App.TelemetryClient.TrackEvent("FlashSDCardCancel");
                        }
                    }
                    break;
            }
            _currentFlashingState = FlashingStates.Completed;
        }

        private void ComboBoxDeviceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ComboBoxDeviceType.SelectedItem as LkgPlatform;
            if (item != null)
            {
                if (_internalBuild)
                {
                    ComboBoxIotBuild.Items.Clear();

                    foreach (var cur in item.LkgBuilds)
                        ComboBoxIotBuild.Items.Add(cur);

                    if (ComboBoxIotBuild.Items.Count > 0)
                        ComboBoxIotBuild.SelectedIndex = 0;
                }

                PanelManualImage.Visibility = (item.Platform == "QCOM") ? Visibility.Visible : Visibility.Collapsed;
                PanelAutomaticImage.Visibility = (item.Platform != "QCOM") ? Visibility.Visible : Visibility.Collapsed;

                buttonFlash.IsEnabled = UpdateStartState();
            }
            else
                buttonFlash.IsEnabled = false;
        }

        private bool UpdateStartState()
        {
            if (!RemoveableDevicesComboBox.IsEnabled || !ComboBoxDeviceType.IsEnabled)
                return false;

            if (_dismProcess != null)
                return false;

            if (_internalBuild)
            {
                // guards for invalid data
                if (ComboBoxIotBuild.SelectedIndex < 0)
                    return false;

                // guards for invalid data
                BuildInfo build = ComboBoxIotBuild.SelectedItem as BuildInfo;
                if (build == null)
                    return false;
            }

            if (RemoveableDevicesComboBox.SelectedItem == null)
                return false;

            var driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;

            if (driveInfo == null)
                return false;

            var isChecked = checkBoxEula.IsChecked;

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
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
