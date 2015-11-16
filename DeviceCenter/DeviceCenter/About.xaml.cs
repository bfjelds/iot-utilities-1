using System.Windows.Controls;
using System.Deployment.Application;
using System.Diagnostics;
using System;
using System.Windows;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Page
    {
        public About(PageFlow pageFlow)
        {
            InitializeComponent();

            // Check if this is a ClickOnce deployment
            if(ApplicationDeployment.IsNetworkDeployed)
            {
                labelVersion.Text = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            else
            {
                labelVersion.Text = Strings.Strings.AboutPrivateBuild;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                        ex.Message,
                        Strings.Strings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
            }
            e.Handled = true;
        }
    }
}