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
using DeviceCenter.Helper;

namespace DeviceCenter
{
    public class WifiEntry : INotifyPropertyChanged
    {
        // This is the delay from sending the connect to wifi rest request to pop up the dialog
        // that asks user to reboot their device, set to 120s based on the testing result on my MBM
        // this delay is for the wifi profile to flush to the disk on the device
        private readonly TimeSpan Wifi_Persist_Profile_WaitTime = TimeSpan.FromSeconds(120);

        private readonly AvailableNetwork _network;
        private const string WifiIcons = "";
        private readonly PageFlow _pageFlow;
        private readonly bool _needPassword;
        private readonly DiscoveredDevice _device;
        private readonly string _adapterGuid;
        private readonly PageWifi _parent;

        public WifiEntry(PageWifi parent, PageFlow pageFlow, string adapterGuid, AvailableNetwork ssid, DiscoveredDevice device, Visibility showConnecting = Visibility.Hidden)
        {
            this._pageFlow = pageFlow;
            this._network = ssid;
            this._device = device;
            this.ShowConnecting = showConnecting;
            this._adapterGuid = adapterGuid;
            this._parent = parent;

            this.Active = false;
            this.ShowConnect = Visibility.Collapsed;
            this.NeedPassword = Visibility.Collapsed;
            this.ShowExpanded = Visibility.Collapsed;
            this.WaitingToConnect = Visibility.Collapsed;
            this.SavePassword = false;

            this._needPassword = this._network.SecurityEnabled;
            this.Name = this._network.SSID;
            this.Secure = (this._needPassword) ? Strings.Strings.LabelSecureWifi : Strings.Strings.LabelInsecureWifi;
            this.SignalStrength = this._network.SignalQuality / 20;
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
                    case 0:
                    case 1: return WifiIcons.Substring(0, 1);
                    case 2: return WifiIcons.Substring(1, 1);
                    case 3: return WifiIcons.Substring(2, 1);
                    case 4: return WifiIcons.Substring(3, 1);
                    case 5: return WifiIcons.Substring(4, 1);
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
            ReadyToConnect = true;

            OnPropertyChanged(nameof(ShowConnect));
            OnPropertyChanged(nameof(Active));
            OnPropertyChanged(nameof(NeedPassword));
            OnPropertyChanged(nameof(ShowExpanded));
            OnPropertyChanged(nameof(ReadyToConnect));
        }

        public void Collapse()
        {
            this.Active = false;
            ShowConnect = Visibility.Collapsed;
            NeedPassword = Visibility.Collapsed;
            ShowExpanded = Visibility.Collapsed;

            OnPropertyChanged(nameof(ShowConnect));
            OnPropertyChanged(nameof(Active));
            OnPropertyChanged(nameof(NeedPassword));
            OnPropertyChanged(nameof(ShowExpanded));
        }

        public async Task StartConnect()
        {
            if (this._needPassword)
            {
                // don't connect, show the password screen
                this.NeedPassword = Visibility.Visible;
                this.ShowConnect = Visibility.Collapsed;
                this.EnableSecureConnect = false;

                OnPropertyChanged(nameof(EnableSecureConnect));
                OnPropertyChanged(nameof(NeedPassword));
                OnPropertyChanged(nameof(ShowConnect));
            }
            else
            {
                // do connect now
                await DoConnect(string.Empty);
            }
        }

        public async Task DoConnect(string password)
        {
            this.ReadyToConnect = false;
            OnPropertyChanged(nameof(ReadyToConnect));

            await ConnectDeviceToWifi(password);
        }

