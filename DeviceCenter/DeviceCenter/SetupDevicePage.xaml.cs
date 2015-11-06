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
using DeviceCenter.Helper;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SetupDevicePage.xaml
    /// </summary>
    public partial class SetupDevicePage : Page
    {
        #region DownloadLinks

        private readonly Uri _rpi2DownloadLink = new Uri("http://go.microsoft.com/fwlink/?LinkId=619755");
        private readonly Uri _mbmDownloadLink = new Uri("http://go.microsoft.com/fwlink/?LinkId=619756");

        #endregion

        #region Private Members

        private readonly string _isoFileName = "windows_10_iot_core.iso";
        private readonly LastKnownGood _lkg = new LastKnownGood();
        private EventArrivedEventHandler _usbhandler = null;
        private readonly Frame navigationFrame;
        private string _isoFilePath;
        private FlashingStates _currentFlashingState;
        private bool _internalBuild = false;
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
        }

        private void SetUpDefaults()
        {
            // If unable to read files
            _internalBuild = false;
            ComboBoxIotBuild.Visibility = Visibility.Hidden;

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
                        entries.Add(currentPlatform);
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
            if (_internalBuild)
            {
                var build = ComboBoxIotBuild.SelectedItem as BuildInfo;
                if (build != null)
                {
                    try
                    {
                        _dismProcessId = _deviceSetupHelper.FlashFFU(build, deviceType, driveInfo);
                    }
                    catch (Exception ex)
                    {
                        HandleFlashFFUException(ex);
                    }
                    return;
                }
            }

            // For public builds, download the ISO from Download Center
            bool fDownloadSuccess = await DownloadISO();
            if (!fDownloadSuccess)
                return;

            // Extract the FFU and flash it on the SD card
            FlashDownloadedFFU();
        }

        private async Task<bool>  DownloadISO()
        {
            // Show the Download progress 
            PanelFlashing.Visibility = Visibility.Visible;

            // Download the image from the URL 
            // Only Rpi2 and MBM flashing is supported. Return if the selected index is greater than 1 
            if (ComboBoxDeviceType.SelectedIndex > 1)
            {
                return false;
            }

            Uri downloadUri = ComboBoxDeviceType.SelectedIndex == 1 ? _rpi2DownloadLink : _mbmDownloadLink;

            // Generate temp path for the downloaded ISO file
            _isoFilePath = Path.Combine(Path.GetTempPath(), _isoFileName);

            // Change the flashing state to downloading
            FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashingDownload;
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
                await _webClient.DownloadFileTaskAsync(downloadUri, _isoFilePath);
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

        private async void FlashDownloadedFFU()
        {
            var deviceType = ComboBoxDeviceType.SelectedItem as LkgPlatform;
            var driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;

            FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashingExtractMSI;
            _currentFlashingState = FlashingStates.Extracting;
            FlashingProgress.Value = 0;
            string ffuPath = string.Empty;
            await Task.Run((Action)(() =>
            {
                ffuPath = _deviceSetupHelper.ExtractFFUFromISO(_isoFilePath, deviceType);
            }));

            if (String.IsNullOrEmpty(ffuPath))
            {
                MessageBox.Show(Strings.Strings.NewDeviceFlashingExtractMSIFailed,
                                Strings.Strings.NewDeviceFlashingExtractMSIFailedCaption,
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            FlashingStateTextBox.Text = Strings.Strings.NewDeviceFlashing;
            _currentFlashingState = FlashingStates.Flashing;

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
                return;
            }
            else
            {
                try
                {
                    _deviceSetupHelper.FlashFFU(ffuPath, driveInfo);
                }
                catch (Exception ex)
                {
                    HandleFlashFFUException(ex);
                    return;
                }
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
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                buttonFlash.IsEnabled = true;
                FlashingProgress.Value = 0;
                PanelFlashing.Visibility = Visibility.Hidden;
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
                    _deviceSetupHelper.CancelDism();
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
