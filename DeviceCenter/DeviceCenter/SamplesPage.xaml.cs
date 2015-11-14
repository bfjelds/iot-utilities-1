using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SamplesPage.xaml
    /// </summary>
    public partial class SamplesPage : Page
    {
        public SamplesPage(PageFlow pageFlow)
        {
            this._pageFlow = pageFlow;

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
            this._pageFlow.Navigate(typeof(PageAppDetails), e.Info);
        }

        private readonly PageFlow _pageFlow;

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
    }
}
