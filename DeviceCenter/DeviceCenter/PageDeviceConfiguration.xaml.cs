using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _navigationFrame.GoBack();
        }

        private async void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            //textBoxDeviceName.Text;
            //textBoxCurrentPassword.Text
            //textBoxPassword1

            // do stuff here
            IPAddress ip = IPAddress.Parse(this.Device.IPAddress);
            WebBRest webbRequest = new WebBRest(ip, "Administrator", "p@ssw0rd");
            if (!String.IsNullOrWhiteSpace(textBoxDeviceName.Text))
            {
                bool x = await webbRequest.SetDeviceNameAsync(textBoxDeviceName.Text);

                // TO DO: make it a dialog before restart
                MessageBox.Show("Do you want to restart the device?");
                bool z = await webbRequest.Restart();
            }
            if (!String.IsNullOrWhiteSpace(textBoxCurrentPassword.Password)
                && !String.IsNullOrWhiteSpace(textBoxPassword1.Password))
            {
                bool y = await webbRequest.SetPasswordAsync(textBoxCurrentPassword.Password, textBoxPassword1.Password);
            }
        }
    }
}
