using System;
using System.Windows.Controls;

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
        }
    }
}