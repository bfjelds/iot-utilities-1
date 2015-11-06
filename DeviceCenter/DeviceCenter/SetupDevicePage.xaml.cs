﻿using System;
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
using DeviceCenter.Helper;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SetupDevicePage.xaml
    /// </summary>
    public partial class SetupDevicePage : Page
    {
        #region DownloadLinks

        private readonly string _rpi2DownloadLink = "http://go.microsoft.com/fwlink/?LinkId=619755";
        private readonly string _mbmDownloadLink = "http://go.microsoft.com/fwlink/?LinkId=619756";

        #endregion

        #region Private Members

        private readonly string _isoFileName = "windows_10_iot_core.iso";
        private readonly LastKnownGood _lkg = new LastKnownGood();
        private EventArrivedEventHandler _usbhandler = null;
        private readonly Frame navigationFrame;
        private FlashingStates _currentFlashingState = FlashingStates.Completed;
        private readonly WebClient _webClient = new WebClient();
        private int _dismProcessId = 0;
        private DeviceSetupHelper _deviceSetupHelper = new DeviceSetupHelper();

        #endregion

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
            UpdateProgressState();
        }

        private void SetUpDefaults()
        {
            // If unable to read files
            ComboBoxIotBuild.Visibility = Visibility.Collapsed;
            var mbmPlatform = LkgPlatform.CreateMbm();
            mbmPlatform.LkgBuilds = new List<BuildInfo>();
            mbmPlatform.LkgBuilds.Add(new BuildInfo("RTM", _mbmDownloadLink));

            var rpi2Platform = LkgPlatform.CreateRpi2();
            rpi2Platform.LkgBuilds = new List<BuildInfo>();
            rpi2Platform.LkgBuilds.Add(new BuildInfo("RTM", _rpi2DownloadLink));

            var qcomPlatform = LkgPlatform.CreateQCom();
            qcomPlatform.LkgBuilds = new List<BuildInfo>();
            qcomPlatform.LkgBuilds.Add(new BuildInfo("RTM", "NA"));

            ComboBoxDeviceType.Items.Add(rpi2Platform);
            ComboBoxDeviceType.Items.Add(mbmPlatform);
            ComboBoxDeviceType.Items.Add(qcomPlatform);
            ComboBoxDeviceType.SelectedIndex = 0;
            ComboBoxDeviceType.IsEnabled = true;
            ComboBoxIotBuild.IsEnabled = true;
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
                        entries.Add(currentPlatform);
                    }
                }
            }));

            foreach (var currentEntry in entries)
                ComboBoxDeviceType.Items.Add(currentEntry);

            if (ComboBoxDeviceType.Items.Count == 0)
            {
                ComboBoxDeviceType.Items.Add("Failed to fetch the list");
                ComboBoxDeviceType.SelectedIndex = 0;
                ComboBoxDeviceType.IsEnabled = false;
            }
            else
            {
                ComboBoxDeviceType.IsEnabled = true;
                ComboBoxIotBuild.IsEnabled = true;
            }

            // Rpi2 is default
            ComboBoxDeviceType.SelectedIndex = 1;
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

        /// <summary>
        /// Called when user clicks Continue to flash image to SD card. 
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">not used</param>
        private async void FlashSDCard_Click(object sender, RoutedEventArgs e)
        {
            var deviceType = ComboBoxDeviceType.SelectedItem as LkgPlatform;
            var driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;

            // For internal build use the BuildInfo
            buttonFlash.IsEnabled = false;
            string ffuPath = string.Empty;
            var buildInfo = ComboBoxIotBuild.SelectedItem as BuildInfo;
            string isoFilePath = Path.Combine(Path.GetTempPath(), _isoFileName);

            var BuildPathType = DeviceSetupHelper.GetTypeOfBuildPath(buildInfo.Path);

            switch (BuildPathType)
            {
                case BuildPathType.FFUFile:
                    ffuPath = buildInfo.Path;
                    break;

                case BuildPathType.HttpURL:
                    bool fDownloadSuccess = await DownloadISO(new Uri(buildInfo.Path), isoFilePath);
                    if (!fDownloadSuccess)
                    {
                        Debug.WriteLine("Failed to download the ISO File from the URL");
                        return;
                    }
                    ffuPath = await ExtractFFU(isoFilePath);
                    break;

                case BuildPathType.ISOFile:
                    _currentFlashingState = FlashingStates.Downloading;
                    PanelFlashing.Visibility = Visibility.Visible;
                    FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashingDownload;
                    FlashingProgress.Value = 0;
                    try
                    {
                        File.Copy(buildInfo.Path, isoFilePath, true);
                        FlashingProgress.Value = 100;
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Failed to copy the ISO File from the remote share");
                        return;
                    }
                    ffuPath = await ExtractFFU(isoFilePath);
                    break;

                default:
                    Debug.WriteLine("Unsupported type of BuildUrl encountered.");
                    return;
            }

            // Finally flash the FFU to SD card
            FlashFFU(ffuPath);
        }

        private async Task<bool> DownloadISO(Uri downloadUri, string isoFilePath)
        {
            _currentFlashingState = FlashingStates.Downloading;
            UpdateProgressState();
            var platform = ComboBoxDeviceType.SelectedItem as LkgPlatform;
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

            // Download the ISO file
            try
            {
                await _webClient.DownloadFileTaskAsync(downloadUri, isoFilePath);
            }
            catch (WebException exception)
            {
                if (exception.Status != WebExceptionStatus.RequestCanceled)
                {
                    var errorCaption = Strings.Strings.AppNameDisplay;
                    MessageBox.Show(exception.Message, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    ResetProgressUi();
                    return false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                var errorCaption = Strings.Strings.AppNameDisplay;
                MessageBox.Show(exception.Message, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                FlashingProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
            }));

        }

        private async Task<string> ExtractFFU(string isoFilePath)
        {
            _currentFlashingState = FlashingStates.Extracting;
            UpdateProgressState();
            _deviceSetupHelper.ExtractFFUProgress += ExtractFFUProgressChanged;
            var deviceType = ComboBoxDeviceType.SelectedItem as LkgPlatform;
            string ffuPath = string.Empty;
            await Task.Run((Action)(() =>
            {
                ffuPath = _deviceSetupHelper.ExtractFFUFromISO(isoFilePath, deviceType);
            }));
            if (String.IsNullOrEmpty(ffuPath))
            {
                Debug.WriteLine("Failed to extract the FFU from ISO file");
                MessageBox.Show(Strings.Strings.NewDeviceFlashingExtractMSIFailed,
                                Strings.Strings.NewDeviceFlashingExtractMSIFailedCaption,
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ffuPath = string.Empty;
            }
            return ffuPath;
        }

        private void ExtractFFUProgressChanged(object sender, ExtractFFUProgressEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (FlashingProgress.Value == 0)
                {
                    
                }
                FlashingProgress.Value = e.Progress;
            }));
        }

        private void FlashFFU(string ffuPath)
        {
            var driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;
            _currentFlashingState = FlashingStates.Flashing;
            UpdateProgressState();

            var dlg = new WindowWarning()
            {
                Header = Strings.Strings.ConnectAlertTitle,
                Message = Strings.Strings.NewDeviceAlertMessage + "\n" + Strings.Strings.NewDeviceAlertMessage2,
                Owner = Window.GetWindow(this)
            };

            var confirmation = dlg.ShowDialog();
            if (confirmation.HasValue && confirmation.Value == false)
            {
                _currentFlashingState = FlashingStates.Completed;
                buttonFlash.IsEnabled = UpdateStartState();
                ResetProgressUi();
                return;
            }
            else
            {
                try
                {
                    _deviceSetupHelper.FlashingCompleted += FlashingCompleted;
                    _dismProcessId = _deviceSetupHelper.FlashFFU(ffuPath, driveInfo);
                }
                catch (Exception ex)
                {
                    HandleFlashFFUException(ex);
                    return;
                }
            }
        }

        void FlashingCompleted(object sender, FlashingCompletedEventArgs e)
        {
            ResetProgressUi();
            if (!e.Success)
            {
                Debug.WriteLine("Flashing FFU to SD Card Failed");
            }
        }

        private void HandleFlashFFUException(Exception ex)
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
            }
        }

        private void ResetProgressUi()
        {
            _currentFlashingState = FlashingStates.Completed;
            UpdateProgressState();
        }

        private void buttonCancelDism_Click(object sender, RoutedEventArgs e)
        {
            // Only cancel DISM/Download
            switch (_currentFlashingState)
            {
                case FlashingStates.Downloading:
                    _webClient.CancelAsync();
                    _currentFlashingState = FlashingStates.Completed;
                    break;
                case FlashingStates.Flashing:
                    _deviceSetupHelper.CancelDism();
                    _currentFlashingState = FlashingStates.Completed;
                    break;
            }
            UpdateProgressState();
        }

        private void ComboBoxDeviceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ComboBoxDeviceType.SelectedItem as LkgPlatform;
            if (item != null)
            {
                ComboBoxIotBuild.Items.Clear();

                foreach (var cur in item.LkgBuilds)
                    ComboBoxIotBuild.Items.Add(cur);

                if (ComboBoxIotBuild.Items.Count > 0)
                    ComboBoxIotBuild.SelectedIndex = 0;

                PanelManualImage.Visibility = (item.Platform == "QCOM") ? Visibility.Visible : Visibility.Collapsed;
                PanelAutomaticImage.Visibility = (item.Platform != "QCOM") ? Visibility.Visible : Visibility.Collapsed;

                buttonFlash.IsEnabled = UpdateStartState();
            }
            else
            {
                buttonFlash.IsEnabled = false;
            }
        }

        private bool UpdateStartState()
        {
            if (_currentFlashingState != FlashingStates.Completed)
                return false;

            if (!RemoveableDevicesComboBox.IsEnabled || !ComboBoxDeviceType.IsEnabled)
                return false;

            if (RemoveableDevicesComboBox.SelectedItem == null)
                return false;

            var driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;

            if (driveInfo == null)
                return false;

            var isChecked = checkBoxEula.IsChecked;

            return isChecked.HasValue && isChecked.Value;
        }

        private void UpdateProgressState()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                buttonFlash.IsEnabled = UpdateStartState();
                switch (_currentFlashingState)
                {
                    case FlashingStates.Completed:
                        FlashingProgress.Value = 0;
                        PanelFlashing.Visibility = Visibility.Collapsed;
                        break;
                    case FlashingStates.Downloading:
                        FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashingDownload;
                        buttonFlash.IsEnabled = false;
                        PanelFlashing.Visibility = Visibility.Visible;
                        buttonCancelDism.IsEnabled = true;
                        break;
                    case FlashingStates.Extracting:
                        FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashingExtractMSI;
                        buttonFlash.IsEnabled = false;
                        PanelFlashing.Visibility = Visibility.Visible;
                        buttonCancelDism.IsEnabled = false;
                        break;
                    case FlashingStates.Flashing:
                        FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashing;
                        buttonFlash.IsEnabled = false;
                        PanelFlashing.Visibility = Visibility.Visible;
                        buttonCancelDism.IsEnabled = true;
                        break;
                }
            }));
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
