using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Onboarding;
using System.Collections.ObjectModel;
using IoTOnboardingConsumerWPF.Handlers;
using IoTOnboardingConsumerWPF.Wrappers;

namespace IoTOnboardingConsumerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IOnboardingManager m_Manager;

        // Holds the list of wifi networks that have AJ_ or _AJ in their SSIDs.
        // Accoarding to Allseen Onboarding service specification the Onboarding
        // producers should generate SSIDs with that format so that we can 
        // easily identify which networks are onboarding networks
        ObservableCollection<ManagedWifi> m_OnboardingNetworks;

        // Holds the list of Onboarding services found by the manager.
        // Ideally we should see only one item in this list, but it can be many more.
        ObservableCollection<ManagedConsumer> m_OnboardingConsumerList;

        // Holds the list of wifi networks available for the selected onboardee
        ObservableCollection<ManagedWifi> m_RemoteWifiList;

        public MainWindow()
        {
            InitializeComponent();
            statusTextBlock.Text = "Initializing...";

            this.listViewOnboardingConsumers.SelectionChanged += ListViewOnboardingConsumers_SelectionChanged;
            this.listViewRemoteWifis.SelectionChanged += ListViewRemoteWifis_SelectionChanged;

            this.Closed += MainWindow_Closed;

            m_OnboardingConsumerList = new ObservableCollection<ManagedConsumer>();
            m_RemoteWifiList = new ObservableCollection<ManagedWifi>();
            m_OnboardingNetworks = new ObservableCollection<ManagedWifi>();

            this.listViewRemoteWifis.ItemsSource = m_RemoteWifiList;
            this.listViewOnboardingConsumers.ItemsSource = m_OnboardingConsumerList;
            this.listViewOnboardingNetworks.ItemsSource = m_OnboardingNetworks;

            InitAsync();
        }

        #region ASync methods

        private void InitAsync()
        {
            m_Manager = new OnboardingManager();

            m_Manager.SetOnboardeeAddedHandler(new OnboardeeAddedHandler(
                (OnboardingConsumer consumer) =>
                {
                    Dispatcher.Invoke(() => { m_OnboardingConsumerList.Add(new ManagedConsumer(consumer)); });
                }));

            m_Manager.SetWifiConnectionResultHandler(new WifiConnectionResultHandler(
                (int reasonCode, string reasonString) =>
                {
                    
                }));

            m_Manager.Init();

        }
        private void GetScanInfoAsyn(IOnboardingConsumer consumer)
        {
            Task.Run(() =>
            {
                IWifiList list = null;
                try
                {
                    list = consumer.GetScanInfo();

                    if(list == null)
                    {
                        Dispatcher.Invoke(() => { statusTextBlock.Text = "Failed to get remote WiFi list. The session was lost."; });

                        return;
                    }

                    uint size = list.Size();

                    Dispatcher.Invoke(() =>
                    {
                        ClearRemoteWifiList();

                        for (uint i = 0; i < size; i++)
                        {
                            var item = list.GetItem(i);

                            m_RemoteWifiList.Add(new ManagedWifi(item));
                        }

                        statusTextBlock.Text = "Done.";
                    });
                }
                catch (COMException ex)
                {
                    Dispatcher.Invoke(() => { statusTextBlock.Text = "Failed to get remote WiFi list. HRESULT: " + ex.HResult; });
                }
                finally
                {
                    if (list != null)
                    {
                        Marshal.ReleaseComObject(list);
                        list = null;
                    }
                }
            });
        }

        private void ConfigWifiAsync(IOnboardingConsumer consumer, string ssid, string password, Int16 authType)
        {
            Task.Run(() =>
            {
                string safe_password;
                safe_password = password != null ? password : "";

                try
                {
                    // Response 1 means the adhoc network will be destroyed when we call the Connect method
                    // Response 2 means the adhoc network will be alive after we call connect and we should
                    // expect a signal with the operation result
                    var response = consumer.ConfigWifi(ssid, safe_password, authType);

                    Dispatcher.Invoke(() => { statusTextBlock.Text = "Credentials sent, trying to onboard now..."; });

                    // Fire and forget
                    consumer.Connect();

                    // Restore wifi, this is not implemented yet
                    m_Manager.RestoreWifi();
                }
                catch (COMException ex)
                {
                    Dispatcher.Invoke(() => { statusTextBlock.Text = "Failed to config wifi. HRESULT: " + ex.HResult; });
                }
            });
        }

        private void FindOnboardeesAsync()
        {
            Task.Run((Action)(() =>
            {
                IWifiList list = null;
                try
                {
                    list = m_Manager.GetOnboardingNetworks();

                    uint size = list.Size();

                    Dispatcher.Invoke((Action)(() =>
                    {
                        ClearOnboardingNetworksList();

                        for (uint i = 0; i < size; i++)
                        {
                            IWifi item = list.GetItem(i);

                            this.m_OnboardingNetworks.Add(new ManagedWifi(item));
                        }

                        statusTextBlock.Text = "Done.";
                    }));
                }
                catch(COMException ex)
                {
                    Dispatcher.Invoke(() => { statusTextBlock.Text = "Failed to find onboardees. HRESULT: " + ex.HResult; });
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

        private void ConnectToOnboardeeAsync(IWifi wifi, string password)
        {
            Task.Run(() =>
            {
                try
                {
                    m_Manager.ConnectToOnboardingNetwork((Onboarding.wifi)wifi, password);

                    Dispatcher.Invoke(() =>
                    {
                        statusTextBlock.Text = "Done.";
                    });
                }
                catch(COMException ex)
                {
                    Dispatcher.Invoke(() => { statusTextBlock.Text = "Failed to connect to onboarding network. HRESULT: " + ex.HResult; });
                }
            });
        }
        #endregion

        #region UI related methods

        private void btGetScanInfo_Click(object sender, RoutedEventArgs e)
        {
            var index = listViewOnboardingConsumers.SelectedIndex;

            if (index == -1)
            {
                MessageBox.Show("Select a target!");
                return;
            }

            IOnboardingConsumer name = ((ManagedConsumer)listViewOnboardingConsumers.Items[index]).NativeConsumer;
            GetScanInfoAsyn(name);
            statusTextBlock.Text = "Getting list...";
        }

        private void btConnect_Click(object sender, RoutedEventArgs e)
        {
            if (listViewOnboardingConsumers.SelectedIndex == -1)
            {
                MessageBox.Show("Select an object and then select a remote network to onboard.");
            }

            if (listViewRemoteWifis.SelectedIndex == -1)
            {
                MessageBox.Show("Select a remote network to onboard.");
                return;
            }

            ManagedWifi row0 = (ManagedWifi)listViewRemoteWifis.SelectedItem;

            if (row0.IntSecurity != 0 && string.IsNullOrEmpty(passwordBox.Password))
            {
                MessageBox.Show("The network you are trying to connect requires a password.");
                return;
            }

            string password = string.IsNullOrEmpty(passwordBox.Password) ? null : passwordBox.Password;

            IOnboardingConsumer consumer = ((ManagedConsumer)listViewOnboardingConsumers.SelectedItem).NativeConsumer;

            ConfigWifiAsync(consumer, row0.Ssid, password, row0.IntSecurity);
        }

        private void btOffboard_Click(object sender, RoutedEventArgs e)
        {
            if (listViewOnboardingConsumers.SelectedIndex == -1)
            {
                MessageBox.Show("Select an object and then select a remote network to onboard.");
            }
        }

        private void btRefreshOnboardeeList_Click(object sender, RoutedEventArgs e)
        {
            statusTextBlock.Text = "Looking for onboardees...";
            FindOnboardeesAsync();
        }

        private void btConnectToOnboardee_Click(object sender, RoutedEventArgs e)
        {
            if (this.listViewOnboardingNetworks.SelectedIndex == -1)
            {
                return;
            }

            ManagedWifi wifi = (ManagedWifi)listViewOnboardingNetworks.SelectedItem;

            ConnectToOnboardeeAsync(wifi.NativeWifi, "password");
        }

        private void ListViewOnboardingConsumers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                m_RemoteWifiList.Clear();
            });
        }

        private void ListViewRemoteWifis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            ManagedWifi row0 = (ManagedWifi)e.AddedItems[0];

            if (row0.IntSecurity != 0)
            {
                this.passwordBox.IsEnabled = true;
            }
            else
            {
                this.passwordBox.IsEnabled = false;
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            ClearOnboardingNetworksList();
            ClearOnboaringConsumerList();
            ClearRemoteWifiList();

            m_Manager.Shutdown();
        }

        #endregion

        #region Helpers
        private void ClearOnboardingNetworksList()
        {
            // Manually releasing COM objects, this way we don't depend
            // on the garbage colletion
            foreach (ManagedWifi wifi in m_OnboardingNetworks)
            {
                Marshal.ReleaseComObject(wifi.NativeWifi);
            }

            m_OnboardingNetworks.Clear();
        }

        private void ClearRemoteWifiList()
        {
            foreach (ManagedWifi wifi in m_RemoteWifiList)
            {
                Marshal.ReleaseComObject(wifi.NativeWifi);
            }
            m_RemoteWifiList.Clear();
        }

        private void ClearOnboaringConsumerList()
        {
            foreach (ManagedConsumer consumer in m_OnboardingConsumerList)
            {
                Marshal.ReleaseComObject(consumer.NativeConsumer);
            }
            m_OnboardingConsumerList.Clear();
        }
        #endregion
    }
}
