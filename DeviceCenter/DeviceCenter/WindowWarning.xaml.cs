
namespace DeviceCenter
{
    using System;
    using System.Windows;
    /// <summary>
    /// Interaction logic for WindowWarning.xaml
    /// </summary>
    public partial class WindowWarning : Window
    {
        public WindowWarning()
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeMethods.HideSystemMenu(this);
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
