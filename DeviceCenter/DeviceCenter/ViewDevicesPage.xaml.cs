using Microsoft.Tools.Connectivity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using DeviceCenter.Helper;
using WlanAPIs;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for ViewDevicesPage.xaml
    /// </summary>
    public partial class ViewDevicesPage : Page
    {
        private readonly DispatcherTimer _telemetryTimer = new DispatcherTimer();
        private DiscoveredDevice _newestBuildDevice, _oldestBuildDevice;
        private readonly DeviceDiscoveryService _deviceDiscoverySvc;
        private readonly ObservableCollection<DiscoveredDevice> _devices = new ObservableCollection<DiscoveredDevice>();

        private readonly SoftApHelper _softwareAccessPoint = new SoftApHelper();
        private readonly DispatcherTimer _wifiRefreshTimer = new DispatcherTimer();
        private readonly ConcurrentDictionary<string, WlanInterop.WlanAvailableNetwork> _adhocNetworks = new ConcurrentDictionary<string, WlanInterop.WlanAvailableNetwork>();

        private readonly Frame _navigationFrame;
        private PageWifi _wifiPage;
        private bool _connectedToAdhoc = false;

        ~ViewDevicesPage()
        {
            _wifiRefreshTimer.Stop();
            if (_connectedToAdhoc)
            {
                _softwareAccessPoint.Disconnect();
            }
        }

        public ViewDevicesPage(Frame navigationFrame)
        {
            InitializeComponent();
            _navigationFrame = navigationFrame;

            _newestBuildDevice = null;
            _oldestBuildDevice = null;

            _telemetryTimer.Interval = TimeSpan.FromSeconds(3);
            _telemetryTimer.Tick += TelemetryTimer_Tick;

            _deviceDiscoverySvc = new DeviceDiscoveryService();
            _deviceDiscoverySvc.Discovered += MdnsDeviceDiscovered;
            _deviceDiscoverySvc.Start();

            ListViewDevices.ItemsSource = _devices;

            _softwareAccessPoint.OnSoftApDisconnected += SoftwareAccessPoint_OnSoftAPDisconnected;

            _wifiRefreshTimer.Interval = TimeSpan.FromSeconds(10);
            _wifiRefreshTimer.Tick += WifiRefreshTimer_Tick;

            RefreshWifiAsync();

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        private void SoftwareAccessPoint_OnSoftAPDisconnected()
        {
            _connectedToAdhoc = false;
        }

        private void ListViewDevices_Unloaded(object sender, RoutedEventArgs e)
        {
            _wifiRefreshTimer.Stop();
        }

        private void RefreshWifiAsync()
        {
            _wifiRefreshTimer.Stop();

            try
            {
                IList<WlanInterop.WlanAvailableNetwork> list = null;
                try
                {
                    list = _softwareAccessPoint.GetAvailableNetworkList();
                }
                catch (WLanException)
                {
                    // probably not connected to wifi
                }

                if (list == null)
                    return;

                foreach (WlanInterop.WlanAvailableNetwork accessPoint in list)
                {
                    _adhocNetworks.GetOrAdd(accessPoint.SsidString, (key) =>
                    {
                        var newDevice = new DiscoveredDevice(accessPoint)
                        {
                            DeviceName = key
                        };

                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            _devices.Add(newDevice);
                        }));

                        return accessPoint;
                    });
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
                int deviceCount = _deviceDiscoverySvc.DevicesDiscovered().Count;

                Debug.WriteLine("Sending telemetry event... ");
                Debug.WriteLine("Max OS Version: " + _newestBuildDevice.OsVersion);
                Debug.WriteLine("Min OS Version: " + _oldestBuildDevice.OsVersion);
                Debug.WriteLine("Number of devices: " + deviceCount);

                App.TelemetryClient.TrackEvent("DeviceDiscovery", new Dictionary<string, string>()
                {
                    { "oldestDeviceId", _oldestBuildDevice.UniqueId.ToString() },
                    { "oldestBuildVersion", _oldestBuildDevice.OsVersion },
                    { "newestDeviceId", _newestBuildDevice.UniqueId.ToString() },
                    { "newestBuildVersion", _newestBuildDevice.OsVersion },
                    { "numDevices", deviceCount.ToString() }
                });
            }

            _telemetryTimer.Stop();
        }

        public void MdnsDeviceDiscovered(object sender, DiscoveredEventArgs args)
        {
            // EventArgs args should never be null, added a check just to be sure. 

            if (args?.Info.Connection == DiscoveredDeviceInfo.ConnectionType.MDNS)
            {
                var newDevice = new DiscoveredDevice()
                {
                    DeviceName = args.Info.Name,
                    DeviceModel = args.Info.Location,
                    Architecture = args.Info.Architecture,
                    OsVersion = args.Info.OSVersion,
                    IpAddress = IPAddress.Parse(args.Info.Address),
                    UniqueId = args.Info.UniqueId,
                    Manage = new Uri($"http://administrator@{args.Info.Address}/"),
                    Authentication = DialogAuthenticate.GetSavedPassword(args.Info.Name)
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    _devices.Add(newDevice);
                }));

                // Figure out which device has the latest build and the oldest build
                if (!string.IsNullOrWhiteSpace(newDevice.OsVersion))
                {
                    // Set initial value if null
                    if (_newestBuildDevice == null && _oldestBuildDevice == null)
                    {
                        _newestBuildDevice = _oldestBuildDevice = newDevice;
                    }

                    // Compare OS Versions
                    try
                    {
                        if (_newestBuildDevice != null)
                        {
                            var compareResult = compareOsVersions(newDevice.OsVersion, _newestBuildDevice.OsVersion);

                            if (compareResult > 0)
                            {
                                _newestBuildDevice = newDevice;
                            }
                            else if (compareResult < 0)
                            {
                                _oldestBuildDevice = newDevice;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }

                // Refresh delay until telemetry is sent
                _telemetryTimer.Start();
            }
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
            try
            {
                var link = (Hyperlink)e.OriginalSource;
                Process.Start(link.NavigateUri.AbsoluteUri);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // TODO: handle errors
            }
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
                        Message = Strings.Strings.ConnectAlertMessage
                    };

                    var confirmation = dlg.ShowDialog();
                    if (confirmation.HasValue && confirmation.Value)
                    {
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

                Process.Start("IExplore.exe", deviceUrl);
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

        private void ButtonAppInstall_Click(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;

            DiscoveredDevice device = null;

            if (frameworkElement != null)
            {
                device = frameworkElement.DataContext as DiscoveredDevice;
            }

            if (device != null)
            {
                App.TelemetryClient.TrackEvent("AppInstallButtonClick", new Dictionary<string, string>()
                {
                    { "DeviceId", device.UniqueId.ToString() },
                    { "DeviceArchitecture", device.Architecture },
                    { "DeviceOSVersion", device.OsVersion },
                    { "DeviceModel", device.DeviceModel }
                });

                _navigationFrame.Navigate(new SamplesPage(_navigationFrame, device));
            }
        }

    }
}
