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
            rectangleSelected.Opacity = 0;
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
                        ((Storyboard)FindResource("StoryboardSelect")).Begin();
                    else
                        ((Storyboard)FindResource("StoryboardDeselect")).Begin();
                }
            }
        }

        [CategoryAttribute("Common")]
        public FontFamily IconFont
        {
            get
            {
                return this.textBlockIcon.FontFamily;
            }
            set
            {
                this.textBlockIcon.FontFamily = value;
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

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            ((Storyboard)FindResource("StoryboardMouseEnter")).Begin();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            ((Storyboard)FindResource("StoryboardMouseLeave")).Begin();
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
