namespace DeviceCenter
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    public class UserInfo
    {
        public UserInfo()
        {
            this.DeviceName = string.Empty;
            this.UserName = string.Empty;
            this.Password = string.Empty;
            this.SavePassword = false;
        }

        public string DeviceName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool? SavePassword { get; set; }
    }

    /// <summary>
    /// Interaction logic for DialogAuthenticate.xaml
    /// </summary>
    public partial class DialogAuthenticate : Window
    {
        static Dictionary<string, UserInfo> savedPasswords = new Dictionary<string, UserInfo>();

        public bool GetSavedPassword(string deviceName, out UserInfo info)
        {
            return savedPasswords.TryGetValue(deviceName, out info);
        }

        public DialogAuthenticate(UserInfo info)
        {
            InitializeComponent();

            DataContext = info;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeMethods.HideSystemMenu(this);
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            UserInfo info = this.DataContext as UserInfo;

            info.UserName = editUserName.Text;
            info.Password = editPassword.Password;
            info.SavePassword = checkboxSavePassword.IsChecked;

            if (info.SavePassword.HasValue && info.SavePassword.Value)
                savedPasswords.Add(info.DeviceName, info);

            else if (savedPasswords.ContainsKey(info.DeviceName))
                savedPasswords.Remove(info.DeviceName);

            this.DialogResult = true;

            this.Close();
        }

        private void UpdateState()
        {
            buttonOk.IsEnabled =
                editPassword.Password.Length > 0 &&
                editUserName.Text.Length > 0;
        }

        private void editPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateState();
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateState();
        }
    }
}
