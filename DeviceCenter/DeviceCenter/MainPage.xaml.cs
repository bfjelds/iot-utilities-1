namespace DeviceCenter
{
    using System;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PageFlow pageFlow;
        public MainWindow()
        {
            // Uncomment to test localization
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("it-IT");

            InitializeComponent();

            pageFlow = new PageFlow(_NavigationFrame);
            if (String.IsNullOrWhiteSpace((string)Application.Current.Properties["FFUFilePath"]))
            {
                pageFlow.Navigate(typeof(PageWelcome));
            }
            else
            {
                pageFlow.Navigate(typeof(SetupDevicePage));
            }
            pageFlow.PageChange += PageFlow_PageChange;
        }

        private void PageFlow_PageChange(object sender, PageChangeCancelEventArgs e)
        {
            if (e.NewPage is ViewDevicesPage)
                buttonMyDevices.Selected = true;
            else if (e.NewPage is SetupDevicePage)
                buttonSetupDevice.Selected = true;
            else if (e.NewPage is SamplesPage)
                buttonSamples.Selected = true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            NativeMethods.HideSystemMenu(this);

            base.OnSourceInitialized(e);
        }

        private void buttonMyDevices_Click(object sender, RoutedEventArgs e)
        {
            pageFlow.Navigate(typeof(ViewDevicesPage));
        }

        private void buttonAddDevice_Click(object sender, RoutedEventArgs e)
        {
            pageFlow.Navigate(typeof(SetupDevicePage));
        }
        private void buttonSamples_Click(object sender, RoutedEventArgs e)
        {
            pageFlow.Navigate(typeof(SamplesPage));
        }        

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            pageFlow.Navigate(typeof(About));
        }
    }
}
