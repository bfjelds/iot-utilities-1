using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for PageDiskImageComplete.xaml
    /// </summary>
    public partial class PageDiskImageComplete : Page
    {
        private Frame navigationFrame;

        public PageDiskImageComplete(Frame navigationFrame)
        {
            InitializeComponent();
            this.navigationFrame = navigationFrame;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Hyperlink_SetupDevice(object sender, RequestNavigateEventArgs e)
        {
            navigationFrame.GoBack();
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            navigationFrame.Navigate(new ViewDevicesPage(navigationFrame));
        }
    }
}
