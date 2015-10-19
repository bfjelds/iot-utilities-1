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
        public SamplesPage(Frame navigation)
        {
            this._navigation = navigation;

            InitializeComponent();

            App.TelemetryClient.TrackPageView(this.GetType().Name);

            this.AppList.Add(new AppInformation(
                "Assets/Blinky.png",
                Strings.Strings.SamplesBlinkyTitle,
                Strings.Strings.SamplesBlinkyMessage1 + "\n" + Strings.Strings.SamplesBlinkyMessage2));

            foreach (AppInformation cur in AppInformation.Initialize())
            {
                ButtonAppInfo newButton = new ButtonAppInfo(cur);
                newButton.Click += ShowApp_Click;

                listAppButtons.Children.Add(newButton);
            }
        }

        private void ShowApp_Click(object sender, ButtonAppInfo.ButtonAppEventArgs e)
        {
            this._navigation.Navigate(new PageAppDetails(e.Info));
        }

        private Frame _navigation;
    }
}
