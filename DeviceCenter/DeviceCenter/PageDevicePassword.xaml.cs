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
        private readonly Frame _navigationFrame;

        public PageDevicePassword(Frame navigationFrame, DiscoveredDevice device)
        {
            InitializeComponent();

            this._navigationFrame = navigationFrame;
            this.Device = device;

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LabelDeviceName.Text = this.Device.DeviceName;
            ButtonOk.IsEnabled = false;
        }

        private void textBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (textBoxPassword1.Password == textBoxPassword2.Password)
            {
                PasswordCheckLabel.Text = "";
                ButtonOk.IsEnabled = true;
            }
            else
            {
                PasswordCheckLabel.Text = Strings.Strings.DeviceNamePwdPasswordDontMatch;
                ButtonOk.IsEnabled = false;
            }
        }

        private async void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            var webbRequest = new WebBRest(Window.GetWindow(this), this.Device.IpAddress, this.Device.Authentication);

            if (!string.IsNullOrWhiteSpace(textBoxCurrentPassword.Password) && 
                !string.IsNullOrWhiteSpace(textBoxPassword1.Password))
            {
                var result = await webbRequest.SetPasswordAsync(textBoxCurrentPassword.Password, textBoxPassword1.Password);

                // bring it back to setup screen if password setting is successful.
                if (result == true)
                {
                    MessageBox.Show(
                        "Password changed successfully",
                        Strings.Strings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.None);
                    _navigationFrame.GoBack();
                }
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _navigationFrame.GoBack();
        }
    }
}
