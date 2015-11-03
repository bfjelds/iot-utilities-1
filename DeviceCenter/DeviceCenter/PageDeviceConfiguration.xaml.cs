using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for PageDeviceConfiguration.xaml
    /// </summary>
    public partial class PageDeviceConfiguration : Page
    {
        public DiscoveredDevice Device { get; private set; }
        private readonly Frame _navigationFrame;

        public PageDeviceConfiguration(Frame navigationFrame, DiscoveredDevice device)
        {
            this._navigationFrame = navigationFrame;
            this.Device = device;

            InitializeComponent();

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            LabelDeviceName.Text = this.Device.DeviceName;
            textBoxDeviceName.Text = this.Device.DeviceName;
            ButtonOk.IsEnabled = false;
            linkPortal.NavigateUri = this.Device.Manage;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _navigationFrame.GoBack();
        }

        private async void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            var webbRequest = new WebBRest(this.Device.IpAddress, this.Device.Authentication);
            if (!string.IsNullOrWhiteSpace(textBoxDeviceName.Text))
            {
                var x = await webbRequest.SetDeviceNameAsync(textBoxDeviceName.Text);

                // TO DO: make it a dialog before restart
                MessageBox.Show(Strings.Strings.DeviceRebootingMessage);
                var z = await webbRequest.RestartAsync();
            }
        }
             
        private void textBoxDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ButtonOk.IsEnabled = true;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Hyperlink_SetPassword(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            _navigationFrame.Navigate(new PageDevicePassword(this._navigationFrame, this.Device));
            e.Handled = true;
        }        
    }
}
