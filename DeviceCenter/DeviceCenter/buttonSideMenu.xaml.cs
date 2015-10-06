using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// <summary>
    /// Interaction logic for buttonSideMenu.xaml
    /// </summary>
    public partial class buttonSideMenu : UserControl
    {
        public buttonSideMenu()
        {
            InitializeComponent();

            //Button x;
            //x.Click
            //x.Content
        }

        [Category("Behavior")]
        public event RoutedEventHandler Click;

        [CategoryAttribute("Common")]
        public string Icon
        {
            get { return textBlockIcon.Text; }
            set { textBlockIcon.Text = value; }
        }

        [CategoryAttribute("Common")]
        public string Text
        {
            get { return textBlockText.Text; }
            set { textBlockText.Text = value; }
        }

        private bool _mouseDown = false;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _mouseDown = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_mouseDown)
                if (Click != null)
                    Click.Invoke(this, new RoutedEventArgs());
            _mouseDown = false;
        }
    }
}
