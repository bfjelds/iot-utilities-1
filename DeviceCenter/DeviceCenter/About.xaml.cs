using System;
using System.Windows.Controls;
using System.Deployment.Application;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Page
    {
        public About()
        {
            InitializeComponent();

            // Check if this is a ClickOnce deployment
            if(ApplicationDeployment.IsNetworkDeployed)
            {
                labelVersion.Text = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            else
            {
                labelVersion.Text = "Private Build";
            }
        }
    }
}