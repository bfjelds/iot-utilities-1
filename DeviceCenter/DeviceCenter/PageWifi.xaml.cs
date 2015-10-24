using DeviceCenter.DataContract;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DeviceCenter
{
    public class WifiEntry : INotifyPropertyChanged
    {
        private AvailableNetwork network;
        private const string WifiIcons = "";
        private Frame navigationFrame;
        private bool needPassword;
        private WebBRest webbRequest;
        private string adapterGUID;

        public WifiEntry(Frame navigationFrame, string adapterGUID, AvailableNetwork ssid, WebBRest webbRequest)
        {
            this.navigationFrame = navigationFrame;
            this.network = ssid;
            this.webbRequest = webbRequest;
            this.adapterGUID = adapterGUID;

            this.Active = false;
            this.ShowConnect = Visibility.Collapsed;
            this.NeedPassword = Visibility.Collapsed;
            this.ShowExpanded = Visibility.Collapsed;
            this.SavePassword = false;

            this.needPassword = this.network.SecurityEnabled;
            this.Name = this.network.SSID;
            this.Secure = (this.needPassword) ? Strings.Strings.LabelSecureWifi : Strings.Strings.LabelInsecureWifi;
            this.SignalStrength = this.network.SignalQuality / 20;
        }

        public string Name { get; private set; }
        public string Secure { get; private set; }
        public string Password { get; set; }
        public bool SavePassword { get; set; }
        public string WifiIcon
        {
            get
            {
                switch (SignalStrength)
                {
                    case 1: return WifiIcons.Substring(1, 1);
                    case 2: return WifiIcons.Substring(2, 1);
                    case 3: return WifiIcons.Substring(3, 1);
                    case 4: return WifiIcons.Substring(4, 1);
                    default: return WifiIcons.Substring(0, 1);
                }
            }
        }
        public int SignalStrength { get; set; }

        public void Expand()
        {
            this.Active = true;
            ShowConnect = Visibility.Visible;
            NeedPassword = Visibility.Collapsed;
            ShowExpanded = Visibility.Visible;

            OnPropertyChanged("ShowConnect");
            OnPropertyChanged("Active");
            OnPropertyChanged("NeedPassword");
            OnPropertyChanged("ShowExpanded");
        }

        public void Collapse()
        {
            this.Active = false;
            ShowConnect = Visibility.Collapsed;
            NeedPassword = Visibility.Collapsed;
            ShowExpanded = Visibility.Collapsed;

            OnPropertyChanged("ShowConnect");
            OnPropertyChanged("Active");
            OnPropertyChanged("NeedPassword");
            OnPropertyChanged("ShowExpanded");
        }

        public void StartConnect()
        {
            if (this.needPassword)
            {
                this.NeedPassword = Visibility.Visible;
                this.ShowConnect = Visibility.Collapsed;
                this.EnableSecureConnect = false;

                OnPropertyChanged("EnableSecureConnect");
                OnPropertyChanged("NeedPassword");
                OnPropertyChanged("ShowConnect");
            }
            else
            {
                DoConnectAsync(string.Empty);
            }
        }

        public async void DoConnectAsync(string password)
        {
            await webbRequest.ConnectToNetworkAsync(adapterGUID, this.network.SSID, password);
            this.navigationFrame.GoBack();
        }

        public void AllowSecure(bool enabled)
        {
            this.EnableSecureConnect = enabled;
            OnPropertyChanged("EnableSecureConnect");
        }

        public bool EnableSecureConnect { get; private set; }

        public void CancelSecure()
        {
            NeedPassword = Visibility.Collapsed;
            ShowConnect = Visibility.Visible;

            OnPropertyChanged("NeedPassword");
            OnPropertyChanged("ShowConnect");
        }

        public bool Active { get; private set; }
        public Visibility ShowConnect { get; private set; }
        public Visibility ShowConnecting { get; private set; }
        public Visibility NeedPassword { get; private set; }
        public Visibility ShowExpanded { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    /// <summary>
    /// Interaction logic for PageWifi.xaml
    /// </summary>
    public partial class PageWifi : Page
    {
        private Frame navigationFrame;
        private DiscoveredDevice device;
        private SoftAPHelper wifiManager;
        private DispatcherTimer delayStart;

        public PageWifi(Frame navigationFrame, SoftAPHelper wifiManager, DiscoveredDevice device)
        {
            InitializeComponent();

            this.device = device;
            this.navigationFrame = navigationFrame;
            this.wifiManager = wifiManager;

            ListViewWifi.SelectionChanged += ListViewWifi_SelectionChanged;
            progressWaiting.Visibility = Visibility.Visible;

            App.TelemetryClient.TrackPageView(this.GetType().Name);
            LabelDeviceName.Text = device.DeviceName;

            delayStart = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(5),
                IsEnabled = true
            };
            delayStart.Tick += delayStartTimer_Tick;
        }

        private bool connected = false;

        private void ReturnAsError(string message)
        {
            wifiManager.Disconnect();

            MessageBox.Show(message, Strings.Strings.AppNameDisplay, MessageBoxButton.OK, MessageBoxImage.Asterisk);

            navigationFrame.GoBack();
        }

        private async void delayStartTimer_Tick(object sender, EventArgs e)
        {
            delayStart.Stop();

            bool connected = await wifiManager.ConnectAsync(device.WifiInstance, "password");
            if (connected)
            {
                this.connected = true;

                UserInfo authentication = DialogAuthenticate.GetSavedPassword(this.device.WifiInstance.SSIDString);

                try
                {
                    await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
                    {
                        ListViewWifi.ItemsSource = await QueryWifiAsync(device);
                    }));
                }
                catch (Exception error)
                {
                    ReturnAsError(error.Message);

                }
            }
            else
            {
                ReturnAsError(Strings.Strings.MessageUnableToGetWifi);
            }
        }

        private async Task<ObservableCollection<WifiEntry>> QueryWifiAsync(DiscoveredDevice device)
        {
            ObservableCollection<WifiEntry> result = new ObservableCollection<WifiEntry>();
            UserInfo userInfo = DialogAuthenticate.GetSavedPassword(device.DeviceName);

            IPAddress ip = System.Net.IPAddress.Parse("192.168.173.1"); // default on wifi
            WebBRest webbRequest = new WebBRest(ip, DialogAuthenticate.GetSavedPassword(ip.ToString()));

            var adapters = await webbRequest.GetWirelessAdaptersAsync();

            AvailableNetworks networks = await webbRequest.GetAvaliableNetworkAsync(adapters.Items[0].GUID);
            foreach (AvailableNetwork ssid in networks.Items)
            {
                result.Add(new WifiEntry(navigationFrame, adapters.Items[0].GUID, ssid, webbRequest));
            }

            progressWaiting.Visibility = Visibility.Collapsed;

            return result;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ListViewDevices_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ListViewDevices_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.connected)
                wifiManager.Disconnect();
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewWifi.SelectedItem != null)
            {
                WifiEntry entry = ListViewWifi.SelectedItem as WifiEntry;
                if (entry != null)
                    entry.StartConnect();

                if (entry.NeedPassword == Visibility.Visible)
                {
                    PasswordBox editor = FindPasswordEdit(e.Source);
                    if (editor != null)
                    {
                        editor.Password = string.Empty;
                        editor.Focus();
                    }
                }
            }
        }

        // MVVM aka Binding doesn't work on Password controls, must go outside to use
        private PasswordBox FindPasswordEdit(object control)
        {
            FrameworkElement button = control as FrameworkElement;
            if (button != null)
            {
                Panel parentPanel = button.Parent as Panel;
                var findControl = parentPanel.FindName("textboxWifiPassword");
                if (findControl != null)
                {
                    return findControl as PasswordBox;
                }
            }

            return null;
        }

        private void ButtonConnectSecure_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewWifi.SelectedItem != null)
            {
                WifiEntry entry = ListViewWifi.SelectedItem as WifiEntry;
                if (entry != null)
                {
                    PasswordBox editor = FindPasswordEdit(e.Source);
                    if (editor != null)
                        entry.DoConnectAsync(editor.Password);
                }
            }
        }

        private void ButtonCancelSecure_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewWifi.SelectedItem != null)
            {
                WifiEntry entry = ListViewWifi.SelectedItem as WifiEntry;
                if (entry != null)
                    entry.CancelSecure();
            }
        }

        private void ListViewWifi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var cur in e.RemovedItems)
                (cur as WifiEntry).Collapse();
            foreach (var cur in e.AddedItems)
                (cur as WifiEntry).Expand();
        }

        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            (e.Source as WifiEntry).Expand();
        }

        private void ListViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            (e.Source as WifiEntry).Expand();
        }

       private void textboxWifiPassword_PasswordChanged(object sender, RoutedEventArgs e)
       {
            if (ListViewWifi.SelectedItem != null)
            {
                WifiEntry entry = ListViewWifi.SelectedItem as WifiEntry;
                if (entry != null)
                {
                    PasswordBox edit = sender as PasswordBox;
                    if (edit != null)
                        entry.AllowSecure(edit.Password.Length > 0);
                }
            }
        }

        private void textboxWifiPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // todo handle escape and enter
        }
    }
}
