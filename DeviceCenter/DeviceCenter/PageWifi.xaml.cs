using DeviceCenter.Wrappers;
using Onboarding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace DeviceCenter
{
    public class WifiEntry : INotifyPropertyChanged
    {
        private const string WifiIcons = "";
        public WifiEntry(Frame navigationFrame, ManagedConsumer consumer, IWifi comWifi)
        {
            this.comWifi = comWifi;
            this.consumer = consumer;
            this.navigationFrame = navigationFrame;

            this.Active = false;
            this.ShowConnect = Visibility.Collapsed;
            this.NeedPassword = Visibility.Collapsed;
            this.ShowExpanded = Visibility.Collapsed;
            this.SavePassword = false;

            this.needPassword = comWifi.GetSecurity() != 0;
            this.Name = comWifi.GetSSID();
            this.Secure = (this.needPassword) ? Strings.Strings.LabelSecureWifi : Strings.Strings.LabelInsecureWifi;
            this.SignalStrength = 4; // add when supported
        }

        private Frame navigationFrame;
        private ManagedConsumer consumer;
        private bool needPassword;
        private IWifi comWifi;

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
        public int SignalStrength
        {
            get;
            set;
        }

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
            await Task.Run(() =>
            {
                try
                {
                    // start connecting anonymous
                    string ssid = this.comWifi.GetSSID();
                    short security = this.comWifi.GetSecurity();

                    this.consumer.NativeConsumer.ConfigWifi(ssid, password, security);
                    this.consumer.NativeConsumer.Connect();

                    this.navigationFrame.GoBack();
                }
                catch (COMException)
                {
                }
            });
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
        private ManagedConsumer consumer;
        private Frame navigationFrame;

        public PageWifi(Frame navigationFrame, IOnboardingManager wifiManager)
        {
            InitializeComponent();

            this.navigationFrame = navigationFrame;
            ListViewWifi.SelectionChanged += ListViewWifi_SelectionChanged;
            ListViewWifi.ItemsSource = wifiList;
            progressWaiting.Visibility = Visibility.Visible;
        }

        public void SetConsumer(ManagedConsumer consumer)
        {
            this.consumer = consumer;

            LabelDeviceName.Text = consumer.NativeConsumer.GetDisplayName();

            foreach (var comWifi in consumer.WifiList)
                wifiList.Add(new WifiEntry(navigationFrame, consumer, comWifi));

            progressWaiting.Visibility = Visibility.Collapsed;
        }

        private ObservableCollection<WifiEntry> wifiList = new ObservableCollection<WifiEntry>();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ListViewDevices_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ListViewDevices_Unloaded(object sender, RoutedEventArgs e)
        {
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
    }
}
