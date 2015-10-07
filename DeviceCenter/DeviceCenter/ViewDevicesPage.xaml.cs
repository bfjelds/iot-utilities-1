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

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for ViewDevicesPage.xaml
    /// </summary>
    public partial class ViewDevicesPage : Page
    {
        //DispatcherTimer telemetryTimer;
        mDNSDiscoveredDevice newestBuildDevice, oldestBuildDevice;
        DeviceDiscoveryService deviceDiscoverySvc;
        ObservableCollection<mDNSDiscoveredDevice> devices = new ObservableCollection<mDNSDiscoveredDevice>();

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
                var newDevice = new mDNSDiscoveredDevice
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
        private void SetupDevice_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonManage_Click(object sender, RoutedEventArgs e)
        {

        }

        class mDNSDiscoveredDevice
        {
            public string DeviceName { get; set; }
            public string DeviceModel { get; set; }
            public string IPaddress { get; set; }
            public string OSVersion { get; set; }
            public string Architecture { get; set; }
            public Guid UniqueId { get; set; }
            public Uri Manage { get; set; }
            public string ManageText { get { return "Manage"; } }
            public string ConnectionText { get { return "Set up device"; } }
        }
    }
}
