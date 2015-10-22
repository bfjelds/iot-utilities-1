using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SamplesPage.xaml
    /// </summary>
    public partial class SamplesPage : Page
    {
        private DiscoveredDevice device;

        public SamplesPage(Frame navigation, DiscoveredDevice device)
        {
            this._navigation = navigation;
            this.device = device;

            InitializeComponent();

            App.TelemetryClient.TrackPageView(this.GetType().Name);

            foreach (AppInformation cur in AppInformation.Initialize())
            {
                ButtonAppInfo newButton = new ButtonAppInfo(cur);
                newButton.Click += ShowApp_Click;

                listAppButtons.Children.Add(newButton);
            }
        }

        private void ShowApp_Click(object sender, ButtonAppInfo.ButtonAppEventArgs e)
        {
            this._navigation.Navigate(new PageAppDetails(e.Info, this.device));
        }

        private Frame _navigation;
    }
}
