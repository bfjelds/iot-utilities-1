using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for ButtonAppInfo.xaml
    /// </summary>
    public partial class ButtonAppInfo : UserControl
    {
        public class ButtonAppEventArgs : EventArgs
        {
            public ButtonAppEventArgs(AppInformation info)
            {
                
                this.Info = info;
            }

            public AppInformation Info { get; private set; }
        }

        public ButtonAppInfo(AppInformation info)
        {
            InitializeComponent();

            this.Info = info;
            this.DataContext = info;

            buttonMain.Click += ButtonMain_Click;
        }

        public AppInformation Info { get; private set; }

        private void ButtonMain_Click(object sender, RoutedEventArgs e)
        {
            if (this.Click != null)
                this.Click(this, new ButtonAppEventArgs(Info));
        }

        public delegate void RoutedAppEventHandler(object sender, ButtonAppEventArgs e);

        [Category("Behavior")]
        public event RoutedAppEventHandler Click;
    }
}
