using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for PageDeviceConfiguration.xaml
    /// </summary>
    public partial class PageDeviceConfiguration : Page
    {
        /*
            \\ is used to escape chars in the regex string itself
            \  is used to escape the C# string
        */
        public static string InvalidCharsRegexPattern = "[ `~!@#\\$%\\^&\\*()=+\\[\\]\\{\\}\\|;:.'\",<>\\\\/?]";

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
            ButtonOk.IsEnabled = false;

            try
            {
                if (string.IsNullOrWhiteSpace(textBoxDeviceName.Text) || Regex.IsMatch(textBoxDeviceName.Text, InvalidCharsRegexPattern))
                {
                    // Used to get error message from system
                    //
                    // ERROR_BAD_DEVICE
                    //    1200(0x4B0)
                    //    The specified device name is invalid.

                    Win32Exception ex = new Win32Exception(1200);

                    MessageBox.Show(ex.Message, LocalStrings.AppNameDisplay, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                var webbRequest = WebBRest.Instance;

                if (await webbRequest.SetDeviceNameAsync(Device, textBoxDeviceName.Text))
                {
                    var dlg = new WindowWarning()
                    {
                        Header = Strings.Strings.TitleDeviceNameChanged,
                        Message = Strings.Strings.DeviceRebootingMessage,
                        Owner = Window.GetWindow(this)
                    };

                    var confirmation = dlg.ShowDialog();

                    if (confirmation.HasValue && confirmation.Value)
                    {
                        await webbRequest.RestartAsync(Device);
                    }

                    _pageFlow.Close(this);
                }
            }
            finally
            {
                ButtonOk.IsEnabled = true;
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
                        LocalStrings.AppNameDisplay,
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
