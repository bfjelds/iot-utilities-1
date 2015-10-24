using System;
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
        private Frame _navigationFrame;

        public PageDeviceConfiguration(Frame navigationFrame, DiscoveredDevice device)
        {
            InitializeComponent();

            this._navigationFrame = navigationFrame;
            this.Device = device;

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LabelDeviceName.Text = this.Device.DeviceName;
            textBoxDeviceName.Text = this.Device.DeviceName;
            ButtonOk.IsEnabled = false;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _navigationFrame.GoBack();
        }

        private async void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            WebBRest webbRequest = new WebBRest(this.Device.IPAddress, this.Device.Authentication);
            if (!String.IsNullOrWhiteSpace(textBoxDeviceName.Text))
            {
                bool x = await webbRequest.SetDeviceNameAsync(textBoxDeviceName.Text);

                // TO DO: make it a dialog before restart
                MessageBox.Show("Rebooting the Device Now...");
                bool z = await webbRequest.RestartAsync();
            }
            if (!String.IsNullOrWhiteSpace(textBoxCurrentPassword.Password)
                && !String.IsNullOrWhiteSpace(textBoxPassword1.Password))
            {
                bool y = await webbRequest.SetPasswordAsync(textBoxCurrentPassword.Password, textBoxPassword1.Password);
            }
        }
             
        private void textBoxPassword2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (textBoxPassword1.Password == textBoxPassword2.Password)
            {
                PasswordCheckLabel.Text = "";
                ButtonOk.IsEnabled = true;
            }
            else
            {
                PasswordCheckLabel.Text = Strings.Strings.DeviceNamePwdPasswordDontMatch;
            }
        }
       
        private void textBoxDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ButtonOk.IsEnabled = true;
        }       
    }
}
