
namespace DeviceCenter
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    /// <summary>
    /// Interaction logic for WindowWarning.xaml
    /// </summary>
    public partial class WindowError : Window
    {
        public WindowError()
        {
            InitializeComponent();
        }

        public string Header
        {
            get { return labelMessageHeader.Text; }
            set { labelMessageHeader.Text = value; }
        }
        public string Message
        {
            get { return labelMessageText.Text; }
            set { labelMessageText.Text = value; }
        }

        public Uri HelpLink
        {
            set { labelMessageHelpLinkUrl.NavigateUri = value; }
        }

        public string HelpLinkText
        {
            get { return labelMessageHelpLinkText.Text; }
            set { labelMessageHelpLinkText.Text = value; }
        }

        public bool HelpLink_Enabled
        {
            get { return labelMessageHelpLink.Visibility == Visibility.Visible; }
            set { labelMessageHelpLink.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeMethods.HideSystemMenu(this);
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                        ex.Message,
                        LocalStrings.AppNameDisplay,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
            }
            e.Handled = true;
        }
    }
}
