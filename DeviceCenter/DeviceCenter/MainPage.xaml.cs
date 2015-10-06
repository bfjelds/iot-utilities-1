using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            welcomePage = new PageWelcome(_NavigationFrame);
            PanelTitleBar.Visibility = Visibility.Collapsed;

            _NavigationFrame.Navigate(welcomePage);
            _NavigationFrame.Navigated += _NavigationFrame_Navigated;
        }

        internal class NativeMethods
        {
            [DllImport("user32.dll")]
            static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

            private const int GWL_EXSTYLE = -20;
            private const int WS_EX_DLGMODALFRAME = 0x0001;
            private const int SWP_NOSIZE = 0x0001;
            private const int SWP_NOMOVE = 0x0002;
            private const int SWP_NOZORDER = 0x0004;
            private const int SWP_FRAMECHANGED = 0x0020;

            public static void HideSystemMenu(Window window)
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            NativeMethods.HideSystemMenu(this);

            base.OnSourceInitialized(e);
        }

        private void _NavigationFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ButtonBack.Visibility = _NavigationFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            PanelTitleBar.Visibility = e.Content != welcomePage ? Visibility.Visible : Visibility.Collapsed;
            LabelTitle.Text = ((Page)e.Content).Title;
        }

        private Page welcomePage;

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.GoBack();
        }

        private void buttonMyDevices_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new ViewDevicesPage());
        }

        private void buttonAddDevice_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new SetupDevicePage());
        }

        private void buttonSamples_Click(object sender, RoutedEventArgs e)
        {
        }

        private void buttonTestOnly_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new PageTestOnly());
        }
    }
}
