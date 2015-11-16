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
    /// Interaction logic for PageWelcome.xaml
    /// </summary>
    public partial class PageWelcome : Page
    {
        private readonly PageFlow _pageFlow;

        public PageWelcome(PageFlow pageFlow)
        {
            InitializeComponent();
            this._pageFlow = pageFlow;

            App.TelemetryClient.TrackPageView(this.GetType().Name);
        }

        private void SetupDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            _pageFlow.Navigate(typeof(SetupDevicePage));
        }

        private void ViewDevices_Click(object sender, RoutedEventArgs e)
        {
            _pageFlow.Navigate(typeof(ViewDevicesPage));
        }
    }
}