        private async Task ConnectDeviceToWifi(string password)
        {
            this.WaitingToConnect = Visibility.Visible;
            this.ShowConnect = Visibility.Hidden;
            this.ReadyToConnect = false;
            this.EnableSecureConnect = false;
            var webbRequest = WebBRest.Instance;

            OnPropertyChanged(nameof(EnableSecureConnect));
            OnPropertyChanged(nameof(WaitingToConnect));
            OnPropertyChanged(nameof(ReadyToConnect));
            OnPropertyChanged(nameof(ShowConnect));

            Collapse();

            bool connectSuccess = false;

            try
            {
                // 1) send connect to network rest request
                connectSuccess = await webbRequest.ConnectToNetworkAsync(_device, _adapterGuid, _network.SSID, password);
            }
            catch (WebException webException)
            {
                // 1.1 wrong password, stay on the same page to allow user to retry
                var response = webException.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Debug.WriteLine("ConnectDeviceToWifi: Wrong password");
                    MessageBox.Show(Strings.Strings.MessageBadWifiPassword, Strings.Strings.AppNameDisplay, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    this.Active = true;
                    NeedPassword = Visibility.Visible;
                    ShowExpanded = Visibility.Visible;
                    WaitingToConnect = Visibility.Collapsed;

                    OnPropertyChanged(nameof(Active));
                    OnPropertyChanged(nameof(NeedPassword));
                    OnPropertyChanged(nameof(ShowExpanded));
                    OnPropertyChanged(nameof(WaitingToConnect));

                    return;
                }
                else
                {
                    // 1.2) other errors, pop up dialog to ask user to reset manually 
                    // after {Wifi_Persist_Profile_WaitTime} seconds
                    Debug.WriteLine($"Error connecting, {webException.Message}");
                    Debug.WriteLine(webException.ToString());

                    await Task.Delay(Wifi_Persist_Profile_WaitTime);

                    MessageBox.Show(Strings.Strings.WiFiMayBeConfigured,
                        Strings.Strings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }

            // 2) send a restart request to device since EBoot on device fails to run on new network.
            // the eboot issue should be fixed in RS1.the reset won't be necessary afterwards
            if (connectSuccess)
            {
                var restartTask = webbRequest.RestartAsync(_device);
                var timeoutTask = Task.Delay(Wifi_Persist_Profile_WaitTime);

                var resultTask = await Task.WhenAny(restartTask, timeoutTask);
                if (resultTask == restartTask)
                {
                    if (resultTask.Status == TaskStatus.RanToCompletion)
                    {
                        var restartResult = restartTask.Result;

                        // 2.1) restart request succeeds
                        // 2.2) underlying connection is cut off, device is already restarting
                        if (restartResult == WebExceptionStatus.Success || restartResult == WebExceptionStatus.KeepAliveFailure)
                        {
                            MessageBox.Show(Strings.Strings.SuccessWifiConfigured,
                                Strings.Strings.AppNameDisplay,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            // 2.3) restart request fails, pop up dialog to ask user to reset manually 
                            // after {Wifi_Persist_Profile_WaitTime} seconds
                            await Task.Delay(Wifi_Persist_Profile_WaitTime);

                            MessageBox.Show(Strings.Strings.WiFiMayBeConfigured,
                                Strings.Strings.AppNameDisplay,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
                // 2.4 timeout, no response after {Wifi_Persist_Profile_WaitTime} seconds
                else
                {
                    webbRequest.TerminateAnyWebBCall();

                    MessageBox.Show(Strings.Strings.WiFiMayBeConfigured,
                                    Strings.Strings.AppNameDisplay,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }
            }

            this._pageFlow.Close(this._parent);

            this.WaitingToConnect = Visibility.Collapsed;
            this.ReadyToConnect = true;
            this.EnableSecureConnect = true;
            this.ShowConnect = password == string.Empty ? Visibility.Visible : Visibility.Collapsed;

            OnPropertyChanged(nameof(EnableSecureConnect));
            OnPropertyChanged(nameof(WaitingToConnect));
            OnPropertyChanged(nameof(ReadyToConnect));
            OnPropertyChanged(nameof(ShowConnect));
        }

        public void AllowSecure(bool enabled)
        {
            this.ReadyToConnect = enabled;
            this.EnableSecureConnect = enabled;

            OnPropertyChanged(nameof(ReadyToConnect));
            OnPropertyChanged(nameof(EnableSecureConnect));
        }

        public bool EnableSecureConnect { get; private set; }
        public bool ReadyToConnect { get; private set; }

        public void CancelSecure()
        {
            NeedPassword = Visibility.Collapsed;
            ShowConnect = Visibility.Visible;
            ReadyToConnect = true;

            OnPropertyChanged(nameof(ReadyToConnect));
            OnPropertyChanged(nameof(NeedPassword));
            OnPropertyChanged(nameof(ShowConnect));
        }

        public bool Active { get; private set; }

        public Visibility ShowConnect { get; private set; }

        public Visibility ShowConnecting { get; private set; }

        public Visibility NeedPassword { get; private set; }

        public Visibility ShowExpanded { get; private set; }
        public Visibility WaitingToConnect { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

    /// <summary>
    /// Interaction logic for PageWifi.xaml
    /// </summary>
    public partial class PageWifi : Page
    {
        private readonly PageFlow _pageFlow;
        private readonly DiscoveredDevice _device;
        private readonly SoftApHelper _wifiManager;
        private readonly DispatcherTimer _delayStart;

        public PageWifi(PageFlow pageFlow, SoftApHelper wifiManager, DiscoveredDevice device)
        {
            InitializeComponent();

            this._device = device;
            this._pageFlow = pageFlow;
            this._pageFlow.PageChange += _pageFlow_PageChange;

            this._wifiManager = wifiManager;

            ListViewWifi.SelectionChanged += ListViewWifi_SelectionChanged;
            progressWaiting.Visibility = Visibility.Visible;

            App.TelemetryClient.TrackPageView(this.GetType().Name);
            LabelDeviceName.Text = device.DeviceName;

            _delayStart = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(SoftApHelper.PollDelay),
                IsEnabled = true
            };
            _delayStart.Tick += delayStartTimer_Tick;
        }

        ~PageWifi()
        {
            this._pageFlow.PageChange -= _pageFlow_PageChange;
        }

        private void _pageFlow_PageChange(object sender, PageChangeCancelEventArgs e)
        {
            if (e.CurrentPage == this)
                e.Close = true;
        }

        private void ReturnAsError(string message)
        {
            _wifiManager.DisconnectIfNeeded();

            MessageBox.Show(message, Strings.Strings.AppNameDisplay, MessageBoxButton.OK, MessageBoxImage.Asterisk);

            this._pageFlow.Close(this);
        }

        private async void delayStartTimer_Tick(object sender, EventArgs e)
        {
            _delayStart.Stop();

            var connected = await _wifiManager.ConnectAsync(_device.WifiInstance, SoftApHelper.SoftApPassword);
            if (connected)
            {
                var authentication = DialogAuthenticate.GetSavedPassword(this._device.WifiInstance.SsidString);
                var ip = System.Net.IPAddress.Parse(SoftApHelper.SoftApHostIp); // default on wifi

                _device.IpAddress = new IPAddressSortable(ip);
                _device.Authentication = authentication;

                try
                {
                    await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
                    {
                        ListViewWifi.ItemsSource = await QueryWifiAsync();

                        progressWaiting.Visibility = Visibility.Collapsed;
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

        private async Task<ObservableCollection<WifiEntry>> QueryWifiAsync()
        {
            var result = new ObservableCollection<WifiEntry>();

            try
            {
                var webbRequest = WebBRest.Instance;

                var adapters = await webbRequest.GetWirelessAdaptersAsync(_device);

                if (adapters != null && adapters.Items != null)
                {
                    var networks = await webbRequest.GetAvaliableNetworkAsync(_device, adapters.Items[0].GUID);
                    if (networks != null)
                    {
                        foreach (var ssid in networks.Items)
                        {
                            result.Add(new WifiEntry(this, _pageFlow, adapters.Items[0].GUID, ssid, _device));
                        }
                    }
                }
                else
                {
                    MessageBox.Show(Strings.Strings.MessageUnableToGetWifi, Strings.Strings.AppNameDisplay);
                    this._pageFlow.Close(this);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(Strings.Strings.MessageUnableToGetWifi, Strings.Strings.AppNameDisplay);
                this._pageFlow.Close(this);
            }

            return result;
        }

        private void ListViewDevices_Unloaded(object sender, RoutedEventArgs e)
        {
            _wifiManager.DisconnectIfNeeded();
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewWifi.SelectedItem != null)
            {
                var entry = ListViewWifi.SelectedItem as WifiEntry;
                await entry?.StartConnect();

                if (entry != null && entry.NeedPassword == Visibility.Visible)
                {
                    var editor = FindPasswordEdit(e.Source);
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
            var button = control as FrameworkElement;
            var parentPanel = button?.Parent as Panel;

            var findControl = parentPanel?.FindName("textboxWifiPassword");
            return findControl as PasswordBox;
        }

        private async void ButtonConnectSecure_Click(object sender, RoutedEventArgs e)
        {
            var entry = ListViewWifi.SelectedItem as WifiEntry;
            if (entry != null)
            {
                var editor = FindPasswordEdit(e.Source);
                if (editor != null)
                {
                    await entry.DoConnect(editor.Password);
                    editor.Password = string.Empty;
                    editor.Focus();
                }
            }
        }

        private void ButtonCancelSecure_Click(object sender, RoutedEventArgs e)
        {
            var entry = ListViewWifi.SelectedItem as WifiEntry;
            entry?.CancelSecure();
        }

        private void ListViewWifi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var cur in e.RemovedItems)
            {
                var wifiEntry = cur as WifiEntry;
                wifiEntry?.Collapse();
            }

            foreach (var cur in e.AddedItems)
            {
                var wifiEntry = cur as WifiEntry;
                wifiEntry?.Expand();
            }
        }

        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            var wifiEntry = e.Source as WifiEntry;
            wifiEntry?.Expand();
        }

        private void ListViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            var wifiEntry = e.Source as WifiEntry;
            wifiEntry?.Expand();
        }

        private void textboxWifiPassword_PasswordChanged(object sender, RoutedEventArgs e)
       {
            var entry = ListViewWifi.SelectedItem as WifiEntry;
            if (entry != null)
            {
                var edit = sender as PasswordBox;
                if (edit != null)
                    entry.AllowSecure(edit.Password.Length > 0);
            }
       }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this._pageFlow.Close(this);
        }
    }
}
