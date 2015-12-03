using System.Windows.Controls;
using System.Deployment.Application;
using System.Diagnostics;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Globalization;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Page
    {
        private readonly Dictionary<string, int> licenseFwLinkLookup = new Dictionary<string, int>()
        {
            { "en", 703961 },
            { "fr", 715644 },
            { "it", 715645 },
            { "de", 715646 },
            { "es", 715647 },
            { "zh-cn", 715648 },
            { "zh-tw", 715649 },
            { "ja", 715650 },
            { "ko", 715651 },
            { "pt", 715652 },
            { "ru", 715653 },
        };

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
                        LocalStrings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
            }
            e.Handled = true;
        }

        private void Hyperlink_ServicesAgreement(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            int resourceId = -1;

            if (licenseFwLinkLookup.ContainsKey(CultureInfo.CurrentUICulture.Name))
                resourceId = licenseFwLinkLookup[CultureInfo.CurrentUICulture.Name];
            else if (licenseFwLinkLookup.ContainsKey(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName))
                resourceId = licenseFwLinkLookup[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];
            else
                resourceId = licenseFwLinkLookup["en"];

            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri + resourceId.ToString(CultureInfo.InvariantCulture)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                        ex.Message,
                        LocalStrings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
            }
            e.Handled = true;
        }
    }
}