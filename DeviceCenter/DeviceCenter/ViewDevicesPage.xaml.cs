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

        private readonly PageFlow _pageFlow;

        private DateTime _lastTelemetryEventTime = DateTime.Now.AddDays(-1);

        public ViewDevicesPage(PageFlow pageFlow)
        {
            // initialize parameters
            this._pageFlow = pageFlow;
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
            _softwareAccessPoint.OnWlanScanComplete += SoftwareAccessPoint_OnWlanScanComplete;

            // Get avaliable wifi list once at startup
            _softwareAccessPoint.GetAvailableNetworkList();

            // Set up time out for sending telemetry - this timer waits 3 seconds after the last device is seen before sending a telemetry event
            _telemetryTimer.Interval = TimeSpan.FromSeconds(5);
            _telemetryTimer.Tick += TelemetryTimer_Tick;
            _discoveryHelper.AllDevices.CollectionChanged += AllDevices_CollectionChanged;

            Sort(_lastDirection, "DeviceName", "IpAddress");
        }

        private void SoftwareAccessPoint_OnWlanScanComplete(object sender, WlanScanCompleteArgs e)
        {
            _discoveryHelper.RefreshAdhocDevices(e.AvaliableNetworks);
        }

        ~ViewDevicesPage()
        {
            _softwareAccessPoint.DisconnectIfNeeded();

            DiscoveryHelper.Release();
            _discoveryHelper = null;
        }

        private void AllDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DiscoveredDevice newDevice in e.NewItems)
                {
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
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }

                // Refresh the timeout for sending telemetry
                _telemetryTimer.Start();
            }
        }

        private void TelemetryTimer_Tick(object sender, EventArgs e)
        {
            // Only send telemetry information if it's been more than 30 minutes since the last event was sent
            if (_lastTelemetryEventTime < DateTime.Now.AddMinutes(-30))
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

                    _lastTelemetryEventTime = DateTime.Now;
                }
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
            try
            {
                Process.Start(link.NavigateUri.AbsoluteUri);
            }
            catch(Exception ex)
            {
                MessageBox.Show(
                        ex.Message,
                        LocalStrings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
            }
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Need to get the Text control for the hyperlink, this contains the data binding.  Should never be null.
                var hyperLink = sender as Hyperlink;
                if (hyperLink == null)
                    return;

                var frameworkElement = hyperLink.Parent as FrameworkElement;

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

                        _pageFlow.Navigate(typeof(PageWifi), this._softwareAccessPoint, device);

                        return;
                    }
                }
            }
            finally
            {
            }
        }

        private void ButtonPortal_Click(object sender, RoutedEventArgs e)
        {
            // Need to get the Text control for the hyperlink, this contains the data binding.  Should never be null.
            var hyperLink = sender as Hyperlink;
            if (hyperLink == null)
                return;

            var frameworkElement = hyperLink.Parent as FrameworkElement;

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

                try
                {
                    Process.Start(new ProcessStartInfo(deviceUrl));
                }
                catch(Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        LocalStrings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                }
            }
        }

        private void ButtonManage_Click(object sender, RoutedEventArgs e)
        {
            // Need to get the Text control for the hyperlink, this contains the data binding.  Should never be null.
            var hyperLink = sender as Hyperlink;
            if (hyperLink == null)
                return;

            var frameworkElement = hyperLink.Parent as FrameworkElement;

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

                _pageFlow.Navigate(typeof(PageDeviceConfiguration), device);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            App.TelemetryClient.TrackEvent("LearnMoreHyperLinkClick", new Dictionary<string, string>());

            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
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

        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void Sort(ListSortDirection direction, params string[] sortByList)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(ListViewDevices.ItemsSource);

            using (dataView.DeferRefresh())
            {
                dataView.SortDescriptions.Clear();

                foreach (var sortBy in sortByList)
                {
                    SortDescription sd = new SortDescription(sortBy, direction);
                    dataView.SortDescriptions.Add(sd);
                }
            }
        }

        private void ListViewDevices_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    // callback will provide the localized name of the column clicked.  Reverse
                    // it to get the column itself and sort it
                    string header = headerClicked.Column.Header as string;
                    if (header == Strings.Strings.DeviceListColName)
                        Sort(direction, "DeviceName", "IpAddress");

                    else if (header == Strings.Strings.DeviceListColType)
                        Sort(direction, "DeviceModel", "DeviceName", "IpAddress");

                    else if (header == Strings.Strings.DeviceListColIPAddress)
                        Sort(direction, "IpAddress");

                    else if (header == Strings.Strings.DeviceListOS)
                        Sort(direction, "OsVersion", "DeviceName", "IpAddress");

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFilter();
        }
    }
}
