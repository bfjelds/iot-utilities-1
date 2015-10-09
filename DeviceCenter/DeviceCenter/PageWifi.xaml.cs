using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeviceCenter
{
    public class WifiEntry
    {
        private const string WifiIcons = "";
        public string Name { get; private set; }
        public string Secure { get; private set; }
        public string WifiIcon
        {
            get
            {
                switch (SignalStrength)
                {
                    case 1: return WifiIcons.Substring(1, 1);
                    case 2: return WifiIcons.Substring(2, 1);
                    case 3: return WifiIcons.Substring(3, 1);
                    case 4: return WifiIcons.Substring(4, 1);
                    default: return WifiIcons.Substring(0, 1);
                }
            }
        }
        public int SignalStrength { get; private set; }

        public Visibility ShowConnect { get; private set; }
        public Visibility ShowConnecting { get; private set; }
    }

    /// <summary>
    /// Interaction logic for PageWifi.xaml
    /// </summary>
    public partial class PageWifi : Page
    {
        public PageWifi()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ListViewDevices_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ListViewDevices_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void ListViewDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
