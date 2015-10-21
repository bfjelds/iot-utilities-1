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

            welcomePage = new PageWelcome(_NavigationFrame);
            ButtonBack.Visibility = Visibility.Hidden;

            _NavigationFrame.Navigate(welcomePage);
            _NavigationFrame.Navigated += _NavigationFrame_Navigated;
        }

        private void _NavigationFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ButtonBack.Visibility = _NavigationFrame.CanGoBack ? Visibility.Visible : Visibility.Hidden;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            NativeMethods.HideSystemMenu(this);

            base.OnSourceInitialized(e);
        }

        private Page welcomePage;

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

        private void buttonTestOnly_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new PageTestOnly());
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.GoBack();
        }
    }
}
