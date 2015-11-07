using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for buttonSideMenu.xaml
    /// </summary>
    public partial class ButtonSideMenu : UserControl
    {
        public ButtonSideMenu()
        {
            InitializeComponent();
        }

        [Category("Behavior")]
        public event RoutedEventHandler Click;

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    if (_selected)
                        Highlight();
                    else
                        ReturnNormal();
                }
            }
        }

        private FontFamily SegoeMDL2Font = new FontFamily("Segoe MDL2 Assets");
        private FontFamily IotFont = new FontFamily("IOT MDL2 Assets");

        [CategoryAttribute("Common")]
        public bool UseIotFont
        {
            get
            {
                return this.textBlockIcon.FontFamily == IotFont;
            }
            set
            {
                this.textBlockIcon.FontFamily = (value) ? IotFont : SegoeMDL2Font;
            }
        }

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

        private bool _selected = false;
        private bool _mouseDown = false;

        private void Highlight()
        {
            Storyboard animation = (Storyboard)FindResource("StoryboardMouseEnter");
            animation.Begin();
        }

        private void ReturnNormal()
        {
            Storyboard animation = (Storyboard)FindResource("StoryboardMouseLeave");
            animation.Begin();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (!Selected)
                Highlight();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (!Selected)
                ReturnNormal();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Space || e.Key == Key.Enter)
                DoClick();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _mouseDown = true;
        }

        private void DoClick()
        {
            Selected = true;

            var myParent = this.Parent as Panel;
            if (myParent != null)
            {
                foreach (var cur in myParent.Children)
                {
                    var currentButton = cur as ButtonSideMenu;
                    if (currentButton != null && currentButton != this)
                    {
                        currentButton.Selected = false;
                    }
                }
            }

            Click?.Invoke(this, new RoutedEventArgs());
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (!_mouseDown) return;
            DoClick();
            _mouseDown = false;
        }
    }
}
