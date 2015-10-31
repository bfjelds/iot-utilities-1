using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private async void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            var webbRequest = new WebBRest(this.Device.IpAddress, this.Device.Authentication);

            if (!String.IsNullOrWhiteSpace(textBoxCurrentPassword.Password) && 
                !String.IsNullOrWhiteSpace(textBoxPassword1.Password))
            {
                var y = await webbRequest.SetPasswordAsync(textBoxCurrentPassword.Password, textBoxPassword1.Password);
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _navigationFrame.GoBack();
        }
    }
}
