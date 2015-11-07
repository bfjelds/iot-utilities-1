using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using DeviceCenter.Helper;
using WlanAPIs;
using System.Windows.Data;
using System.ComponentModel;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for ViewDevicesPage.xaml
    /// </summary>
    public partial class ViewDevicesPage : Page
    {
        private DiscoveryHelper _discoveryHelper = DiscoveryHelper.Instance;
        private readonly DispatcherTimer _telemetryTimer = new DispatcherTimer();
        private DiscoveredDevice _newestBuildDevice, _oldestBuildDevice;

        private readonly SoftApHelper _softwareAccessPoint;
        private readonly DispatcherTimer _wifiRefreshTimer = new DispatcherTimer();

        private readonly Frame _navigationFrame;
        private PageWifi _wifiPage;
        private readonly int pollDelayWifi = 5;

        public ViewDevicesPage(Frame navigationFrame)
        {
            // initialize parameters
            this._navigationFrame = navigationFrame;
            this._newestBuildDevice = null;
            this._oldestBuildDevice = null;

            // initialize helpers
            this._softwareAccessPoint = SoftApHelper.Instance;

            App.TelemetryClient.TrackPageView(this.GetType().Name);

            InitializeComponent();

            // set up binding
            UpdateFilter();

            //Sort the listview
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDevices.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("DeviceName", ListSortDirection.Ascending));

            //Register the callbacks
            _softwareAccessPoint.OnSoftApDisconnected += SoftwareAccessPoint_OnSoftAPDisconnected;
            _softwareAccessPoint.OnWlanScanComplete += SoftwareAccessPoint_OnWlanScanComplete;
            _wifiRefreshTimer.Interval = TimeSpan.FromSeconds(pollDelayWifi);
            _wifiRefreshTimer.Tick += WifiRefreshTimer_Tick;
            _wifiRefreshTimer.Start();
            WifiRefreshTimer_Tick(this, null);

            // Set up polling
            _telemetryTimer.Interval = TimeSpan.FromSeconds(3);
            _telemetryTimer.Tick += TelemetryTimer_Tick;
            _telemetryTimer.Start();
        }

        private void SoftwareAccessPoint_OnWlanScanComplete(object sender, WlanScanCompleteArgs e)
        {
            foreach (WlanInterop.WlanAvailableNetwork accessPoint in e.AvaliableNetworks)
            {
                _discoveryHelper.AddAdhocDevice(accessPoint);
            }
        }

        ~ViewDevicesPage()
        {
            _wifiRefreshTimer.Stop();
            _softwareAccessPoint.DisconnectIfNeeded();

            DiscoveryHelper.Release();
            _discoveryHelper = null;
        }

        private void SoftwareAccessPoint_OnSoftAPDisconnected(object sender, EventArgs e)
        {
        }

        private void ListViewDevices_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_wifiRefreshTimer != null)
                _wifiRefreshTimer.Stop();
        }

        private void RefreshWifiAsync()
        {
            if (_wifiRefreshTimer == null || _softwareAccessPoint == null)
                return;

            try
            {
                try
                {
                    _softwareAccessPoint.GetAvailableNetworkList();
                }
                catch (WLanException)
                {
                    // ignore error, return empty list
                }
            }
            finally
            {
                _wifiRefreshTimer.Start();
            }
        }

        private void WifiRefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshWifiAsync();
        }

        private void TelemetryTimer_Tick(object sender, EventArgs e)
        {
            // Only send a telemetry event if we've found build information
            if (_oldestBuildDevice != null && _newestBuildDevice != null)
            {
                var deviceCount = _discoveryHelper.AllDevices.Count;

                Debug.WriteLine("Sending telemetry event... ");
                Debug.WriteLine("Max OS Version: " + _newestBuildDevice.OsVersion);
                Debug.WriteLine("Min OS Version: " + _oldestBuildDevice.OsVersion);
                Debug.WriteLine("Number of devices: " + deviceCount);

                App.TelemetryClient.TrackEvent("DeviceDiscovery", new Dictionary<string, string>()
                {
                    { "OldestDeviceId", _oldestBuildDevice.UniqueId.ToString() },
                    { "OldestBuildVersion", _oldestBuildDevice.OsVersion },
                    { "OldestDeviceModel", _oldestBuildDevice.DeviceModel },
                    { "OldestArchitecture", _oldestBuildDevice.Architecture },
                    { "NewestDeviceId", _newestBuildDevice.UniqueId.ToString() },
                    { "NewestBuildVersion", _newestBuildDevice.OsVersion },
                    { "NewestDeviceModel", _newestBuildDevice.DeviceModel },
                    { "NewestArchitecture", _newestBuildDevice.Architecture },
                    { "NumDevices", deviceCount.ToString() }
                });
            }

            _telemetryTimer.Stop();
        }



        private int compareOsVersions(string osVersion1, string osVersion2)
        {
            if (osVersion1 == osVersion2)
            {
                return 0;
            }

            var osParts1 = osVersion1.Split('.');
            var osParts2 = osVersion2.Split('.');

            var numParts = (osParts1.Length < osParts2.Length) ? osParts1.Length : osParts2.Length;

            for (int i = 0; i < numParts; i++)
            {
                var partNum1 = Convert.ToInt32(osParts1[i]);
                var partNum2 = Convert.ToInt32(osParts2[i]);

                if (partNum1 < partNum2)
                {
                    return -1;
                }
                else if (partNum1 > partNum2)
                {
                    return 1;
                }
            }

            return 0;
        }

        private void DeviceManage_Click(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)e.OriginalSource;
            Process.Start(link.NavigateUri.AbsoluteUri);
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            _wifiRefreshTimer.Stop();
            try
            {
                var frameworkElement = sender as FrameworkElement;

                DiscoveredDevice device = null;

                if (frameworkElement != null)
                {
                    device = frameworkElement.DataContext as DiscoveredDevice;
                }

                if (device != null)
                {
                    var dlg = new WindowWarning()
                    {
                        Header = Strings.Strings.ConnectAlertTitle,
                        Message = Strings.Strings.ConnectAlertMessage,
                        Owner = Window.GetWindow(this)
                    };

                    var confirmation = dlg.ShowDialog();

                    if (confirmation.HasValue && confirmation.Value)
                    {
                        App.TelemetryClient.TrackEvent("WiFiButtonClick", new Dictionary<string, string>()
                        {
                            { "DeviceId", device.UniqueId.ToString() },
                            { "DeviceArchitecture", device.Architecture },
                            { "DeviceOSVersion", device.OsVersion },
                            { "DeviceModel", device.DeviceModel }
                        });

                        _wifiPage = new PageWifi(_navigationFrame, this._softwareAccessPoint, device);

                        _navigationFrame.Navigate(_wifiPage);

                        return;
                    }
                }
            }
            finally
            {
                _wifiRefreshTimer.Start();
            }
        }

        private void ButtonPortal_Click(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;

            DiscoveredDevice device = null;

            if (frameworkElement != null)
            {
                device = frameworkElement.DataContext as DiscoveredDevice;
            }

            if (device?.Manage != null)
            {
                App.TelemetryClient.TrackEvent("PortalButtonClick", new Dictionary<string, string>()
                {
                    { "DeviceId", device.UniqueId.ToString() },
                    { "DeviceArchitecture", device.Architecture },
                    { "DeviceOSVersion", device.OsVersion },
                    { "DeviceModel", device.DeviceModel }
                });

                var deviceUrl = "http://" + device.IpAddress + ":8080"; //Append the port number as well for the URL to work

                Process.Start(new ProcessStartInfo(deviceUrl));
            }
        }

        private void ButtonManage_Click(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;

            DiscoveredDevice device = null;

            if (frameworkElement != null)
            {
                device = frameworkElement.DataContext as DiscoveredDevice;
            }

            if (device != null)
            {
                App.TelemetryClient.TrackEvent("ManageButtonClick", new Dictionary<string, string>()
                {
                    { "DeviceId", device.UniqueId.ToString() },
                    { "DeviceArchitecture", device.Architecture },
                    { "DeviceOSVersion", device.OsVersion },
                    { "DeviceModel", device.DeviceModel }
                });

                _navigationFrame.Navigate(new PageDeviceConfiguration(_navigationFrame, device));
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void UpdateFilter()
        {
            ComboBoxItem item = comboBoxFilter.SelectedItem as ComboBoxItem;

            if (item != null && ListViewDevices != null)
            {
                string filterText = item.Tag as string;
                if (filterText == "New")
                    ListViewDevices.ItemsSource = _discoveryHelper.NewDevices;
                else
                    ListViewDevices.ItemsSource = _discoveryHelper.AllDevices;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFilter();
        }
    }
}
