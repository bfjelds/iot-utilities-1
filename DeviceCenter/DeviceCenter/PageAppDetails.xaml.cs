using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly DiscoveredDevice _device;

        public AppInformation AppItem { get; private set; }

        public PageAppDetails(AppInformation item, DiscoveredDevice device)
        {
            InitializeComponent();

            this.AppItem = item;
            this.DataContext = this.AppItem;
            this._device = device;

            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Collapsed;

            GetAppState();
        }

        private async void GetAppState()
        {
            var webbRequest = new WebBRest(this._device.IpAddress, this._device.Authentication);

            if (await webbRequest.IsAppRunning(this.AppItem.AppName))
            {
                PanelDeployed.Visibility = Visibility.Visible;
            }
            else
            {
                PanelDeploy.Visibility = Visibility.Visible;
            }
        }

        private async void ButtonDeploy_Click(object sender, RoutedEventArgs e)
        {
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
}
