using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        public AppInformation AppItem { get; private set; }
        public Frame navigation;
        private readonly ObservableCollection<DiscoveredDevice> _devices = new ObservableCollection<DiscoveredDevice>();
        private DiscoveredDevice _device;
        private NativeMethods.AddDeviceCallbackDelegate _addCallbackdel;

        public PageAppDetails(Frame navigation, AppInformation item)
        {
            InitializeComponent();

            _addCallbackdel = new NativeMethods.AddDeviceCallbackDelegate(AddDeviceCallback);
            NativeMethods.RegisterCallback(_addCallbackdel);

            //Start device discovery using DNS-SD
            NativeMethods.StartDiscovery();
            comboBoxDevices.ItemsSource = _devices;

            this.AppItem = item;
            this.DataContext = this.AppItem;
            this.navigation = navigation;

            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Collapsed;

            GetAppState();
        }

        private async void GetAppState()
        {
            if (_device == null)
            {
                PanelDeploy.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(this._device.IpAddress, this._device.Authentication);

                try
                {
                    if (await webbRequest.IsAppRunning(this.AppItem.AppName))
                    {
                        PanelDeployed.Visibility = Visibility.Visible;
                        PanelDeploy.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        PanelDeploy.Visibility = Visibility.Visible;
                        PanelDeployed.Visibility = Visibility.Collapsed;
                    }
                }
                catch (WebBRest.RestError)
                {
                    PanelDeploy.Visibility = Visibility.Collapsed;
                    PanelDeployed.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void ButtonDeploy_Click(object sender, RoutedEventArgs e)
        {
            if (_device != null && _device.Architecture == null || _device.Architecture.Length == 0)
            {
                // the app name as caption
                var errorCaption = Strings.Strings.AppNameDisplay;

                // show the filename, use standard windows error
                string minBuild = "10.0.10577";
                var errorMsg = string.Format(Strings.Strings.ErrorUnknownArchitecture, minBuild);

                MessageBox.Show(errorMsg, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            PanelDeploy.Visibility = Visibility.Collapsed;
            PanelDeploying.Visibility = Visibility.Visible;
            PanelDeployed.Visibility = Visibility.Collapsed;

            var webbRequest = new WebBRest(this._device.IpAddress, this._device.Authentication);                      

            var sourceFiles = this.AppItem.PlatformFiles[_device.Architecture];

            var files = new List<FileInfo> {sourceFiles.AppX, sourceFiles.Certificate};

            files.AddRange(sourceFiles.Dependencies);

            if (!await webbRequest.InstallAppxAsync(this.AppItem.AppName, files))
            {
                PanelDeploying.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
                PanelDeploy.Visibility = Visibility.Visible;
            }
            else
            {
                PanelDeploying.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Visible;
                PanelDeploy.Visibility = Visibility.Collapsed;

                var appUrl = "http://" + this._device.IpAddress + ":" + this.AppItem.AppPort;
                System.Diagnostics.Process.Start("IExplore.exe", appUrl);
            }
        }

        private void ButtonStopDeploy_Click(object sender, RoutedEventArgs e)
        {
            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Visible;
        }

        private async void ButtonStopApp_Click(object sender, RoutedEventArgs e)
        {
            if (_device == null)
            {
                PanelDeployed.Visibility = Visibility.Collapsed;
                PanelDeploying.Visibility = Visibility.Collapsed;
                PanelDeploy.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(this._device.IpAddress, this._device.Authentication);

                if (await webbRequest.StopAppAsync(this.AppItem.AppName))
                {
                    PanelDeployed.Visibility = Visibility.Collapsed;
                    PanelDeploying.Visibility = Visibility.Collapsed;
                    PanelDeploy.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelDeploying.Visibility = Visibility.Collapsed;
                    PanelDeployed.Visibility = Visibility.Collapsed;
                    PanelDeploy.Visibility = Visibility.Visible;
                }
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            navigation.GoBack();
        }

        private void AddDeviceCallback(string deviceName, string ipV4Address, string ipV6Address, string txtParameters)
        {
            if (String.IsNullOrEmpty(deviceName) || String.IsNullOrEmpty(ipV4Address))
            {
                return;
            }

            string deviceModel = "";
            string osVersion = "";
            string deviceGuid = "";
            string arch = "";

            //The txt parameter are in following format
            // txtParameters = "guid=79F50796-F59B-D97A-A00F-63D798C6C144,model=Virtual,architecture=x86,osversion=10.0.10557,"
            // Split them with ',' and '=' and get the odd values 
            string[] deviceDetails = txtParameters.Split(',', '=');
            int index = 0;
            while (index < deviceDetails.Length)
            {
                switch (deviceDetails[index])
                {
                    case "guid":
                        deviceGuid = deviceDetails[index + 1];
                        break;
                    case "model":
                        deviceModel = deviceDetails[index + 1];
                        break;
                    case "osversion":
                        osVersion = deviceDetails[index + 1];
                        break;
                    case "architecture":
                        arch = deviceDetails[index + 1];
                        break;
                }
                index += 2;
            }

            var newDevice = new DiscoveredDevice()
            {
                DeviceName = deviceName.Substring(0, deviceName.IndexOf('.')),
                DeviceModel = deviceModel,
                Architecture = arch,
                OsVersion = osVersion,
                IpAddress = IPAddress.Parse(ipV4Address),
                UniqueId = String.IsNullOrEmpty(deviceGuid) ? Guid.Empty : new Guid(deviceDetails[1]),
                Manage = new Uri($"http://administrator@{ipV4Address}/"),
                Authentication = DialogAuthenticate.GetSavedPassword(deviceName)
            };

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _devices.Add(newDevice);
            }));
        }

        private async void ButtonBringAppForground_Click(object sender, RoutedEventArgs e)
        {
            if (_device == null)
            {
                PanelDeploying.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
                PanelDeploy.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(this._device.IpAddress, this._device.Authentication);

                if (!(await webbRequest.StartAppAsync(this.AppItem.AppName)))
                {
                    PanelDeploying.Visibility = Visibility.Collapsed;
                    PanelDeployed.Visibility = Visibility.Collapsed;
                    PanelDeploy.Visibility = Visibility.Visible;
                }
                else
                {
                    var appUrl = "http://" + this._device.IpAddress + ":" + this.AppItem.AppPort;
                    System.Diagnostics.Process.Start("IExplore.exe", appUrl);
                }
            }
        }

        private void comboBoxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _device = comboBoxDevices.SelectedItem as DiscoveredDevice;
            if (_device == null)
            {
                PanelDeploying.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
                PanelDeploy.Visibility = Visibility.Collapsed;
            }
            else
            {
                GetAppState();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
