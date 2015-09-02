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

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for ViewDevicesPage.xaml
    /// </summary>
    public partial class ViewDevicesPage : Page
    {
        public ViewDevicesPage()
        {
            InitializeComponent();
            var deviceDiscoverySvc = new DeviceDiscoveryService();
            deviceDiscoverySvc.Discovered += MDNSDeviceDiscovered;
            deviceDiscoverySvc.Start();
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
                var newDevice = new mDNSDiscoveredDevice
                {
                    DeviceName = args.Info.Name,
                    DeviceModel = args.Info.Location,
                    Architecture = args.Info.Architecture,
                    OSVersion = args.Info.OSVersion,
                    IPaddress = args.Info.Address
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => discoveredDevices.Items.Add(newDevice)));
            }
        }

        class mDNSDiscoveredDevice
        {
            public string DeviceName { get; set; }
            public string DeviceModel { get; set; }
            public string IPaddress { get; set; }
            public string OSVersion { get; set; }
            public string Architecture { get; set; }


        }
    }
}
