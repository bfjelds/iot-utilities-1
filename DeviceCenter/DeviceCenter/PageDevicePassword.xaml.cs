using System;
using System.Windows;
using System.Windows.Controls;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for PageDevicePassword.xaml
    /// </summary>
    public partial class PageDevicePassword : Page
    {
        public DiscoveredDevice Device { get; private set; }
        private readonly PageFlow _pageFlow;

        public PageDevicePassword(PageFlow pageFlow, DiscoveredDevice device)
        {
            InitializeComponent();

            this._pageFlow = pageFlow;
            this._pageFlow.PageChange += _pageFlow_PageChange;
            this.Device = device;

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        ~PageDevicePassword()
        {
            this._pageFlow.PageChange -= _pageFlow_PageChange;
        }

        private void _pageFlow_PageChange(object sender, PageChangeCancelEventArgs e)
        {
            if (e.CurrentPage == this)
                e.Close = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LabelDeviceName.Text = this.Device.DeviceName;
            ButtonOk.IsEnabled = false;
        }

        private void textBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ButtonOk.IsEnabled = !string.IsNullOrWhiteSpace(textBoxCurrentPassword.Password) 
                && !string.IsNullOrWhiteSpace(textBoxPassword1.Password) 
                && !string.IsNullOrWhiteSpace(textBoxPassword2.Password) 
                && textBoxPassword1.Password == textBoxPassword2.Password;

            if (textBoxPassword1.Password == textBoxPassword2.Password)
            {
                PasswordCheckLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                PasswordCheckLabel.Visibility = Visibility.Visible;
            }
        }

        private async void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            ButtonOk.IsEnabled = false;

            var webbRequest = WebBRest.Instance;

            if (!string.IsNullOrWhiteSpace(textBoxCurrentPassword.Password) &&
                !string.IsNullOrWhiteSpace(textBoxPassword1.Password))
            {
                var result = await webbRequest.SetPasswordAsync(Device, textBoxCurrentPassword.Password, textBoxPassword1.Password);

                // bring it back to setup screen if password setting is successful.
                if (result == true)
                {
                    MessageBox.Show(
                        Strings.Strings.SuccessPasswordChanged,
                        Strings.Strings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.None);

                    _pageFlow.Close(this);
                }
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _pageFlow.GoBack();
        }
    }
}
