using DeviceCenter.Helper;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for PageAppDetails.xaml
    /// </summary>
    public partial class PageAppDetails : Page
    {
        private const string BlinkyAppName = "BlinkyHeadedWebService";
        private const string InternetRadioAppName = "InternetRadioHeaded";

        public AppInformation AppItem { get; private set; }
        public Frame navigation;
        private DiscoveredDevice _device = null;
        private DiscoveryHelper _discoveryHelper = DiscoveryHelper.Instance;

        public PageAppDetails(Frame navigation, AppInformation item)
        {
            this.AppItem = item;
            this.DataContext = this.AppItem;
            this.navigation = navigation;

            InitializeComponent();

            comboBoxDevices.ItemsSource = _discoveryHelper.ConfiguredDevices;

            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Collapsed;

            GetAppState();
        }

        private void Page_Unloaded(object sender, object args)
        {
            _device = null;
        }

        ~PageAppDetails()
        {
            DiscoveryHelper.Release();
            _discoveryHelper = null;
        }

        private async void StopTheOtherApp()
        {
            if (_device == null)
            {
                PanelDeploy.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(Window.GetWindow(this), this._device.IpAddress, this._device.Authentication);

                var theOtherAppName = (this.AppItem.AppName == BlinkyAppName) ? InternetRadioAppName : BlinkyAppName;

                try
                {
                    if (await webbRequest.IsAppRunning(theOtherAppName))
                    {
                        await webbRequest.StopAppAsync(theOtherAppName);
                    }
                }
                catch (WebBRest.RestError)
                {
                    PanelDeploying.Visibility = Visibility.Collapsed;
                    PanelDeployed.Visibility = Visibility.Collapsed;
                    PanelDeploy.Visibility = Visibility.Visible;
                }
            }
        }

        private async void GetAppState()
        {
            var currentDevice = _device;

            if (_device == null)
            {
                PanelDeploy.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(Window.GetWindow(this), this._device.IpAddress, this._device.Authentication);

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
                catch (WebBRest.RestError ex)
                {
                    // Only hide the panels and display message box
                    // if the selection didn't change
                    if (currentDevice == _device)
                    {
                        PanelDeploy.Visibility = Visibility.Collapsed;
                        PanelDeployed.Visibility = Visibility.Collapsed;
                        PanelDeploying.Visibility = Visibility.Collapsed;

                        // If inner exception is SoketException, let the user know
                        if (ex.InnerException is WebException)
                        {
                            MessageBox.Show(ex.Message, Strings.Strings.AppNameDisplay, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private async void ButtonDeploy_Click(object sender, RoutedEventArgs e)
        {
            if (_device == null)
            {
                var errorCaption = Strings.Strings.AppNameDisplay;
                var errorMsg = Strings.Strings.ErrorNullDevice;

                MessageBox.Show(errorMsg, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                var currentDevice = _device;

                StopTheOtherApp();

                var webbRequest = new WebBRest(Window.GetWindow(this), this._device.IpAddress, this._device.Authentication);

                string arch = string.Empty;

                // Device discovered with pinger, try to get architecture with WebB REST call
                if(string.IsNullOrEmpty(_device.Architecture))
                {
                    // GetDeviceInfoAsync does not throw
                    var osInfo = await webbRequest.GetDeviceInfoAsync();

                    if(osInfo != null)
                    {
                        arch = osInfo.Arch;
                    }
                }
                // Device discovered with mDNS
                else
                {
                    arch = _device.Architecture;
                }

                // If we still could not get the device arch, give up
                // and hide all the panels
                // V2: Add message box saying that the architecture is
                // unknown
                if(string.IsNullOrEmpty(arch))
                {
                    PanelDeploying.Visibility = Visibility.Collapsed;
                    PanelDeployed.Visibility = Visibility.Collapsed;
                    PanelDeploy.Visibility = Visibility.Collapsed;

                    return;
                }

                // Make sure device architecture is up to date
                _device.Architecture = arch;

                PanelDeploy.Visibility = Visibility.Collapsed;
                PanelDeploying.Visibility = Visibility.Visible;
                PanelDeployed.Visibility = Visibility.Collapsed;

                var sourceFiles = this.AppItem.PlatformFiles[arch];

                var files = new List<FileInfo> { sourceFiles.AppX, sourceFiles.Certificate };

                files.AddRange(sourceFiles.Dependencies);

                try
                {
                    AppInformation.ApplicationFiles appFiles = null;

                    // Should never happen
                    if (!this.AppItem.PlatformFiles.TryGetValue(arch, out appFiles))
                    {
                        return;
                    }

                    string packageFullName = appFiles.PackageFullName;

                    if (!await webbRequest.RunAppxAsync(packageFullName, files))
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
                        Process.Start(new ProcessStartInfo(appUrl));
                    }
                }
                catch(WebBRest.RestError ex)
                {
                    // Only hide the panels and display message box
                    // if the selection didn't change
                    if (currentDevice == _device)
                    {
                        PanelDeploy.Visibility = Visibility.Visible;
                        PanelDeployed.Visibility = Visibility.Collapsed;
                        PanelDeploying.Visibility = Visibility.Collapsed;

                        // If inner exception is SoketException, let the user know
                        if (ex.InnerException is WebException)
                        {
                            MessageBox.Show(ex.Message, Strings.Strings.AppNameDisplay, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                    Debug.WriteLine(ex.Message);
                }
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
                var currentDevice = _device;

                var webbRequest = new WebBRest(Window.GetWindow(this), this._device.IpAddress, this._device.Authentication);

                AppInformation.ApplicationFiles appFiles = null;

                Debug.Assert(_device.Architecture != null);

                // Should never happen
                if (!this.AppItem.PlatformFiles.TryGetValue(_device.Architecture, out appFiles))
                {
                    return;
                }

                string packageFullName = appFiles.PackageFullName;

                try
                {
                    if (await webbRequest.StopAppAsync(packageFullName))
                    {
                        PanelDeployed.Visibility = Visibility.Collapsed;
                        PanelDeploying.Visibility = Visibility.Collapsed;
                        PanelDeploy.Visibility = Visibility.Visible;
                    }
                }
                catch(WebBRest.RestError ex)
                {
                    // Only hide the panels and display message box
                    // if the selection didn't change
                    if (currentDevice == _device)
                    {
                        PanelDeploy.Visibility = Visibility.Collapsed;
                        PanelDeployed.Visibility = Visibility.Visible;
                        PanelDeploying.Visibility = Visibility.Collapsed;

                        // If inner exception is SoketException, let the user know
                        if (ex.InnerException is WebException)
                        {
                            MessageBox.Show(ex.Message, Strings.Strings.AppNameDisplay, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            navigation.GoBack();
        }

        private void comboBoxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _device = comboBoxDevices.SelectedItem as DiscoveredDevice;

            // If the selection changed, hide all the commands, as we don't yet know
            // what we should show and the REST calls might take a while or even fail
            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Collapsed;

            if (_device == null)
            {
                return;
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
