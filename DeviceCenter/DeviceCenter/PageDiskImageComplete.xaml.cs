using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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
