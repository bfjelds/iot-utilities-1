using DeviceCenter.WlanAPIs;
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

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for ViewDevicesPage.xaml
    /// </summary>
    public partial class ViewDevicesPage : Page
    {
        private DispatcherTimer telemetryTimer = new DispatcherTimer();
        private DiscoveredDevice newestBuildDevice, oldestBuildDevice;
        private DeviceDiscoveryService deviceDiscoverySvc;
        private ObservableCollection<DiscoveredDevice> devices = new ObservableCollection<DiscoveredDevice>();

        private SoftAPHelper softwareAccessPoint = new SoftAPHelper();
        private DispatcherTimer wifiRefreshTimer = new DispatcherTimer();
        private ConcurrentDictionary<string, WlanInterop.WlanAvailableNetwork> adhocNetworks = new ConcurrentDictionary<string, WlanInterop.WlanAvailableNetwork>();

        private Frame _navigationFrame;
        private PageWifi wifiPage;
        private bool connectedToAdhoc = false;

        ~ViewDevicesPage()
        {
            wifiRefreshTimer.Stop();
            if (connectedToAdhoc)
            {
                softwareAccessPoint.Disconnect();
            }
        }

        public ViewDevicesPage(Frame navigationFrame)
        {
            InitializeComponent();
            _navigationFrame = navigationFrame;

            newestBuildDevice = null;
            oldestBuildDevice = null;

            telemetryTimer.Interval = TimeSpan.FromSeconds(3);
            telemetryTimer.Tick += TelemetryTimer_Tick;

            deviceDiscoverySvc = new DeviceDiscoveryService();
            deviceDiscoverySvc.Discovered += MDNSDeviceDiscovered;
            deviceDiscoverySvc.Start();

            ListViewDevices.ItemsSource = devices;

            softwareAccessPoint.OnSoftAPDisconnected += SoftwareAccessPoint_OnSoftAPDisconnected;

            wifiRefreshTimer.Interval = TimeSpan.FromSeconds(10);
            wifiRefreshTimer.Tick += WifiRefreshTimer_Tick;

            RefreshWifiAsync();

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        private void SoftwareAccessPoint_OnSoftAPDisconnected()
        {
            connectedToAdhoc = false;
        }

        private void ListViewDevices_Unloaded(object sender, RoutedEventArgs e)
        {
            wifiRefreshTimer.Stop();
        }

        private void RefreshWifiAsync()
        {
            wifiRefreshTimer.Stop();

            try
            {
                var list = softwareAccessPoint.GetAvailableNetworkList();

                if (list == null)
                    return;

                foreach (WlanInterop.WlanAvailableNetwork accessPoint in list)
                {
                    adhocNetworks.GetOrAdd(accessPoint.SSIDString, (key) =>
                    {
                        var newDevice = new DiscoveredDevice(accessPoint)
                        {
                            DeviceName = key
                        };

                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            devices.Add(newDevice);
                        }));

                        return accessPoint;
                    });
                }
            }
            finally
            {
                wifiRefreshTimer.Start();
            }
        }

        private void WifiRefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshWifiAsync();
        }

        private void TelemetryTimer_Tick(object sender, EventArgs e)
        {
            // Only send a telemetry event if we've found build information
            if (oldestBuildDevice != null && newestBuildDevice != null)
            {
                int deviceCount = deviceDiscoverySvc.DevicesDiscovered().Count;

                Debug.WriteLine("Sending telemetry event... ");
                Debug.WriteLine("Max OS Version: " + newestBuildDevice.OSVersion);
                Debug.WriteLine("Min OS Version: " + oldestBuildDevice.OSVersion);
                Debug.WriteLine("Number of devices: " + deviceCount);

                App.TelemetryClient.TrackEvent("DeviceDiscovery", new Dictionary<string, string>()
                {
                    { "oldestDeviceId", oldestBuildDevice.UniqueId.ToString() },
                    { "oldestBuildVersion", oldestBuildDevice.OSVersion },
                    { "newestDeviceId", newestBuildDevice.UniqueId.ToString() },
                    { "newestBuildVersion", newestBuildDevice.OSVersion },
                    { "numDevices", deviceCount.ToString() }
                });
            }

            telemetryTimer.Stop();
        }

        public void MDNSDeviceDiscovered(object sender, DiscoveredEventArgs args)
        {
            // EventArgs args should never be null, added a check just to be sure. 
            if (args == null)
            {
                return;
            }

            if (args.Info.Connection == DiscoveredDeviceInfo.ConnectionType.MDNS)
            {
                var newDevice = new DiscoveredDevice()
                {
                    DeviceName = args.Info.Name,
                    DeviceModel = args.Info.Location,
                    Architecture = args.Info.Architecture,
                    OSVersion = args.Info.OSVersion,
                    IPAddress = IPAddress.Parse(args.Info.Address),
                    UniqueId = args.Info.UniqueId,
                    Manage = new Uri(string.Format("http://administrator@{0}/", args.Info.Address)),
                    Authentication = DialogAuthenticate.GetSavedPassword(args.Info.Name)
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    devices.Add(newDevice);
                }));

                // Figure out which device has the latest build and the oldest build
                if (!string.IsNullOrWhiteSpace(newDevice.OSVersion))
                {
                    // Set initial value if null
                    if (newestBuildDevice == null && oldestBuildDevice == null)
                    {
                        newestBuildDevice = oldestBuildDevice = newDevice;
                    }

                    // Compare OS Versions
                    try
                    {
                        int compareResult = compareOsVersions(newDevice.OSVersion, newestBuildDevice.OSVersion);

                        if (compareResult > 0)
                        {
                            newestBuildDevice = newDevice;
                        }
                        else if (compareResult < 0)
                        {
                            oldestBuildDevice = newDevice;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }

                // Refresh delay until telemetry is sent
                telemetryTimer.Start();
            }
        }

        private int compareOsVersions(string osVersion1, string osVersion2)
        {
            if (osVersion1 == osVersion2)
            {
                return 0;
            }

            string[] osParts1 = osVersion1.Split('.');
            string[] osParts2 = osVersion2.Split('.');

            int numParts = (osParts1.Length < osParts2.Length) ? osParts1.Length : osParts2.Length;

            for (int i = 0; i < numParts; i++)
            {
                int partNum1 = Convert.ToInt32(osParts1[i]);
                int partNum2 = Convert.ToInt32(osParts2[i]);

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
                Hyperlink link = (Hyperlink)e.OriginalSource;
                Process.Start(link.NavigateUri.AbsoluteUri);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // TODO: handle errors
            }
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            wifiRefreshTimer.Stop();
            try
            {
                DiscoveredDevice device = ListViewDevices.SelectedItem as DiscoveredDevice;
                if (device != null)
                {
                    WindowWarning dlg = new WindowWarning()
                    {
                        Header = Strings.Strings.ConnectAlertTitle,
                        Message = Strings.Strings.ConnectAlertMessage
                    };

                    bool? confirmation = dlg.ShowDialog();
                    if (confirmation.HasValue && confirmation.Value)
                    {
                        wifiPage = new PageWifi(_navigationFrame, this.softwareAccessPoint, device);

                        _navigationFrame.Navigate(wifiPage);

                        return;
                    }
                }
            }
            finally
            {
                wifiRefreshTimer.Start();
            }
        }

        private void ButtonPortal_Click(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;

            DiscoveredDevice device = null;

            if (frameworkElement != null)
            {
                device = frameworkElement.DataContext as DiscoveredDevice;
            }

            if (device != null && device.Manage != null)
            {
                App.TelemetryClient.TrackEvent("PortalButtonClick", new Dictionary<string, string>()
                {
                    { "DeviceId", device.UniqueId.ToString() },
                    { "DeviceArchitecture", device.Architecture },
                    { "DeviceOSVersion", device.OSVersion },
                    { "DeviceModel", device.DeviceModel }
                });

                string deviceUrl = "http://" + device.IPAddress + ":8080"; //Append the port number as well for the URL to work

                Process.Start("IExplore.exe", deviceUrl);
            }
        }

        private void ButtonManage_Click(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;

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
                    { "DeviceOSVersion", device.OSVersion },
                    { "DeviceModel", device.DeviceModel }
                });

                _navigationFrame.Navigate(new PageDeviceConfiguration(_navigationFrame, device));
            }
        }

        private void ButtonAppInstall_Click(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;

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
                    { "DeviceOSVersion", device.OSVersion },
                    { "DeviceModel", device.DeviceModel }
                });

                _navigationFrame.Navigate(new SamplesPage(_navigationFrame, device));
            }
        }

    }
}
