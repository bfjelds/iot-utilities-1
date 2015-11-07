﻿using DeviceCenter.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
                catch (WebBRest.RestError)
                {
                    PanelDeploy.Visibility = Visibility.Collapsed;
                    PanelDeployed.Visibility = Visibility.Collapsed;
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
                StopTheOtherApp();

                var webbRequest = new WebBRest(Window.GetWindow(this), this._device.IpAddress, this._device.Authentication);

                string arch = string.Empty;

                // Device discovered with pinger, try to get architecture with WebB REST call
                if(string.IsNullOrEmpty(_device.Architecture))
                {
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

                PanelDeploy.Visibility = Visibility.Collapsed;
                PanelDeploying.Visibility = Visibility.Visible;
                PanelDeployed.Visibility = Visibility.Collapsed;

                var sourceFiles = this.AppItem.PlatformFiles[arch];

                var files = new List<FileInfo> { sourceFiles.AppX, sourceFiles.Certificate };

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
                    Process.Start(new ProcessStartInfo(appUrl));
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
                var webbRequest = new WebBRest(Window.GetWindow(this), this._device.IpAddress, this._device.Authentication);

                if (await webbRequest.StopAppAsync(this.AppItem.AppName))
                {
                    PanelDeployed.Visibility = Visibility.Collapsed;
                    PanelDeploying.Visibility = Visibility.Collapsed;
                    PanelDeploy.Visibility = Visibility.Visible;
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
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
