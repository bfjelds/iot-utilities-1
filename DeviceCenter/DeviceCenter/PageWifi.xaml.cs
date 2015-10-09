﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace DeviceCenter
{
    public class WifiEntry : INotifyPropertyChanged
    {
        private const string WifiIcons = "";
        public WifiEntry(string name, bool secure)
        {
            this.Active = false;
            this.ShowConnect = Visibility.Collapsed;
            this.NeedPassword = Visibility.Collapsed;

            this.needPassword = secure;
            this.Name = name;
            this.Secure = (secure) ? Strings.Strings.LabelSecureWifi : Strings.Strings.LabelInsecureWifi;
            this.SignalStrength = 4; // can we do this?
        }

        private bool needPassword;

        public string Name { get; private set; }
        public string Secure { get; private set; }
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

            OnPropertyChanged("ShowConnect");
            OnPropertyChanged("Active");
            OnPropertyChanged("NeedPassword");
        }

        public void Collapse()
        {
            this.Active = false;
            ShowConnect = Visibility.Collapsed;
            NeedPassword = Visibility.Collapsed;

            OnPropertyChanged("ShowConnect");
            OnPropertyChanged("Active");
            OnPropertyChanged("NeedPassword");
        }

        public void StartConnect()
        {
            if (this.needPassword)
            {
                NeedPassword = Visibility.Visible;
                ShowConnect = Visibility.Collapsed;

                OnPropertyChanged("NeedPassword");
                OnPropertyChanged("ShowConnect");
            }
            else
            {
                // start connecting
            }
        }

        public void StartConnectSecure()
        {
            //textbo
            //start connecting
        }

        public bool Active { get; private set; }
        public Visibility ShowConnect { get; private set; }
        public Visibility ShowConnecting { get; private set; }
        public Visibility NeedPassword { get; private set; }

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
        public PageWifi()
        {
            InitializeComponent();
            ListViewWifi.SelectionChanged += ListViewWifi_SelectionChanged;
        }

        private ObservableCollection<WifiEntry> wifiList = new ObservableCollection<WifiEntry>();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ListViewWifi.ItemsSource = wifiList;

            wifiList.Add(new WifiEntry("MSFTTest", false));
            wifiList.Add(new WifiEntry("MSFTFTE", true));
            wifiList.Add(new WifiEntry("HackedWifi", false));
            wifiList.Add(new WifiEntry("OpenWifi", false));
            wifiList.Add(new WifiEntry("Linksys", true));
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
            }
        }

        private void ButtonConnectSecure_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewWifi.SelectedItem != null)
            {
                WifiEntry entry = ListViewWifi.SelectedItem as WifiEntry;
                if (entry != null)
                    entry.StartConnectSecure();
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
    }
}
