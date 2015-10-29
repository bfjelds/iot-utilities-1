using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SamplesPage.xaml
    /// </summary>
    public partial class SamplesPage : Page
    {
        public SamplesPage(Frame navigation)
        {
            this._navigation = navigation;

            InitializeComponent();

            App.TelemetryClient.TrackPageView(this.GetType().Name);

            foreach (AppInformation cur in AppInformation.Initialize())
            {
                ButtonAppInfo newButton = new ButtonAppInfo(cur);
                newButton.Click += ShowApp_Click;

                listAppButtons.Children.Add(newButton);
            }
        }

        private void ShowApp_Click(object sender, ButtonAppInfo.ButtonAppEventArgs e)
        {
            this._navigation.Navigate(new PageAppDetails(_navigation, e.Info));
        }

        private Frame _navigation;

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
