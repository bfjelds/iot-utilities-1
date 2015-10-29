﻿namespace DeviceCenter
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Navigation;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Uncomment to test localization
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("it-IT");

            InitializeComponent();

            Page welcomePage = new PageWelcome(_NavigationFrame);

            _NavigationFrame.Navigate(welcomePage);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            NativeMethods.HideSystemMenu(this);

            base.OnSourceInitialized(e);
        }

        private void buttonMyDevices_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new ViewDevicesPage(_NavigationFrame));
        }

        private void buttonAddDevice_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new SetupDevicePage());
        }
        private void buttonSamples_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new SamplesPage(_NavigationFrame));
        }        

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new About());
        }
    }
}
