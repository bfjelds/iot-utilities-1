using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Tools.Connectivity;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Onboarding;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for ViewDevicesPage.xaml
    /// </summary>
    public partial class ViewDevicesPage : Page
    {
        //DispatcherTimer telemetryTimer;
        private DiscoveredDevice newestBuildDevice, oldestBuildDevice;
        private DeviceDiscoveryService deviceDiscoverySvc;
        private ObservableCollection<DiscoveredDevice> devices = new ObservableCollection<DiscoveredDevice>();

        private class AdhocNetwork
        {
            public AdhocNetwork(IWifi wifi)
            {
                this.Wifi = wifi;
            }

            public override string ToString()
            {
                return this.Wifi.GetSSID();
            }

            public IWifi Wifi { get; private set; }
        }

        private ConcurrentDictionary<string, AdhocNetwork> adhocNetworks = new ConcurrentDictionary<string, AdhocNetwork>();

        private IOnboardingManager wifiManager;
        private DispatcherTimer wifiRefreshTimer;

        public ViewDevicesPage()
        {
            InitializeComponent();
            /*
            newestBuildDevice = null;
            oldestBuildDevice = null;

            telemetryTimer = new DispatcherTimer();
            telemetryTimer.Interval = TimeSpan.FromSeconds(3);
            telemetryTimer.Tick += TelemetryTimer_Tick;
            */

            deviceDiscoverySvc = new DeviceDiscoveryService();
            deviceDiscoverySvc.Discovered += MDNSDeviceDiscovered;
            deviceDiscoverySvc.Start();

            ListViewDevices.ItemsSource = devices;

            wifiManager = new OnboardingManager();
            wifiManager.Init();

            wifiRefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            wifiRefreshTimer.Tick += WifiRefreshTimer_Tick;
            RefreshWifiAsync();

            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.MainWindow.Closing -= MainWindow_Closing;

            wifiManager.Shutdown();
        }

        private async void RefreshWifiAsync()
        {
            wifiRefreshTimer.Stop();
            try
            {
                await Task.Run((Action)(() =>
                {
                    WifiList list = null;
                    try
                    {
                        list = wifiManager.GetOnboardingNetworks();

                        if (list == null)
                            return;

                        uint size = list.Size();

                        for (uint i = 0; i < size; i++)
                        {
                            IWifi item = list.GetItem(i);

                            AdhocNetwork ssid = adhocNetworks.GetOrAdd(item.GetSSID(), (key)  =>
                            {
                                var newDevice = new DiscoveredDevice(DiscoveredDevice.NetworkType.adhoc)
                                {
                                    DeviceName = item.GetSSID()
                                };

                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    devices.Add(newDevice);
                                }));

                                return new AdhocNetwork(item);
                            });
                        }
                    }
                    catch (COMException /*ex*/)
                    {
                        // TODO handle errors
                        //Dispatcher.Invoke(() => { statusTextBlock.Text = "Failed to find onboardees. HRESULT: " + ex.HResult; });
                    }
                    finally
                    {
                        if (list != null)
                        {
                            Marshal.ReleaseComObject(list);
                            list = null;
                        }
                    }
                }));
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

        /*
        private void TelemetryTimer_Tick(object sender, EventArgs e)
        {
            // Only send a telemetry event if we've found build information
            if (oldestBuildDevice != null && newestBuildDevice != null)
            {
                Debug.WriteLine("Sending telemetry event... ");
                Debug.WriteLine("Max OS Version: " + newestBuildDevice.OSVersion);
                Debug.WriteLine("Min OS Version: " + oldestBuildDevice.OSVersion);

                TelemetryHelper.eventLogger.Write(TelemetryHelper.DeviceDiscoveryEvent, TelemetryHelper.TelemetryInfoOption, new
                {
                    oldestBuildVersion = oldestBuildDevice.OSVersion,
                    newestBuildVersion = newestBuildDevice.OSVersion,
                    newestDeviceId = newestBuildDevice.UniqueId,
                    oldestDeviceId = oldestBuildDevice.UniqueId,
                    numDevices = deviceDiscoverySvc.DevicesDiscovered().Count
                });
            }

            telemetryTimer.Stop();
        }
        */

        public void MDNSDeviceDiscovered(object sender, DiscoveredEventArgs args)
        {
            // EventArgs args should never be null, added a check just to be sure. 
            if (args == null)
            {
                return;
            }

            if (args.Info.Connection == DiscoveredDeviceInfo.ConnectionType.MDNS)
            {
                var newDevice = new DiscoveredDevice(DiscoveredDevice.NetworkType.ethernet)
                {
                    DeviceName = args.Info.Name,
                    DeviceModel = args.Info.Location,
                    Architecture = args.Info.Architecture,
                    OSVersion = args.Info.OSVersion,
                    IPaddress = args.Info.Address,
                    UniqueId = args.Info.UniqueId,
                    Manage = new Uri(string.Format("http://administrator@{0}/", args.Info.Address))
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    devices.Add(newDevice);
                }));

                // Figure out what device has the latest build and the oldest build
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
                //telemetryTimer.Start();
            }
        }

        private int compareOsVersions(string osVersion1, string osVersion2)
        {
            if(osVersion1 == osVersion2)
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

                if(partNum1 < partNum2)
                {
                    return -1;
                }
                else if(partNum1 > partNum2)
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
            catch(System.ComponentModel.Win32Exception)
            {
                // TODO: handle errors
            }
        }
        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonManage_Click(object sender, RoutedEventArgs e)
        {

        }

        public class DiscoveredDevice
        {
            public enum NetworkType { ethernet, adhoc };
            public DiscoveredDevice(NetworkType network)
            {
                this.Network = network;
                switch (network)
                {
                    case NetworkType.ethernet:
                        this.ManageVisible = Visibility.Visible;
                        this.ConnectVisible = Visibility.Collapsed;
                        break;
                    case NetworkType.adhoc:
                        this.ManageVisible = Visibility.Collapsed;
                        this.ConnectVisible = Visibility.Visible;
                        break;
                }
            }

            public NetworkType Network { get; private set; }
            public string DeviceName { get; set; }
            public string DeviceModel { get; set; }
            public string IPaddress { get; set; }
            public string OSVersion { get; set; }
            public string Architecture { get; set; }
            public Guid UniqueId { get; set; }
            public Uri Manage { get; set; }
            public Visibility ManageVisible { get; private set; }
            public Visibility ConnectVisible { get; private set; }
            
        }
    }
}
