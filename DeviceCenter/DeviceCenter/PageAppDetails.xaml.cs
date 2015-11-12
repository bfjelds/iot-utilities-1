using DeviceCenter.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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
        private bool initializing = true;

        public PageAppDetails(Frame navigation, AppInformation item)
        {
            this.AppItem = item;
            this.DataContext = this.AppItem;
            this.navigation = navigation;

            InitializeComponent();

            // this is to prevent the combobox from selecting the first item while loading.  This
            // can cause it to try to get application state which may trigger an authentication
            // message.
            initializing = false;
            comboBoxDevices.SelectedIndex = -1;

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
        }

        private async void StopTheOtherApp(DiscoveredDevice device, string arch)
        {
            if (device == null)
            {
                PanelDeploy.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(Window.GetWindow(this), device.IpAddress, device.Authentication);

                string theOtherAppName = null;

                if(arch.Equals("x86"))
                {
                    theOtherAppName = (this.AppItem.AppName == BlinkyAppName) ? AppInformation.InternetRadio_PackageFullName_x86 : AppInformation.Blinky_PackageFullName_x86;
                }
                else if(arch.Equals("ARM"))
                {
                    theOtherAppName = (this.AppItem.AppName == BlinkyAppName) ? AppInformation.InternetRadio_PackageFullName_arm : AppInformation.Blinky_PackageFullName_arm;
                }

                // This should never happen
                Debug.Assert(theOtherAppName != null);

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

            if (currentDevice == null)
            {
                PanelDeploy.Visibility = Visibility.Collapsed;
                PanelDeployed.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(Window.GetWindow(this), currentDevice.IpAddress, currentDevice.Authentication);

                try
                {
                    AppInformation.ApplicationFiles appFiles = null;

                    var arch = await GetDeviceArchAsync(currentDevice, webbRequest);

                    // If we can't get device arch, hide everything as we can't be sure
                    // which app version to deploy
                    if(string.IsNullOrEmpty(arch))
                    {
                        throw new WebBRest.RestError("Unable to get device architecture.", null);
                    }

                    // Should never happen
                    if (!this.AppItem.PlatformFiles.TryGetValue(arch, out appFiles))
                    {
                        return;
                    }

                    string packageFullName = appFiles.PackageFullName;

                    if (await webbRequest.IsAppRunning(packageFullName))
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

        private async Task<string> GetDeviceArchAsync(DiscoveredDevice device, WebBRest webbRequest)
        {
            if (device == null || webbRequest == null)
            {
                return string.Empty;
            }

            string arch = string.Empty;

            var osInfo = await webbRequest.GetDeviceInfoAsync();

            if (osInfo != null)
            {
                arch = osInfo.Arch;

                // Update information for this device
                device.Architecture = arch;
            }

            return arch;
        }

        private async void ButtonDeploy_Click(object sender, RoutedEventArgs e)
        {
            var currentDevice = _device;

            if (currentDevice == null)
            {
                var errorCaption = Strings.Strings.AppNameDisplay;
                var errorMsg = Strings.Strings.ErrorNullDevice;

                MessageBox.Show(errorMsg, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                var webbRequest = new WebBRest(Window.GetWindow(this), currentDevice.IpAddress, currentDevice.Authentication);

                string arch = await GetDeviceArchAsync(currentDevice, webbRequest);

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

                StopTheOtherApp(currentDevice, arch);

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

                        var appUrl = "http://" + currentDevice.IpAddress + ":" + this.AppItem.AppPort;
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
            var currentDevice = _device;

            if (currentDevice == null)
            {
                PanelDeployed.Visibility = Visibility.Collapsed;
                PanelDeploying.Visibility = Visibility.Collapsed;
                PanelDeploy.Visibility = Visibility.Collapsed;
            }
            else
            {
                var webbRequest = new WebBRest(Window.GetWindow(this), currentDevice.IpAddress, currentDevice.Authentication);

                AppInformation.ApplicationFiles appFiles = null;

                Debug.Assert(currentDevice.Architecture != null);

                // Should never happen
                if (!this.AppItem.PlatformFiles.TryGetValue(currentDevice.Architecture, out appFiles))
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
            if (!this.initializing)
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
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
