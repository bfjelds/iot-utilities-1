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
        private readonly PageFlow _pageFlow;

        public PageDeviceConfiguration(PageFlow pageFlow, DiscoveredDevice device)
        {
            this._pageFlow = pageFlow;
            this._pageFlow.PageChange += _pageFlow_PageChange;
            this.Device = device;

            InitializeComponent();

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        ~PageDeviceConfiguration()
        {
            this._pageFlow.PageChange -= _pageFlow_PageChange;
        }

        private void _pageFlow_PageChange(object sender, PageChangeCancelEventArgs e)
        {
            if (e.CurrentPage == this)
                e.Close = true;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            LabelDeviceName.Text = this.Device.DeviceName;
            textBoxDeviceName.Text = this.Device.DeviceName;
            ButtonOk.IsEnabled = false;
            linkPortal.NavigateUri = this.Device.Manage;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _pageFlow.GoBack();
        }

        private async void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Strings.Strings.DevicesConfigureDevice,
                Strings.Strings.DeviceRebootingMessage,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question,
                MessageBoxResult.OK) == MessageBoxResult.OK)
            {
                var webbRequest = WebBRest.Instance;
                if (!string.IsNullOrWhiteSpace(textBoxDeviceName.Text))
                {
                    if (await webbRequest.SetDeviceNameAsync(Device, textBoxDeviceName.Text))
                    {
                        MessageBox.Show(Strings.Strings.DeviceRebootingMessage);
                        await webbRequest.RestartAsync(Device);
                        _pageFlow.GoBack();
                    }
                }
            }
        }
             
        private void textBoxDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ButtonOk.IsEnabled = textBoxDeviceName.Text.Length > 0 && textBoxDeviceName.Text != this.Device.DeviceName;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            }
            catch(Exception ex)
            {
                MessageBox.Show(
                        ex.Message,
                        Strings.Strings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
            }
            e.Handled = true;
        }

        private void Hyperlink_SetPassword(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            _pageFlow.Navigate(typeof(PageDevicePassword), this.Device);
            e.Handled = true;
        }        
    }
}
