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

            labelVersion.Text = DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToShortTimeString().ToLower();

            // Check if this is a ClickOnce deployment
            if(ApplicationDeployment.IsNetworkDeployed)
            {
                labelClickOnceVersion.Text = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            else
            {
                labelClickOnceVersion.Text = "Private Build";
            }
        }
    }
}