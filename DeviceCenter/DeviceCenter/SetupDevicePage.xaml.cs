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
using System.Globalization;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SetupDevicePage.xaml
    /// </summary>
    public partial class SetupDevicePage : Page, IDisposable
    {
        #region DownloadLinks

        private readonly string _rpi2DownloadLink = "http://go.microsoft.com/fwlink/?LinkId=619755";
        private readonly string _mbmDownloadLink = "http://go.microsoft.com/fwlink/?LinkId=619756";

        #endregion

        #region Private Members

        private readonly string _isoFileName = "windows_10_iot_core.iso";

        private readonly LastKnownGood _lkg = new LastKnownGood();
        private EventArrivedEventHandler _usbhandler = null;
        private readonly PageFlow _pageFlow;
        private readonly WebClient _webClient = new WebClient();
        private DeviceSetupHelper _deviceSetupHelper = DeviceSetupHelper.Instance;
        private int previousDriveListHash = -1;
        private readonly Dictionary<string, int> licenseFwLinkLookup = new Dictionary<string, int>()
        {
            { "en", 703961 },
            { "fr", 715644 },
            { "it", 715645 },
            { "de", 715646 },
            { "es", 715647 },
            { "zh-cn", 715648 },
            { "zh-tw", 715649 },
            { "ja", 715650 },
            { "ko", 715651 },
            { "pt", 715652 },
            { "ru", 715653 },
        };

        #endregion

        public SetupDevicePage(PageFlow pageFlow)
        {
            InitializeComponent();
            this._pageFlow = pageFlow;
            App.TelemetryClient.TrackPageView(this.GetType().Name);

            buttonFlash.IsEnabled = false;
            PanelFlashing.Visibility = Visibility.Collapsed;
            PanelManualImage.Visibility = Visibility.Collapsed;
            PanelAutomaticImage.Visibility = Visibility.Visible;

            LoadStateAsync();
        }

        private async void LoadStateAsync()
        {
            ReadLkgFile();
            await RefreshDriveList();
            this._usbhandler = new EventArrivedEventHandler(UsbAddedorRemoved);
            DriveInfo.AddUSBDetectionHandler(_usbhandler);

            SetFlashingState(_deviceSetupHelper.CurrentFlashingState);

            // Hookup the event handlers
            _deviceSetupHelper.ExtractFFUProgress += ExtractFFUProgressChanged;
            _deviceSetupHelper.FlashingCompleted += FlashingCompleted;
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
            List<DriveInfo> drives = null;
            await Task.Run((Action)(() =>
            {
                drives = DriveInfo.GetRemovableDriveList();
            }));

            int newDriveListHash = 0;
            foreach (var cur in drives)
                newDriveListHash |= cur.ToString().GetHashCode();

            if (drives != null && (previousDriveListHash == -1 || newDriveListHash != previousDriveListHash))
            {
                RemoveableDevicesComboBox.Items.Clear();

                if (drives.Count == 0)
                {
                    RemoveableDevicesComboBox.Items.Add(Strings.Strings.NewDeviceInsertSDCardMessage);

                    RemoveableDevicesComboBox.IsEnabled = false;
                    checkBoxEula.IsEnabled = false;
                }
                else
                {
                    foreach (var drive in drives)
                    {
                        RemoveableDevicesComboBox.Items.Add(drive);
                    }

                    RemoveableDevicesComboBox.IsEnabled = true;
                    checkBoxEula.IsEnabled = true;
                }

                previousDriveListHash = newDriveListHash;
                RemoveableDevicesComboBox.SelectedIndex = 0;

                buttonFlash.IsEnabled = UpdateStartState();
            }
        }

        public async void UsbAddedorRemoved(object sender, EventArgs e)
        {
            await this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
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
            string isoFilePath = Path.Combine(Path.GetTempPath(), deviceType.Platform, _isoFileName);

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
                    SetFlashingState(FlashingStates.Downloading);
                    PanelFlashing.Visibility = Visibility.Visible;
                    ProgressText.Text = Strings.Strings.NewDeviceFlashingDownload;
                    try
                    {
                        File.Copy(buildInfo.Path, isoFilePath, true);
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
            if (string.IsNullOrEmpty(ffuPath))
            {
                Debug.WriteLine("Could not get the FFUPath");
                SetFlashingState(FlashingStates.Completed);
                return;
            }
            FlashFFU(ffuPath);
        }

        private async Task<bool> DownloadISO(Uri downloadUri, string isoFilePath)
        {
            long remoteFileSize = 0;

            try
            {
                HttpWebRequest headRequest = HttpWebRequest.CreateHttp(downloadUri);
                headRequest.Method = "HEAD";

                using (WebResponse response = await headRequest.GetResponseAsync())
                {
                    remoteFileSize = response.ContentLength;
                }
            }
            catch (WebException exception)
            {
                if (exception.Status != WebExceptionStatus.RequestCanceled)
                {
                    var errorCaption = LocalStrings.AppNameDisplay;
                    MessageBox.Show(exception.Message, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    ResetProgressUi();
                    return false;
                }
                else
                {
                    return false;
                }
            }

            FileInfo isoFileInfo = new FileInfo(isoFilePath);
            isoFileInfo.Directory.Create();

            if (!isoFileInfo.Exists || (isoFileInfo.Length != remoteFileSize))
            {
                SetFlashingState(FlashingStates.Downloading);
                var platform = ComboBoxDeviceType.SelectedItem as LkgPlatform;
                _webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                _webClient.DownloadFileCompleted += (s, eventargs) =>
                {
                    if (eventargs.Cancelled)
                    {
                        isoFileInfo.Delete();
                        ResetProgressUi();
                        return;
                    }
                };

                SetFlashingState(FlashingStates.Downloading);

                // Download the ISO file
                try
                {
                    await _webClient.DownloadFileTaskAsync(downloadUri, isoFilePath);
                }
                catch (WebException exception)
                {
                    if (exception.Status != WebExceptionStatus.RequestCanceled)
                    {
                        var errorCaption = LocalStrings.AppNameDisplay;
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
                    var errorCaption = LocalStrings.AppNameDisplay;
                    MessageBox.Show(exception.Message, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
            }

            return true;
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                FlashingProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
                ProgressText.Text = string.Format(Strings.Strings.DownloadProgress, Math.Truncate(bytesIn / 1048576), Math.Truncate(percentage));
            }));
        }

        private async Task<string> ExtractFFU(string isoFilePath)
        {
            ProgressText.Text = string.Empty;
            SetFlashingState(FlashingStates.Extracting);
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
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                // TODO: get real progress
                //FlashingProgress.Value = e.Progress;
            }));
        }

        private void FlashFFU(string ffuPath)
        {
            var driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;
            SetFlashingState(FlashingStates.Flashing);

            var dlg = new WindowWarning()
            {
                Header = Strings.Strings.ConnectAlertTitle,
                Message = Strings.Strings.NewDeviceAlertMessage + "\n" + Strings.Strings.NewDeviceAlertMessage2,
                Owner = Window.GetWindow(this)
            };

            var confirmation = dlg.ShowDialog();
            if (confirmation.HasValue && confirmation.Value == false)
            {
                SetFlashingState(FlashingStates.Completed);
                buttonFlash.IsEnabled = UpdateStartState();
                return;
            }
            else
            {
                try
                {
                    _deviceSetupHelper.FlashFFU(ffuPath, driveInfo);

                    // end this early, flashing is asynchronous and will restore state
                    return;
                }
                catch (FileNotFoundException ex)
                {
                    Debug.WriteLine(ex.ToString());
                    // the app name as caption
                    var errorCaption = LocalStrings.AppNameDisplay;

                    // show the filename, use standard windows error
                    var errorMsg = new Win32Exception(2).Message + ": " + ex.FileName;

                    MessageBox.Show(errorMsg, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                catch (Win32Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    // This happens if UAC is declined, ignore and reset back
                }

                ResetProgressUi();
            }
        }

        void FlashingCompleted(object sender, FlashingCompletedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                ResetProgressUi();
                if (!e.Success)
                {
                    Debug.WriteLine("Flashing FFU to SD Card Failed");
                }
                else
                {
                    _pageFlow.Navigate(typeof(PageDiskImageComplete));
                }
            }));
        }

        private void ResetProgressUi()
        {
            SetFlashingState(FlashingStates.Completed);
        }

        private void buttonCancelDism_Click(object sender, RoutedEventArgs e)
        {
            // Only cancel DISM/Download
            switch (_deviceSetupHelper.CurrentFlashingState)
            {
                case FlashingStates.Downloading:
                    _webClient.CancelAsync();
                    SetFlashingState(FlashingStates.Completed);
                    break;
                case FlashingStates.Flashing:
                    _deviceSetupHelper.CancelDism();
                    SetFlashingState(FlashingStates.Completed);
                    break;
            }
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

                ComboBoxDeviceType.UpdateLayout();
                ComboBoxIotBuild.UpdateLayout();

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
            if (_deviceSetupHelper.CurrentFlashingState != FlashingStates.Completed)
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

        private void SetFlashingState(FlashingStates state)
        {
            _deviceSetupHelper.CurrentFlashingState = state;
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                buttonFlash.IsEnabled = UpdateStartState();
                switch (_deviceSetupHelper.CurrentFlashingState)
                {
                    case FlashingStates.Completed:
                        FlashingProgress.Value = 100;
                        PanelFlashing.Visibility = Visibility.Collapsed;
                        ComboBoxDeviceType.IsEnabled = true;
                        ProgressText.Text = string.Empty;
                        break;
                    case FlashingStates.Downloading:
                        ProgressText.Text = Strings.Strings.NewDeviceFlashingDownload;
                        buttonFlash.IsEnabled = false;
                        PanelFlashing.Visibility = Visibility.Visible;
                        buttonCancelDism.IsEnabled = true;
                        ComboBoxDeviceType.IsEnabled = false;
                        break;
                    case FlashingStates.Extracting:
                        FlashingProgress.Value = 33;
                        ProgressText.Text = Strings.Strings.NewDeviceFlashingExtractMSI;
                        buttonFlash.IsEnabled = false;
                        PanelFlashing.Visibility = Visibility.Visible;
                        buttonCancelDism.IsEnabled = false;
                        ComboBoxDeviceType.IsEnabled = false;
                        break;
                    case FlashingStates.Flashing:
                        FlashingProgress.Value = 66;
                        ProgressText.Text = Strings.Strings.NewDeviceFlashing;
                        buttonFlash.IsEnabled = false;
                        PanelFlashing.Visibility = Visibility.Visible;
                        buttonCancelDism.IsEnabled = true;
                        ComboBoxDeviceType.IsEnabled = true;
                        break;
                }

                ComboBoxIotBuild.IsEnabled = ComboBoxDeviceType.IsEnabled;
                RemoveableDevicesComboBox.IsEnabled = ComboBoxDeviceType.IsEnabled;
                checkBoxEula.IsEnabled = ComboBoxDeviceType.IsEnabled;
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

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            int resourceId = -1;

            if (licenseFwLinkLookup.ContainsKey(CultureInfo.CurrentUICulture.Name))
                resourceId = licenseFwLinkLookup[CultureInfo.CurrentUICulture.Name];
            else if (licenseFwLinkLookup.ContainsKey(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName))
                resourceId = licenseFwLinkLookup[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];
            else
                resourceId = licenseFwLinkLookup["en"];

            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri + resourceId.ToString(CultureInfo.InvariantCulture)));
            }
            catch(Exception ex)
            {
                MessageBox.Show(
                        ex.Message,
                        LocalStrings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
            }
            e.Handled = true;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _deviceSetupHelper.ExtractFFUProgress -= ExtractFFUProgressChanged;
                    _deviceSetupHelper.FlashingCompleted -= FlashingCompleted;

                    // Only cancel the download, do not cancel DISM
                    if (_deviceSetupHelper.CurrentFlashingState == FlashingStates.Downloading)
                    {
                        _webClient.CancelAsync();
                        _deviceSetupHelper.CurrentFlashingState = FlashingStates.Completed;
                    }

                    DriveInfo.RemoveUSBDetectionHandler();

                    _webClient.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}

