using Microsoft.Tools.Connectivity;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for PageAppDetails.xaml
    /// </summary>
    public partial class PageAppDetails : Page
    {
        private DeviceDiscoveryService deviceDiscoverySvc;
        private ObservableCollection<DiscoveredDevice> devices = new ObservableCollection<DiscoveredDevice>();

        public AppInformation AppItem { get; private set; }
        public PageAppDetails(AppInformation item)
        {
            InitializeComponent();

            this.AppItem = item;
            this.DataContext = this.AppItem;

            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Visible;

            deviceDiscoverySvc = new DeviceDiscoveryService();
            deviceDiscoverySvc.Discovered += DeviceDiscoverySvc_Discovered;
            deviceDiscoverySvc.Start();

            comboBoxDevices.ItemsSource = devices;
        }

        private void DeviceDiscoverySvc_Discovered(object sender, DiscoveredEventArgs args)
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
                    IPAddress = args.Info.Address,
                    UniqueId = args.Info.UniqueId,
                    Manage = new Uri(string.Format("http://administrator@{0}/", args.Info.Address))
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    devices.Add(newDevice);
                }));
            }
        }

        private void ButtonDeploy_Click(object sender, RoutedEventArgs e)
        {
            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeploying.Visibility = Visibility.Visible;
            PanelDeployed.Visibility = Visibility.Collapsed;
        }

        private void ButtonStopDeploy_Click(object sender, RoutedEventArgs e)
        {
            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Visible;
        }
    }
}
