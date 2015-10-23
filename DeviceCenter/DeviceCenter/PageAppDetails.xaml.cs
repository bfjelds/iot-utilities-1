using Microsoft.Tools.Connectivity;
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
        private DiscoveredDevice device;
        public AppInformation AppItem { get; private set; }
        public PageAppDetails(AppInformation item, DiscoveredDevice device)
        {
            InitializeComponent();

            this.AppItem = item;
            this.DataContext = this.AppItem;
            this.device = device;

            PanelDeploying.Visibility = Visibility.Collapsed;
            PanelDeployed.Visibility = Visibility.Collapsed;
            PanelDeploy.Visibility = Visibility.Collapsed;

            GetAppState();
        }

        private async void GetAppState()
        {
            WebBRest webbRequest = new WebBRest(this.device.IPAddress, this.device.Authentication);

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

            WebBRest webbRequest = new WebBRest(this.device.IPAddress, this.device.Authentication);

            AppInformation.ApplicationFiles sourceFiles = this.AppItem.PlatformFiles[device.Architecture];

            List<FileInfo> files = new List<FileInfo>();
            files.Add(sourceFiles.AppX);
            files.Add(sourceFiles.Certificate);

            foreach (var cur in sourceFiles.Dependencies)
                files.Add(cur);

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
            IPAddress ip = IPAddress.Parse(this.device.IPAddress);
            WebBRest webbRequest = new WebBRest(ip, "Administrator", "p@ssw0rd");

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
